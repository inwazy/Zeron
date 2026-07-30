// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.ZCore.Type
{
    /// <summary>
    /// AgentHeartbeatRequestType
    /// </summary>
    public class AgentHeartbeatRequestType
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
        /// MachineName
        /// </summary>
        public string? MachineName
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
        /// Version
        /// </summary>
        public string? Version
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
    }
}
