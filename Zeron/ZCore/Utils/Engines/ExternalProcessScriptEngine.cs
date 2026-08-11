// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Zeron.ZCore.Type;
using Zeron.ZInterfaces;

namespace Zeron.ZCore.Utils.Engines
{
    /// <summary>
    /// ExternalProcessScriptEngine - config-driven process runner for user-supplied engines.
    /// </summary>
    public sealed class ExternalProcessScriptEngine : IScriptEngine
    {
        // Max captured stdout/stderr length.
        private const int MaxOutputLength = 8000;

        // Engine options.
        private readonly ExternalProcessScriptEngineOptionsType m_Options;

        /// <summary>
        /// ExternalProcessScriptEngine
        /// </summary>
        /// <param name="options"></param>
        /// <returns>Returns void.</returns>
        public ExternalProcessScriptEngine(
            ExternalProcessScriptEngineOptionsType options)
        {
            ArgumentNullException.ThrowIfNull(options);

            if (string.IsNullOrWhiteSpace(options.Id))
            {
                throw new ArgumentException("Engine Id is required.", nameof(options));
            }

            if (string.IsNullOrWhiteSpace(options.ExecutablePath))
            {
                throw new ArgumentException("ExecutablePath is required.", nameof(options));
            }

            m_Options = options;
        }

        /// <summary>
        /// Id
        /// </summary>
        /// <returns>Returns string.</returns>
        public string Id => m_Options.Id;

        /// <summary>
        /// DisplayName
        /// </summary>
        /// <returns>Returns string.</returns>
        public string DisplayName => string.IsNullOrWhiteSpace(m_Options.DisplayName)
            ? m_Options.Id
            : m_Options.DisplayName;

        /// <summary>
        /// Platforms
        /// </summary>
        /// <returns>Returns IReadOnlyList<string>.</returns>
        public IReadOnlyList<string> Platforms => m_Options.Platforms;

        /// <summary>
        /// IsAvailable
        /// </summary>
        /// <returns>Returns bool.</returns>
        public bool IsAvailable()
        {
            if (!m_Options.Enabled)
            {
                return false;
            }

            return ExecutableExists(m_Options.ExecutablePath);
        }

        /// <summary>
        /// Execute
        /// </summary>
        /// <param name="request"></param>
        /// <returns>Returns ScriptResult.</returns>
        public ScriptResultType Execute(
            ScriptRequestType request)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (string.IsNullOrWhiteSpace(request.Script) && string.IsNullOrWhiteSpace(request.ScriptPath))
            {
                return new ScriptResultType
                {
                    EngineId = Id,
                    Success = true,
                    ExitCode = 0
                };
            }

            if (!m_Options.Enabled)
            {
                return Fail("Engine is disabled: " + Id);
            }

            if (!IsAvailable())
            {
                return Fail("Executable is not available: " + m_Options.ExecutablePath);
            }

            int timeoutMs = request.TimeoutMs > 0
                ? request.TimeoutMs
                : ScriptHostServer.GetDefaultTimeoutMs();

            string? tempScriptPath = null;

            try
            {
                string? scriptPath = request.ScriptPath?.Trim();
                string? stdinPayload = null;

                if (string.IsNullOrWhiteSpace(scriptPath)
                    && !string.IsNullOrWhiteSpace(request.Script))
                {
                    switch (m_Options.InlineMode)
                    {
                        case ExternalScriptInlineModeType.StdIn:
                            stdinPayload = request.Script;
                            break;

                        case ExternalScriptInlineModeType.TempFile:
                            tempScriptPath = WriteTempScript(request.Script);
                            scriptPath = tempScriptPath;
                            break;

                        case ExternalScriptInlineModeType.None:
                            return Fail("Inline script requires inline_mode stdin or tempfile for engine " + Id);
                    }
                }

                string arguments = ExpandArguments(
                    m_Options.ArgumentsTemplate,
                    scriptPath,
                    request.Arguments,
                    request.Script);

                ProcessStartInfo startInfo = new()
                {
                    FileName = m_Options.ExecutablePath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = stdinPayload != null
                };

                if (!string.IsNullOrWhiteSpace(request.WorkingDirectory)
                    && Directory.Exists(request.WorkingDirectory))
                {
                    startInfo.WorkingDirectory = request.WorkingDirectory;
                }

                using Process? process = Process.Start(startInfo);

                if (process == null)
                {
                    return Fail("Failed to start process: " + m_Options.ExecutablePath);
                }

                Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync();
                Task<string> stderrTask = process.StandardError.ReadToEndAsync();

                if (stdinPayload != null)
                {
                    process.StandardInput.Write(stdinPayload);
                    process.StandardInput.Close();
                }

                if (!process.WaitForExit(timeoutMs))
                {
                    try
                    {
                        process.Kill(entireProcessTree: true);
                    }
                    catch (Exception)
                    {
                    }

                    string timedOutOut = SafeGetResult(stdoutTask);
                    string timedOutErr = SafeGetResult(stderrTask);

                    return new ScriptResultType
                    {
                        EngineId = Id,
                        Success = false,
                        ExitCode = -1,
                        StdOut = TrimOutput(timedOutOut),
                        StdErr = TrimOutput(timedOutErr),
                        ErrorMessage = string.Format(CultureInfo.InvariantCulture,
                            "Engine '{0}' timed out after {1} ms.", Id, timeoutMs)
                    };
                }

                process.WaitForExit();
                string stdOut = SafeGetResult(stdoutTask);
                string stdErr = SafeGetResult(stderrTask);

                return BuildResult(process.ExitCode, stdOut, stdErr);
            }
            catch (Exception e)
            {
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture,
                    "ExternalProcessScriptEngine[{0}] Error:{1}\n{2}", Id, e.Message, e.StackTrace));

                return Fail(e.Message);
            }
            finally
            {
                if (!string.IsNullOrWhiteSpace(tempScriptPath))
                {
                    try
                    {
                        File.Delete(tempScriptPath);
                    }
                    catch (Exception)
                    {
                    }
                }
            }
        }

        /// <summary>
        /// ExpandArguments
        /// </summary>
        /// <param name="template"></param>
        /// <param name="scriptPath"></param>
        /// <param name="arguments"></param>
        /// <param name="script"></param>
        /// <returns>Returns string.</returns>
        public static string ExpandArguments(
            string? template,
            string? scriptPath,
            string? arguments,
            string? script)
        {
            string pattern = template ?? "";
            string pathToken = string.IsNullOrWhiteSpace(scriptPath) ? "" : QuoteArgument(scriptPath.Trim());
            string argsToken = string.IsNullOrWhiteSpace(arguments) ? "" : arguments.Trim();
            string scriptToken = script ?? "";

            string expanded = pattern
                .Replace("{scriptPath}", pathToken, StringComparison.OrdinalIgnoreCase)
                .Replace("{arguments}", argsToken, StringComparison.OrdinalIgnoreCase)
                .Replace("{script}", scriptToken, StringComparison.OrdinalIgnoreCase);

            return expanded.Trim();
        }

        /// <summary>
        /// BuildResult
        /// </summary>
        /// <param name="exitCode"></param>
        /// <param name="stdOut"></param>
        /// <param name="stdErr"></param>
        /// <returns>Returns ScriptResult.</returns>
        private ScriptResultType BuildResult(
            int exitCode,
            string stdOut,
            string stdErr)
        {
            ScriptResultType result = new()
            {
                EngineId = Id,
                ExitCode = exitCode,
                Success = exitCode == 0,
                StdOut = TrimOutput(stdOut),
                StdErr = TrimOutput(stdErr),
                ErrorMessage = exitCode == 0 ? null : "Process exited with code " + exitCode
            };

            if (TryParseTrailingJson(stdOut, out ExternalScriptJsonResultType? json) && json != null)
            {
                result.Success = json.Success;
                result.ExitCode = json.ExitCode ?? exitCode;
                result.ErrorMessage = string.IsNullOrWhiteSpace(json.Message)
                    ? (result.Success ? null : "Engine reported failure.")
                    : json.Message;
            }

            return result;
        }

        /// <summary>
        /// TryParseTrailingJson
        /// </summary>
        /// <param name="stdOut"></param>
        /// <param name="parsed"></param>
        /// <returns>Returns bool.</returns>
        public static bool TryParseTrailingJson(
            string? stdOut,
            out ExternalScriptJsonResultType? parsed)
        {
            parsed = null;

            if (string.IsNullOrWhiteSpace(stdOut))
            {
                return false;
            }

            string[] lines = stdOut.Replace("\r\n", "\n", StringComparison.Ordinal)
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (lines.Length == 0)
            {
                return false;
            }

            string last = lines[^1];

            if (!last.StartsWith('{') || !last.EndsWith('}'))
            {
                return false;
            }

            try
            {
                parsed = JsonSerializer.Deserialize<ExternalScriptJsonResultType>(
                    last,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return parsed != null;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        /// <summary>
        /// WriteTempScript
        /// </summary>
        /// <param name="script"></param>
        /// <returns>Returns string.</returns>
        private static string WriteTempScript(
            string script)
        {
            string path = Path.Combine(Path.GetTempPath(), "zeron-script-" + Guid.NewGuid().ToString("N") + ".tmp");
            File.WriteAllText(path, script, Encoding.UTF8);

            return path;
        }

        /// <summary>
        /// ExecutableExists
        /// </summary>
        /// <param name="executablePath"></param>
        /// <returns>Returns bool.</returns>
        internal static bool ExecutableExists(
            string executablePath)
        {
            try
            {
                if (Path.IsPathRooted(executablePath))
                {
                    return File.Exists(executablePath);
                }

                string? pathEnv = Environment.GetEnvironmentVariable("PATH");

                if (string.IsNullOrWhiteSpace(pathEnv))
                {
                    return false;
                }

                foreach (string directory in pathEnv.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
                {
                    string candidate = Path.Combine(directory.Trim(), executablePath);

                    if (File.Exists(candidate))
                    {
                        return true;
                    }

                    if (OperatingSystem.IsWindows()
                        && !executablePath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
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
        /// QuoteArgument
        /// </summary>
        /// <param name="value"></param>
        /// <returns>Returns string.</returns>
        private static string QuoteArgument(
            string value)
        {
            if (value.Length >= 2 && value.StartsWith('"') && value.EndsWith('"'))
            {
                return value;
            }

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

            return value.Length <= MaxOutputLength
                ? value
                : value[..MaxOutputLength] + "...(truncated)";
        }

        /// <summary>
        /// SafeGetResult
        /// </summary>
        /// <param name="task"></param>
        /// <returns>Returns string.</returns>
        private static string SafeGetResult(
            Task<string> task)
        {
            try
            {
                return task.Wait(TimeSpan.FromSeconds(2)) ? task.Result : "";
            }
            catch (Exception)
            {
                return "";
            }
        }

        /// <summary>
        /// Fail
        /// </summary>
        /// <param name="message"></param>
        /// <returns>Returns ScriptResult.</returns>
        private ScriptResultType Fail(
            string message)
        {
            return new ScriptResultType
            {
                EngineId = Id,
                Success = false,
                ExitCode = -1,
                ErrorMessage = message
            };
        }
    }
}
