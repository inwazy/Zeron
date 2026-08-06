// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.ZCore.Type
{
    /// <summary>
    /// DashboardTaskItemType
    /// </summary>
    public class DashboardTaskItemType
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
        /// TargetApi
        /// </summary>
        public string TargetApi
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
        /// CreatedAt
        /// </summary>
        public DateTime CreatedAt
        {
            get;
            set;
        }

        /// <summary>
        /// AssignmentCount
        /// </summary>
        public int AssignmentCount
        {
            get;
            set;
        }
    }
}
