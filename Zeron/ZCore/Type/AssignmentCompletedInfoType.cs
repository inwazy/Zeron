// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.ZCore.Type
{
    /// <summary>
    /// AssignmentCompletedInfoType - payload for assignment.completed bus events.
    /// </summary>
    public sealed class AssignmentCompletedInfoType
    {
        /// <summary>
        /// AssignmentId
        /// </summary>
        public Guid AssignmentId
        {
            get;
            set;
        }

        /// <summary>
        /// TaskId
        /// </summary>
        public Guid TaskId
        {
            get;
            set;
        }

        /// <summary>
        /// AgentKey
        /// </summary>
        public string? AgentKey
        {
            get;
            set;
        }

        /// <summary>
        /// TargetApi
        /// </summary>
        public string? TargetApi
        {
            get;
            set;
        }

        /// <summary>
        /// Status - completed or failed
        /// </summary>
        public string? Status
        {
            get;
            set;
        }

        /// <summary>
        /// AssignedAt (UTC)
        /// </summary>
        public DateTime AssignedAt
        {
            get;
            set;
        }

        /// <summary>
        /// CompletedAt (UTC)
        /// </summary>
        public DateTime CompletedAt
        {
            get;
            set;
        }

        /// <summary>
        /// DurationMs - wall-clock from AssignedAt to CompletedAt
        /// </summary>
        public double DurationMs
        {
            get;
            set;
        }
    }
}
