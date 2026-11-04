// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.ZCore.Type
{
    /// <summary>
    /// TaskCompletedInfoType - payload for task.completed bus events.
    /// </summary>
    public sealed class TaskCompletedInfoType
    {
        /// <summary>
        /// TaskId
        /// </summary>
        public Guid TaskId
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
        /// CreatedAt (UTC)
        /// </summary>
        public DateTime CreatedAt
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
        /// DurationMs - wall-clock from CreatedAt to CompletedAt
        /// </summary>
        public double DurationMs
        {
            get;
            set;
        }
    }
}
