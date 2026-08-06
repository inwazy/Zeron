// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.Server.Data.Entities
{
    /// <summary>
    /// AgentEntity
    /// </summary>
    public class AgentEntity
    {
        /// <summary>
        /// Id
        /// </summary>
        public Guid Id
        {
            get;
            set;
        }

        /// <summary>
        /// AgentKey - maps to AgentServer.AgentId on the agent.
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
        /// IpAddress
        /// </summary>
        public string? IpAddress
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
        /// Status - online, offline, disabled
        /// </summary>
        public string Status
        {
            get;
            set;
        } = "offline";

        /// <summary>
        /// RegisteredAt
        /// </summary>
        public DateTime RegisteredAt
        {
            get;
            set;
        }

        /// <summary>
        /// LastSeenAt
        /// </summary>
        public DateTime LastSeenAt
        {
            get;
            set;
        }

        /// <summary>
        /// LastHeartbeatAt
        /// </summary>
        public DateTime LastHeartbeatAt
        {
            get;
            set;
        }

        /// <summary>
        /// SupportedEnginesJson - serialized ScriptEngineInfoType list from agent heartbeat.
        /// </summary>
        public string? SupportedEnginesJson
        {
            get;
            set;
        }

        /// <summary>
        /// Heartbeats
        /// </summary>
        public ICollection<AgentHeartbeatEntity> Heartbeats { get; set; } = [];

        /// <summary>
        /// Assignments
        /// </summary>
        public ICollection<TaskAssignmentEntity> Assignments { get; set; } = [];

        /// <summary>
        /// Events
        /// </summary>
        public ICollection<EventEntity> Events { get; set; } = [];
    }
}
