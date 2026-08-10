// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.Server.Data.Entities
{
    /// <summary>
    /// AuditLogEntity - attributed operation history.
    /// </summary>
    public class AuditLogEntity
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
        /// OccurredAt
        /// </summary>
        public DateTime OccurredAt
        {
            get;
            set;
        }

        /// <summary>
        /// ActorUserId
        /// </summary>
        public Guid? ActorUserId
        {
            get;
            set;
        }

        /// <summary>
        /// ActorUsername
        /// </summary>
        public string? ActorUsername
        {
            get;
            set;
        }

        /// <summary>
        /// ActorRole
        /// </summary>
        public string? ActorRole
        {
            get;
            set;
        }

        /// <summary>
        /// Action - e.g. catalog.create, package.deploy
        /// </summary>
        public string Action
        {
            get;
            set;
        } = "";

        /// <summary>
        /// TargetType - e.g. package, agent, binding
        /// </summary>
        public string? TargetType
        {
            get;
            set;
        }

        /// <summary>
        /// TargetKey - package name, agent key, etc.
        /// </summary>
        public string? TargetKey
        {
            get;
            set;
        }

        /// <summary>
        /// Success
        /// </summary>
        public bool Success
        {
            get;
            set;
        }

        /// <summary>
        /// Summary
        /// </summary>
        public string Summary
        {
            get;
            set;
        } = "";

        /// <summary>
        /// DetailsJson
        /// </summary>
        public string? DetailsJson
        {
            get;
            set;
        }

        /// <summary>
        /// Source - server or agent
        /// </summary>
        public string Source
        {
            get;
            set;
        } = "server";
    }
}
