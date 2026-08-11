// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.ZCore.Type
{
    /// <summary>
    /// DeviceAgentStatusType - self-service status for a bound Demand agent.
    /// </summary>
    public sealed class DeviceAgentStatusType
    {
        /// <summary>
        /// AgentKey
        /// </summary>
        public string? AgentKey
        {
            get;
            set;
        }

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
        public string? Status
        {
            get;
            set;
        }

        /// <summary>
        /// Version
        /// </summary>
        public string? Version
        {
            get;
            set;
        }

        /// <summary>
        /// IpAddress
        /// </summary>
        public string? IpAddress
        {
            get;
            set;
        }

        /// <summary>
        /// LastHeartbeatAt
        /// </summary>
        public DateTime? LastHeartbeatAt
        {
            get;
            set;
        }

        /// <summary>
        /// InstallQueueCount
        /// </summary>
        public int InstallQueueCount
        {
            get;
            set;
        }

        /// <summary>
        /// InstallRunning
        /// </summary>
        public bool InstallRunning
        {
            get;
            set;
        }

        /// <summary>
        /// SchedulerTaskCount
        /// </summary>
        public int SchedulerTaskCount
        {
            get;
            set;
        }

        /// <summary>
        /// UptimeSeconds
        /// </summary>
        public long UptimeSeconds
        {
            get;
            set;
        }

        /// <summary>
        /// LastCatalogSyncAt
        /// </summary>
        public DateTime? LastCatalogSyncAt
        {
            get;
            set;
        }
    }
}
