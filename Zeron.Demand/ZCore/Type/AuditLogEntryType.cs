// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.Demand.ZCore.Type
{
    /// <summary>
    /// AuditLogEntryType
    /// </summary>
    internal class AuditLogEntryType
    {
        /// <summary>
        /// Id
        /// </summary>
        /// <returns>Returns long.</returns>
        public long Id
        {
            get;
            set;
        }

        /// <summary>
        /// AgentId
        /// </summary>
        /// <returns>Returns string.</returns>
        public string? AgentId
        {
            get;
            set;
        }

        /// <summary>
        /// ApiName
        /// </summary>
        /// <returns>Returns string.</returns>
        public string? ApiName
        {
            get;
            set;
        }

        /// <summary>
        /// Command
        /// </summary>
        /// <returns>Returns string.</returns>
        public string? Command
        {
            get;
            set;
        }

        /// <summary>
        /// Success
        /// </summary>
        /// <returns>Returns bool.</returns>
        public bool Success
        {
            get;
            set;
        }

        /// <summary>
        /// Message
        /// </summary>
        /// <returns>Returns string.</returns>
        public string? Message
        {
            get;
            set;
        }

        /// <summary>
        /// Source
        /// </summary>
        /// <returns>Returns string.</returns>
        public string? Source
        {
            get;
            set;
        }

        /// <summary>
        /// CreatedAt
        /// </summary>
        /// <returns>Returns string.</returns>
        public string? CreatedAt
        {
            get;
            set;
        }
    }
}
