// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.ZCore.Type
{
    /// <summary>
    /// TaskScheduleCreateRequestType
    /// </summary>
    public class TaskScheduleCreateRequestType
    {
        /// <summary>
        /// Name
        /// </summary>
        public string? Name
        {
            get;
            set;
        }

        /// <summary>
        /// Description
        /// </summary>
        public string? Description
        {
            get;
            set;
        }

        /// <summary>
        /// Cron - NCrontab 5-field expression
        /// </summary>
        public string? Cron
        {
            get;
            set;
        }

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

        /// <summary>
        /// TargetType - all, agent, filter
        /// </summary>
        public string? TargetType
        {
            get;
            set;
        }

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
    }
}
