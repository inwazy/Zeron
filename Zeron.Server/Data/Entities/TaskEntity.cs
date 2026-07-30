// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.Server.Data.Entities
{
    /// <summary>
    /// TaskEntity
    /// </summary>
    public class TaskEntity
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
        /// Name
        /// </summary>
        public string Name
        {
            get;
            set;
        } = "";

        /// <summary>
        /// Description
        /// </summary>
        public string? Description
        {
            get;
            set;
        }

        /// <summary>
        /// TargetApi
        /// </summary>
        public string TargetApi
        {
            get;
            set;
        } = "";

        /// <summary>
        /// Command
        /// </summary>
        public string Command
        {
            get;
            set;
        } = "";


        /// <summary>
        /// TargetType
        /// </summary>
        public string TargetType
        {
            get;
            set;
        } = "all";

        /// <summary>
        /// TargetFilterJson
        /// </summary>
        public string? TargetFilterJson
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
        /// CreatedAt
        /// </summary>
        public DateTime CreatedAt
        {
            get;
            set;
        }

        /// <summary>
        /// Assignments
        /// </summary>
        public ICollection<TaskAssignmentEntity> Assignments { get; set; } = [];
    }
}
