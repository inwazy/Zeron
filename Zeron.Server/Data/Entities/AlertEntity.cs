// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Zeron.Server.ZCore.Type;

namespace Zeron.Server.Data.Entities
{
    /// <summary>
    /// AlertEntity
    /// </summary>
    public class AlertEntity
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
        /// RuleType - agent.offline, etc.
        /// </summary>
        public string RuleType
        {
            get;
            set;
        } = "";

        /// <summary>
        /// AgentKey
        /// </summary>
        public string? AgentKey
        {
            get;
            set;
        }

        /// <summary>
        /// AgentId
        /// </summary>
        public Guid? AgentId
        {
            get;
            set;
        }

        /// <summary>
        /// Title
        /// </summary>
        public string Title
        {
            get;
            set;
        } = "";

        /// <summary>
        /// Message
        /// </summary>
        public string Message
        {
            get;
            set;
        } = "";

        /// <summary>
        /// Severity - warning, critical
        /// </summary>
        public string Severity
        {
            get;
            set;
        } = AlertSeveritiesType.Warning;

        /// <summary>
        /// Status - open, acknowledged, resolved
        /// </summary>
        public string Status
        {
            get;
            set;
        } = AlertStatusesType.Open;

        /// <summary>
        /// CreatedAt
        /// </summary>
        public DateTime CreatedAt
        {
            get;
            set;
        }

        /// <summary>
        /// ResolvedAt
        /// </summary>
        public DateTime? ResolvedAt
        {
            get;
            set;
        }

        /// <summary>
        /// NotifiedAt
        /// </summary>
        public DateTime? NotifiedAt
        {
            get;
            set;
        }
    }
}
