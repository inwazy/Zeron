// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.ZCore.Type
{
    /// <summary>
    /// DashboardSummaryType
    /// </summary>
    public sealed class DashboardSummaryType
    {
        /// <summary>
        /// AgentsOnline
        /// </summary>
        public int AgentsOnline
        {
            get;
            set;
        }

        /// <summary>
        /// AgentsOffline
        /// </summary>
        public int AgentsOffline
        {
            get;
            set;
        }

        /// <summary>
        /// AgentsDisabled
        /// </summary>
        public int AgentsDisabled
        {
            get;
            set;
        }

        /// <summary>
        /// AgentsTotal
        /// </summary>
        public int AgentsTotal
        {
            get;
            set;
        }

        /// <summary>
        /// AgentsStale
        /// </summary>
        public int AgentsStale
        {
            get;
            set;
        }

        /// <summary>
        /// ActiveTasks
        /// </summary>
        public int ActiveTasks
        {
            get;
            set;
        }

        /// <summary>
        /// OpenAlerts
        /// </summary>
        public int OpenAlerts
        {
            get;
            set;
        }

        /// <summary>
        /// CatalogSyncHealthy - online agents with a recent successful catalog sync.
        /// </summary>
        public int CatalogSyncHealthy
        {
            get;
            set;
        }

        /// <summary>
        /// CatalogSyncUnhealthy - stale, never-synced, or recently failed (excludes offline).
        /// </summary>
        public int CatalogSyncUnhealthy
        {
            get;
            set;
        }

        /// <summary>
        /// CatalogSyncOffline
        /// </summary>
        public int CatalogSyncOffline
        {
            get;
            set;
        }

        /// <summary>
        /// UnreadInstallNotifications - unread DeviceOwner install-result tips.
        /// </summary>
        public int UnreadInstallNotifications
        {
            get;
            set;
        }

        /// <summary>
        /// UnreadInstallFailures - unread install-result tips that failed.
        /// </summary>
        public int UnreadInstallFailures
        {
            get;
            set;
        }

        /// <summary>
        /// RecentAgents
        /// </summary>
        public List<DashboardAgentItemType> RecentAgents
        {
            get;
            set;
        } = [];

        /// <summary>
        /// RecentTasks
        /// </summary>
        public List<DashboardTaskItemType> RecentTasks
        {
            get;
            set;
        } = [];

        /// <summary>
        /// RecentAlerts
        /// </summary>
        public List<DashboardAlertItemType> RecentAlerts
        {
            get;
            set;
        } = [];

        /// <summary>
        /// RecentEvents
        /// </summary>
        public List<DashboardEventItemType> RecentEvents
        {
            get;
            set;
        } = [];

        /// <summary>
        /// Security
        /// </summary>
        public DashboardSecurityStatusType Security
        {
            get;
            set;
        } = new();

        /// <summary>
        /// GeneratedAtUtc
        /// </summary>
        public DateTime GeneratedAtUtc
        {
            get;
            set;
        }
    }
}
