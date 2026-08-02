// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.ZCore.Type
{
    /// <summary>
    /// AgentDiagnosticType
    /// </summary>
    public class AgentDiagnosticType
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
        /// ConnectionState - healthy, stale, offline, disabled, never_seen
        /// </summary>
        public string? ConnectionState
        {
            get;
            set;
        }

        /// <summary>
        /// SecondsSinceLastHeartbeat
        /// </summary>
        public int SecondsSinceLastHeartbeat
        {
            get;
            set;
        }

        /// <summary>
        /// HeartbeatTimeoutSeconds
        /// </summary>
        public int HeartbeatTimeoutSeconds
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
        /// HasOpenOfflineAlert
        /// </summary>
        public bool HasOpenOfflineAlert
        {
            get;
            set;
        }

        /// <summary>
        /// DiagnosticMessage
        /// </summary>
        public string? DiagnosticMessage
        {
            get;
            set;
        }

        /// <summary>
        /// RecommendedAction
        /// </summary>
        public string? RecommendedAction
        {
            get;
            set;
        }
    }
}
