// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.Server.Data.Entities
{
    /// <summary>
    /// TaskAssignmentEntity
    /// </summary>
    public class TaskAssignmentEntity
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
        /// TaskId
        /// </summary>
        public Guid TaskId
        {
            get;
            set;
        }

        /// <summary>
        /// AgentId
        /// </summary>
        public Guid AgentId
        {
            get;
            set;
        }

        /// <summary>
        /// Status
        /// </summary>
        public string Status
        {
            get;
            set;
        } = "pending";

        /// <summary>
        /// AssignedAt
        /// </summary>
        public DateTime AssignedAt
        {
            get;
            set;
        }

        /// <summary>
        /// StartedAt
        /// </summary>
        public DateTime? StartedAt
        {
            get;
            set;
        }

        /// <summary>
        /// CompletedAt
        /// </summary>
        public DateTime? CompletedAt
        {
            get;
            set;
        }

        /// <summary>
        /// RetryCount
        /// </summary>
        public int RetryCount
        {
            get;
            set;
        }

        /// <summary>
        /// Task
        /// </summary>
        public TaskEntity? Task
        {
            get;
            set;
        }

        /// <summary>
        /// Agent
        /// </summary>
        public AgentEntity? Agent
        {
            get;
            set;
        }

        /// <summary>
        /// Result
        /// </summary>
        public TaskResultEntity? Result
        {
            get;
            set;
        }
    }
}
