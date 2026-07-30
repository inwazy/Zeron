// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.Server.Data.Entities
{
    /// <summary>
    /// TaskResultEntity
    /// </summary>
    public class TaskResultEntity
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
        /// AssignmentId
        /// </summary>
        public Guid AssignmentId
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

        /// <summary>
        /// CompletedAt
        /// </summary>
        public DateTime CompletedAt
        {
            get;
            set;
        }

        /// <summary>
        /// Assignment
        /// </summary>
        public TaskAssignmentEntity? Assignment
        {
            get;
            set;
        }
    }
}
