// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.ZCore.Type
{
    /// <summary>
    /// SchedulerTaskDefinition
    /// </summary>
    public class SchedulerTaskDefinition
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
        public List<TaskStepDefinition>? Steps
        {
            get;
            set;
        }
    }
}
