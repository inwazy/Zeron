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
