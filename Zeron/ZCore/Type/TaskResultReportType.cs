// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.ZCore.Type
{
    /// <summary>
    /// TaskResultReportType
    /// </summary>
    public sealed class TaskResultReportType
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
        /// AgentId
        /// </summary>
        public string? AgentId
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
        /// ResponseJson
        /// </summary>
        public string? ResponseJson
        {
            get;
            set;
        }

        /// <summary>
        /// ErrorMessage
        /// </summary>
        public string? ErrorMessage
        {
            get;
            set;
        }
    }
}
