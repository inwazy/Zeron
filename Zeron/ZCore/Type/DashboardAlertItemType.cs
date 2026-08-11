// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.ZCore.Type
{
    /// <summary>
    /// DashboardAlertItemType
    /// </summary>
    public sealed class DashboardAlertItemType
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
        /// Title
        /// </summary>
        public string Title
        {
            get;
            set;
        } = "";

        /// <summary>
        /// Message
        /// </summary>
        public string Message
        {
            get;
            set;
        } = "";

        /// <summary>
        /// Severity
        /// </summary>
        public string Severity
        {
            get;
            set;
        } = "";

        /// <summary>
        /// Status
        /// </summary>
        public string Status
        {
            get;
            set;
        } = "";

        /// <summary>
        /// AgentKey
        /// </summary>
        public string? AgentKey
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
    }
}
