// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.ZCore.Type
{
    /// <summary>
    /// AgentEventReportType
    /// </summary>
    public class AgentEventReportType
    {
        /// <summary>
        /// AgentId
        /// </summary>
        public string? AgentId
        {
            get;
            set;
        }

        /// <summary>
        /// Topic
        /// </summary>
        public string? Topic
        {
            get;
            set;
        }

        /// <summary>
        /// Payload
        /// </summary>
        public string? Payload
        {
            get;
            set;
        }
    }
}
