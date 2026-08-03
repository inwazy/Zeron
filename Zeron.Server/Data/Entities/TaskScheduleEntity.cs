// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.Server.Data.Entities
{
    /// <summary>
    /// TaskScheduleEntity - central cron schedule that spawns TaskEntity instances.
    /// </summary>
    public class TaskScheduleEntity
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
        /// Cron - NCrontab 5-field expression (server local time)
        /// </summary>
        public string Cron
        {
            get;
            set;
        } = "";

        /// <summary>
        /// Enabled
        /// </summary>
        public bool Enabled
        {
            get;
            set;
        } = true;

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
        /// TargetType - all, agent, filter
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
        /// LastRunAt
        /// </summary>
        public DateTime? LastRunAt
        {
            get;
            set;
        }

        /// <summary>
        /// NextRunAt
        /// </summary>
        public DateTime? NextRunAt
        {
            get;
            set;
        }

        /// <summary>
        /// LastTaskId
        /// </summary>
        public Guid? LastTaskId
        {
            get;
            set;
        }

        /// <summary>
        /// CreatedAt
        /// </summary>
        public DateTime CreatedAt
        {
            get;
            set;
        }

        /// <summary>
        /// UpdatedAt
        /// </summary>
        public DateTime UpdatedAt
        {
            get;
            set;
        }
    }
}
