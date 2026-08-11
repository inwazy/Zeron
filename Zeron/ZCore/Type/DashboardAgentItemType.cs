// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.ZCore.Type
{
    /// <summary>
    /// DashboardAgentItemType
    /// </summary>
    public sealed class DashboardAgentItemType
    {
        /// <summary>
        /// AgentKey
        /// </summary>
        public string AgentKey
        {
            get;
            set;
        } = "";

        /// <summary>
        /// MachineName
        /// </summary>
        public string? MachineName
        {
            get;
            set;
        }

        /// <summary>
        /// Status
        /// </summary>
        public string Status
        {
            get;
            set;
        } = "";

        /// <summary>
        /// ConnectionState
        /// </summary>
        public string ConnectionState
        {
            get;
            set;
        } = "";

        /// <summary>
        /// LastHeartbeatAt
        /// </summary>
        public DateTime? LastHeartbeatAt
        {
            get;
            set;
        }
    }
}
