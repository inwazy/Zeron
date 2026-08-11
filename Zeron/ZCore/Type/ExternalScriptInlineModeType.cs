// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.ZCore.Type
{
    /// <summary>
    /// ExternalScriptInlineModeType
    /// </summary>
    public enum ExternalScriptInlineModeType
    {
        /// <summary>
        /// None - ScriptPath / arguments only.
        /// </summary>
        None = 0,

        /// <summary>
        /// StdIn - write inline Script to process stdin.
        /// </summary>
        StdIn = 1,

        /// <summary>
        /// TempFile - write inline Script to a temp file and bind {scriptPath}.
        /// </summary>
        TempFile = 2
    }
}
