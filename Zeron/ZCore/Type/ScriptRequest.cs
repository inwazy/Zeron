// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.ZCore.Type
{
    /// <summary>
    /// ScriptRequest - input for ScriptHostServer / IScriptEngine.
    /// </summary>
    public class ScriptRequest
    {
        /// <summary>
        /// EngineId
        /// </summary>
        public string? EngineId
        {
            get;
            set;
        }

        /// <summary>
        /// Script - inline script or command text.
        /// </summary>
        public string? Script
        {
            get;
            set;
        }

        /// <summary>
        /// ScriptPath - optional path to a script file.
        /// </summary>
        public string? ScriptPath
        {
            get;
            set;
        }

        /// <summary>
        /// Arguments - optional args for ScriptPath engines.
        /// </summary>
        public string? Arguments
        {
            get;
            set;
        }

        /// <summary>
        /// TimeoutMs - 0 or negative uses ScriptHostServer default.
        /// </summary>
        public int TimeoutMs
        {
            get;
            set;
        }

        /// <summary>
        /// WorkingDirectory
        /// </summary>
        public string? WorkingDirectory
        {
            get;
            set;
        }
    }
}
