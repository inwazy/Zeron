// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.ZCore.Type
{
    /// <summary>
    /// AgentHeartbeatResponseType
    /// </summary>
    public class AgentHeartbeatResponseType
    {
        /// <summary>
        /// Success
        /// </summary>
        public bool Success
        {
            get;
            set;
        }

        /// <summary>
        /// ServerTime
        /// </summary>
        public string? ServerTime
        {
            get;
            set;
        }

        /// <summary>
        /// PendingTasks
        /// </summary>
        public List<PendingTaskType>? PendingTasks
        {
            get;
            set;
        }
    }
}
