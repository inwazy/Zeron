// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.ZCore.Type
{
    /// <summary>
    /// PendingTaskType
    /// </summary>
    public class PendingTaskType
    {
        /// <summary>
        /// AssignmentId
        /// </summary>
        public string? AssignmentId
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
        /// Command
        /// </summary>
        public string? Command
        {
            get;
            set;
        }
    }
}
