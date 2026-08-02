// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.ZCore.Type
{
    /// <summary>
    /// TaskScheduleInfoType
    /// </summary>
    public class TaskScheduleInfoType
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
        /// Cron
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
        /// AgentIds
        /// </summary>
        public List<string>? AgentIds
        {
            get;
            set;
        }

        /// <summary>
        /// HostnamePattern
        /// </summary>
        public string? HostnamePattern
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
