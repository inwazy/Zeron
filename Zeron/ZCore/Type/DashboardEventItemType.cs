// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.ZCore.Type
{
    /// <summary>
    /// DashboardEventItemType
    /// </summary>
    public class DashboardEventItemType
    {
        /// <summary>
        /// Id
        /// </summary>
        public long Id
        {
            get;
            set;
        }

        /// <summary>
        /// AgentKey
        /// </summary>
        public string? AgentKey
        {
            get;
            set;
        }

        /// <summary>
        /// Topic
        /// </summary>
        public string Topic
        {
            get;
            set;
        } = "";

        /// <summary>
        /// Payload
        /// </summary>
        public string Payload
        {
            get;
            set;
        } = "";

        /// <summary>
        /// ReceivedAt
        /// </summary>
        public DateTime ReceivedAt
        {
            get;
            set;
        }
    }
}
