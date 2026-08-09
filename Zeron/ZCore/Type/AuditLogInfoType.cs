// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.ZCore.Type
{
    /// <summary>
    /// AuditLogInfoType
    /// </summary>
    public class AuditLogInfoType
    {
        /// <summary>
        /// Id
        /// </summary>
        public string? Id
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
        /// Action
        /// </summary>
        public string? Action
        {
            get;
            set;
        }

        /// <summary>
        /// TargetType
        /// </summary>
        public string? TargetType
        {
            get;
            set;
        }

        /// <summary>
        /// TargetKey
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
        public string? Summary
        {
            get;
            set;
        }

        /// <summary>
        /// DetailsJson
        /// </summary>
        public string? DetailsJson
        {
            get;
            set;
        }

        /// <summary>
        /// Source
        /// </summary>
        public string? Source
        {
            get;
            set;
        }
    }
}
