// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.ZCore.Type
{
    /// <summary>
    /// DashboardSummaryType
    /// </summary>
    public class DashboardSummaryType
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

    /// <summary>
    /// DashboardSecurityStatusType - server transport security posture for Dashboard.
    /// </summary>
    public class DashboardSecurityStatusType
    {
        /// <summary>
        /// CurveEnabled
        /// </summary>
        public bool CurveEnabled
        {
            get;
            set;
        }

        /// <summary>
        /// CurvePublicKeyPresent
        /// </summary>
        public bool CurvePublicKeyPresent
        {
            get;
            set;
        }

        /// <summary>
        /// AgentHmacRequired
        /// </summary>
        public bool AgentHmacRequired
        {
            get;
            set;
        }

        /// <summary>
        /// RequireHttpsAgents
        /// </summary>
        public bool RequireHttpsAgents
        {
            get;
            set;
        }

        /// <summary>
        /// OverallStatus - hardened | partial | insecure
        /// </summary>
        public string OverallStatus
        {
            get;
            set;
        } = "insecure";

        /// <summary>
        /// Recommendations
        /// </summary>
        public List<string> Recommendations
        {
            get;
            set;
        } = [];
    }

    /// <summary>
    /// DashboardAgentItemType
    /// </summary>
    public class DashboardAgentItemType
    {
        /// <summary>
        /// AgentKey
        /// </summary>
        public string AgentKey
        {
            get;
            set;
        } = "";

        /// <summary>
        /// MachineName
        /// </summary>
        public string? MachineName
        {
            get;
            set;
        }

        /// <summary>
        /// Status
        /// </summary>
        public string Status
        {
            get;
            set;
        } = "";

        /// <summary>
        /// ConnectionState
        /// </summary>
        public string ConnectionState
        {
            get;
            set;
        } = "";

        /// <summary>
        /// LastHeartbeatAt
        /// </summary>
        public DateTime? LastHeartbeatAt
        {
            get;
            set;
        }
    }

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

    /// <summary>
    /// DashboardAlertItemType
    /// </summary>
    public class DashboardAlertItemType
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
