// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.ZCore.Type
{
    /// <summary>
    /// ExternalScriptJsonResultType - optional trailing stdout JSON contract.
    /// </summary>
    public sealed class ExternalScriptJsonResultType
    {
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
        public int? ExitCode
        {
            get;
            set;
        }

        /// <summary>
        /// Message
        /// </summary>
        public string? Message
        {
            get;
            set;
        }
    }
}
