// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.Demand.ZCore.Type
{
    /// <summary>
    /// TaskPipelineResultType
    /// </summary>
    internal sealed class TaskPipelineResultType
    {
        /// <summary>
        /// TaskName
        /// </summary>
        /// <returns>Returns string.</returns>
        public string? TaskName
        {
            get;
            set;
        }

        /// <summary>
        /// Success
        /// </summary>
        /// <returns>Returns bool.</returns>
        public bool Success
        {
            get;
            set;
        }

        /// <summary>
        /// Message
        /// </summary>
        /// <returns>Returns string.</returns>
        public string? Message
        {
            get;
            set;
        }

        /// <summary>
        /// Steps
        /// </summary>
        /// <returns>Returns list of TaskStepResult.</returns>
        public List<TaskStepResultType> Steps
        {
            get;
            set;
        } = [];
    }
}
