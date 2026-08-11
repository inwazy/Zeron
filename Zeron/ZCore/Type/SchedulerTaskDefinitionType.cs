// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.ZCore.Type
{
    /// <summary>
    /// SchedulerTaskDefinitionType
    /// </summary>
    public sealed class SchedulerTaskDefinitionType
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
        /// Cron
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
        /// Steps
        /// </summary>
        public List<TaskStepDefinitionType>? Steps
        {
            get;
            set;
        }
    }
}
