// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.ZCore.Type
{
    /// <summary>
    /// DataRetentionResultType - rows removed by a retention pass.
    /// </summary>
    public sealed class DataRetentionResultType
    {
        /// <summary>
        /// AuditLogsDeleted
        /// </summary>
        public int AuditLogsDeleted
        {
            get;
            set;
        }

        /// <summary>
        /// NotificationsDeleted
        /// </summary>
        public int NotificationsDeleted
        {
            get;
            set;
        }

        /// <summary>
        /// CatalogVersionsDeleted
        /// </summary>
        public int CatalogVersionsDeleted
        {
            get;
            set;
        }

        /// <summary>
        /// Skipped - retention disabled or nothing to prune
        /// </summary>
        public bool Skipped
        {
            get;
            set;
        }
    }
}
