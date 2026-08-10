// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.ZCore.Type
{
    /// <summary>
    /// CatalogSyncHealthSummaryType
    /// </summary>
    public class CatalogSyncHealthSummaryType
    {
        /// <summary>
        /// Healthy
        /// </summary>
        public int Healthy
        {
            get;
            set;
        }

        /// <summary>
        /// Stale
        /// </summary>
        public int Stale
        {
            get;
            set;
        }

        /// <summary>
        /// NeverSynced
        /// </summary>
        public int NeverSynced
        {
            get;
            set;
        }

        /// <summary>
        /// Offline
        /// </summary>
        public int Offline
        {
            get;
            set;
        }

        /// <summary>
        /// RecentlyFailed
        /// </summary>
        public int RecentlyFailed
        {
            get;
            set;
        }

        /// <summary>
        /// StaleThresholdMinutes
        /// </summary>
        public int StaleThresholdMinutes
        {
            get;
            set;
        }

        /// <summary>
        /// GeneratedAtUtc
        /// </summary>
        public DateTime GeneratedAtUtc
        {
            get;
            set;
        }

        /// <summary>
        /// Agents
        /// </summary>
        public List<CatalogSyncHealthItemType> Agents
        {
            get;
            set;
        } = [];
    }
}
