// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using System.Diagnostics;
using System.Globalization;
using Zeron.ZCore.Type;
using Zeron.ZInterfaces;

namespace Zeron.ZCore.Utils.Engines
{
    /// <summary>
    /// PowerShellScriptEngine - Windows powershell.exe runner.
    /// </summary>
    public sealed class PowerShellScriptEngine : IScriptEngine
    {
        // Executable path (powershell.exe or full path).
        private readonly string m_ExecutablePath;

        // Whether this engine is enabled in config.
        private readonly bool m_Enabled;

        /// <summary>
        /// PowerShellScriptEngine
        /// </summary>
        /// <param name="executablePath"></param>
        /// <param name="enabled"></param>
        /// <returns>Returns void.</returns>
        public PowerShellScriptEngine(
            string? executablePath = null,
            bool enabled = true)
        {
            m_ExecutablePath = string.IsNullOrWhiteSpace(executablePath)
                ? "powershell.exe"
                : executablePath.Trim();
            m_Enabled = enabled;
        }

        /// <summary>
        /// Id
        /// </summary>
        public string Id => "powershell";

        /// <summary>
        /// DisplayName
        /// </summary>
        public string DisplayName => "Windows PowerShell";

        /// <summary>
        /// Platforms
        /// </summary>
        public IReadOnlyList<string> Platforms { get; } = ["windows"];

        /// <summary>
        /// IsAvailable
        /// </summary>
        /// <returns>Returns bool.</returns>
        public bool IsAvailable()
        {
            if (!m_Enabled)
            {
                return false;
            }

            try
            {
                if (Path.IsPathRooted(m_ExecutablePath))
                {
                    return File.Exists(m_ExecutablePath);
                }

                string? pathEnv = Environment.GetEnvironmentVariable("PATH");

                if (string.IsNullOrWhiteSpace(pathEnv))
                {
                    return false;
                }

                foreach (string directory in pathEnv.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
                {
                    string candidate = Path.Combine(directory.Trim(), m_ExecutablePath);

                    if (File.Exists(candidate))
                    {
                        return true;
                    }

                    if (!m_ExecutablePath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
                        && File.Exists(candidate + ".exe"))
                    {
                        return true;
                    }
                }

                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Execute
        /// </summary>
        /// <param name="request"></param>
        /// <returns>Returns ScriptResult.</returns>
        public ScriptResult Execute(
            ScriptRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (string.IsNullOrWhiteSpace(request.Script) && string.IsNullOrWhiteSpace(request.ScriptPath))
            {
                return new ScriptResult
                {
                    EngineId = Id,
                    Success = true,
                    ExitCode = 0
                };
            }

            if (!m_Enabled)
            {
                return Fail("PowerShell engine is disabled.");
            }

            if (!IsAvailable())
            {
                return Fail("PowerShell executable is not available: " + m_ExecutablePath);
            }

            int timeoutMs = request.TimeoutMs > 0 ? request.TimeoutMs : 300000;

            try
            {
                string arguments = BuildArguments(request);
                ProcessStartInfo startInfo = new()
                {
                    FileName = m_ExecutablePath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                if (!string.IsNullOrWhiteSpace(request.WorkingDirectory)
                    && Directory.Exists(request.WorkingDirectory))
                {
                    startInfo.WorkingDirectory = request.WorkingDirectory;
                }

                using Process? process = Process.Start(startInfo);

                if (process == null)
                {
                    return Fail("Failed to start PowerShell process.");
                }

                string stdOut = process.StandardOutput.ReadToEnd();
                string stdErr = process.StandardError.ReadToEnd();

                if (!process.WaitForExit(timeoutMs))
                {
                    try
                    {
                        process.Kill(entireProcessTree: true);
                    }
                    catch (Exception)
                    {
                    }

                    return new ScriptResult
                    {
                        EngineId = Id,
                        Success = false,
                        ExitCode = -1,
                        StdOut = TrimOutput(stdOut),
                        StdErr = TrimOutput(stdErr),
                        ErrorMessage = string.Format(CultureInfo.InvariantCulture,
                            "PowerShell timed out after {0} ms.", timeoutMs)
                    };
                }

                // Ensure async stream readers finished.
                process.WaitForExit();

                bool success = process.ExitCode == 0;

                if (!success)
                {
                    ZNLogger.Common.Warn(string.Format(CultureInfo.InvariantCulture,
                        "PowerShellScriptEngine exit code {0}", process.ExitCode));
                }

                return new ScriptResult
                {
                    EngineId = Id,
                    Success = success,
                    ExitCode = process.ExitCode,
                    StdOut = TrimOutput(stdOut),
                    StdErr = TrimOutput(stdErr),
                    ErrorMessage = success ? null : "PowerShell execution failed."
                };
            }
            catch (Exception e)
            {
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture,
                    "PowerShellScriptEngine Error:{0}\n{1}", e.Message, e.StackTrace));

                return Fail(e.Message);
            }
        }

        /// <summary>
        /// BuildArguments
        /// </summary>
        /// <param name="request"></param>
        /// <returns>Returns string.</returns>
        private static string BuildArguments(
            ScriptRequest request)
        {
            if (!string.IsNullOrWhiteSpace(request.ScriptPath))
            {
                string fileArgs = string.IsNullOrWhiteSpace(request.Arguments)
                    ? ""
                    : " " + request.Arguments.Trim();

                return "-NoProfile -NonInteractive -ExecutionPolicy Bypass -File "
                    + QuoteArgument(request.ScriptPath.Trim())
                    + fileArgs;
            }

            return "-NoProfile -NonInteractive -ExecutionPolicy Bypass -Command "
                + QuoteArgument(request.Script ?? "");
        }

        /// <summary>
        /// QuoteArgument
        /// </summary>
        /// <param name="value"></param>
        /// <returns>Returns string.</returns>
        private static string QuoteArgument(
            string value)
        {
            return "\"" + value.Replace("\"", "\\\"", StringComparison.Ordinal) + "\"";
        }

        /// <summary>
        /// TrimOutput
        /// </summary>
        /// <param name="value"></param>
        /// <returns>Returns string.</returns>
        private static string TrimOutput(
            string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "";
            }

            const int maxLength = 8000;

            return value.Length <= maxLength
                ? value
                : value[..maxLength] + "...(truncated)";
        }

        /// <summary>
        /// Fail
        /// </summary>
        /// <param name="message"></param>
        /// <returns>Returns ScriptResult.</returns>
        private ScriptResult Fail(
            string message)
        {
            return new ScriptResult
            {
                EngineId = Id,
                Success = false,
                ExitCode = -1,
                ErrorMessage = message
            };
        }
    }
}
