// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.Server.Data.Entities
{
    /// <summary>
    /// AgentHeartbeatEntity
    /// </summary>
    public class AgentHeartbeatEntity
    {
        /// <summary>
        /// Id
        /// </summary>
        public long Id
        {
            get;
            set;
        }

        /// <summary>
        /// AgentId
        /// </summary>
        public Guid AgentId
        {
            get;
            set;
        }

        /// <summary>
        /// ReportedAt
        /// </summary>
        public DateTime ReportedAt
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
        /// Agent
        /// </summary>
        public AgentEntity? Agent
        {
            get;
            set;
        }
    }
}
