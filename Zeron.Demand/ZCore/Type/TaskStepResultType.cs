// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.Demand.ZCore.Type
{
    /// <summary>
    /// TaskStepResultType
    /// </summary>
    internal class TaskStepResultType
    {
        /// <summary>
        /// Type
        /// </summary>
        /// <returns>Returns string.</returns>
        public string? Type
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
        /// Response
        /// </summary>
        /// <returns>Returns string.</returns>
        public string? Response
        {
            get;
            set;
        }
    }
}
