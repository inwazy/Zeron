// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.ZCore.Type
{
    /// <summary>
    /// ScriptResultType - output from ScriptHostServer / IScriptEngine.
    /// </summary>
    public sealed class ScriptResultType
    {
        /// <summary>
        /// EngineId
        /// </summary>
        public string EngineId
        {
            get;
            set;
        } = "";

        /// <summary>
        /// Success
        /// </summary>
        public bool Success
        {
            get;
            set;
        }

        /// <summary>
        /// ExitCode
        /// </summary>
        public int ExitCode
        {
            get;
            set;
        }

        /// <summary>
        /// StdOut
        /// </summary>
        public string? StdOut
        {
            get;
            set;
        }

        /// <summary>
        /// StdErr
        /// </summary>
        public string? StdErr
        {
            get;
            set;
        }

        /// <summary>
        /// ErrorMessage
        /// </summary>
        public string? ErrorMessage
        {
            get;
            set;
        }
    }
}
