// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using System.Diagnostics;
using System.Globalization;

namespace Zeron.ZCore.Utils
{
    /// <summary>
    /// ScriptExecutor - runs PowerShell scripts for install/uninstall hooks.
    /// </summary>
    public static class ScriptExecutor
    {
        /// <summary>
        /// Execute a PowerShell script or command. Returns true when exit code is 0.
        /// </summary>
        /// <param name="script"></param>
        /// <returns>Returns bool.</returns>
        public static bool Execute(
            string? script)
        {
            if (string.IsNullOrWhiteSpace(script))
            {
                return true;
            }

            try
            {
                ProcessStartInfo startInfo = new()
                {
                    FileName = "powershell.exe",
                    Arguments = "-NoProfile -NonInteractive -ExecutionPolicy Bypass -Command " + QuoteArgument(script),
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using Process? process = Process.Start(startInfo);

                if (process == null)
                {
                    return false;
                }

                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    ZNLogger.Common.Warn(string.Format(CultureInfo.InvariantCulture, "ScriptExecutor exit code {0}", process.ExitCode));
                }

                return process.ExitCode == 0;
            }
            catch (Exception e)
            {
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "ScriptExecutor Error:{0}\n{1}", e.Message, e.StackTrace));

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
            return "\"" + value.Replace("\"", "\\\"", StringComparison.Ordinal) + "\"";
        }
    }
}
