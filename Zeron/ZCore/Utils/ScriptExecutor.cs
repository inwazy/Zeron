// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.ZCore.Utils
{
    /// <summary>
    /// ScriptExecutor - legacy facade over ScriptHostServer (PowerShell).
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
            return ScriptHostServer.Execute("powershell", script).Success;
        }
    }
}
