// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.ZCore.Type
{
    /// <summary>
    /// AgentUpdateRequestType
    /// </summary>
    public sealed class AgentUpdateRequestType
    {
        /// <summary>
        /// Status - online, offline, disabled
        /// </summary>
        public string? Status
        {
            get;
            set;
        }
    }
}
