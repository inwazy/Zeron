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

        /// <summary>
        /// SupportedEngines - Script Host engines reported by the agent.
        /// </summary>
        public List<ScriptEngineInfoType> SupportedEngines
        {
            get;
            set;
        } = [];

        /// <summary>
        /// LastCatalogSyncAt - UTC time of last successful catalog sync on the agent.
        /// </summary>
        public DateTime? LastCatalogSyncAt
        {
            get;
            set;
        }
    }
}
