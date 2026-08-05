// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Zeron.Server.Data.Entities;
using Zeron.Server.ZCore;
using Zeron.Server.ZCore.Type;
using Zeron.ZCore.Type;

namespace Zeron.Server.ZServers
{
    /// <summary>
    /// DashboardSummaryServer
    /// </summary>
    public class DashboardSummaryServer
    {
        // Service Aggregators
        private readonly AgentManagerServer m_AgentManager;

        // Service Diagnostics
        private readonly AgentDiagnosticServer m_AgentDiagnosticServer;

        // Service Dispatchers
        private readonly TaskDispatcherServer m_TaskDispatcher;

        // Service Ingestors
        private readonly EventIngestorServer m_EventIngestor;

        // Service Rules
        private readonly AlertRuleServer m_AlertRuleServer;

        // Settings
        private readonly ServerSettings m_Settings;

        /// <summary>
        /// DashboardSummaryServer
        /// </summary>
        /// <param name="agentManager"></param>
        /// <param name="agentDiagnosticServer"></param>
        /// <param name="taskDispatcher"></param>
        /// <param name="eventIngestor"></param>
        /// <param name="alertRuleServer"></param>
        /// <param name="settings"></param>
        /// <returns>Returns void.</returns>
        public DashboardSummaryServer(
            AgentManagerServer agentManager,
            AgentDiagnosticServer agentDiagnosticServer,
            TaskDispatcherServer taskDispatcher,
            EventIngestorServer eventIngestor,
            AlertRuleServer alertRuleServer,
            ServerSettings settings)
        {
            m_AgentManager = agentManager;
            m_AgentDiagnosticServer = agentDiagnosticServer;
            m_TaskDispatcher = taskDispatcher;
            m_EventIngestor = eventIngestor;
            m_AlertRuleServer = alertRuleServer;
            m_Settings = settings;
        }

        /// <summary>
        /// GetSummaryAsync
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns DashboardSummaryType.</returns>
        public async Task<DashboardSummaryType> GetSummaryAsync(
            CancellationToken cancellationToken = default)
        {
            List<AgentEntity> agents = await m_AgentManager.GetAgentsAsync(cancellationToken);
            List<AgentDiagnosticType> diagnostics = await m_AgentDiagnosticServer.GetDiagnosticsAsync(cancellationToken);
            Dictionary<string, AgentDiagnosticType> diagnosticMap = diagnostics
                .Where(item => !string.IsNullOrWhiteSpace(item.AgentKey))
                .ToDictionary(item => item.AgentKey!, StringComparer.OrdinalIgnoreCase);

            List<TaskEntity> tasks = await m_TaskDispatcher.GetTasksAsync(cancellationToken);
            List<AlertEntity> openAlerts = await m_AlertRuleServer.GetAlertsAsync(
                AlertStatusesType.Open,
                8,
                cancellationToken);
            List<EventEntity> events = await m_EventIngestor.GetEventsAsync(null, null, 8, cancellationToken);
            int openAlertCount = await m_AlertRuleServer.GetOpenAlertCountAsync(cancellationToken);

            return new DashboardSummaryType
            {
                AgentsOnline = agents.Count(agent => agent.Status == "online"),
                AgentsOffline = agents.Count(agent => agent.Status == "offline"),
                AgentsDisabled = agents.Count(agent => agent.Status == "disabled"),
                AgentsTotal = agents.Count,
                AgentsStale = diagnostics.Count(item => item.ConnectionState == "stale"),
                ActiveTasks = tasks.Count(task => task.Status is "pending" or "running" or "dispatched"),
                OpenAlerts = openAlertCount,
                RecentAgents = agents
                    .Take(8)
                    .Select(agent =>
                    {
                        diagnosticMap.TryGetValue(agent.AgentKey, out AgentDiagnosticType? diagnostic);

                        return new DashboardAgentItemType
                        {
                            AgentKey = agent.AgentKey,
                            MachineName = agent.MachineName,
                            Status = agent.Status,
                            ConnectionState = diagnostic?.ConnectionState ?? agent.Status,
                            LastHeartbeatAt = agent.LastHeartbeatAt == default ? null : agent.LastHeartbeatAt
                        };
                    })
                    .ToList(),
                RecentTasks = tasks
                    .OrderByDescending(task => task.CreatedAt)
                    .Take(8)
                    .Select(task => new DashboardTaskItemType
                    {
                        Id = task.Id,
                        Name = task.Name,
                        TargetApi = task.TargetApi,
                        Status = task.Status,
                        CreatedAt = task.CreatedAt,
                        AssignmentCount = task.Assignments?.Count ?? 0
                    })
                    .ToList(),
                RecentAlerts = openAlerts
                    .Select(alert => new DashboardAlertItemType
                    {
                        Id = alert.Id,
                        Title = alert.Title,
                        Message = alert.Message,
                        Severity = alert.Severity,
                        Status = alert.Status,
                        AgentKey = alert.AgentKey,
                        CreatedAt = alert.CreatedAt
                    })
                    .ToList(),
                RecentEvents = events
                    .Select(evt => new DashboardEventItemType
                    {
                        Id = evt.Id,
                        AgentKey = evt.Agent?.AgentKey,
                        Topic = evt.Topic,
                        Payload = evt.Payload,
                        ReceivedAt = evt.ReceivedAt
                    })
                    .ToList(),
                Security = BuildSecurityStatus(),
                GeneratedAtUtc = DateTime.UtcNow
            };
        }

        /// <summary>
        /// BuildSecurityStatus
        /// </summary>
        /// <returns>Returns DashboardSecurityStatusType.</returns>
        public static DashboardSecurityStatusType BuildSecurityStatus(
            ServerSettings settings)
        {
            bool curvePublicKeyPresent = !string.IsNullOrWhiteSpace(settings.CurvePublicKeyPath)
                && File.Exists(settings.CurvePublicKeyPath);

            string overallStatus;
            List<string> recommendations = [];

            if (settings.CurveEnabled && settings.AgentHmacRequired)
            {
                overallStatus = "hardened";
            }
            else if (settings.CurveEnabled || settings.AgentHmacRequired)
            {
                overallStatus = "partial";
            }
            else
            {
                overallStatus = "insecure";
            }

            if (!settings.CurveEnabled)
            {
                recommendations.Add("Enable Zeron:CurveEnabled and distribute curve-server.public to agents (zmq_sub_curve_enabled).");
            }
            else if (!curvePublicKeyPresent)
            {
                recommendations.Add("CURVE is enabled but the public key file is missing — restart the server to generate keys, then copy the .public file to agents.");
            }

            if (!settings.AgentHmacRequired)
            {
                recommendations.Add("Enable Zeron:AgentHmacRequired and set agent server_hmac_enabled=true.");
            }

            if (!settings.RequireHttpsAgents)
            {
                recommendations.Add("Prefer HTTPS (reverse proxy) and set Zeron:RequireHttpsAgents=true when agents call HTTPS or X-Forwarded-Proto is set.");
            }

            return new DashboardSecurityStatusType
            {
                CurveEnabled = settings.CurveEnabled,
                CurvePublicKeyPresent = curvePublicKeyPresent,
                AgentHmacRequired = settings.AgentHmacRequired,
                RequireHttpsAgents = settings.RequireHttpsAgents,
                OverallStatus = overallStatus,
                Recommendations = recommendations
            };
        }

        /// <summary>
        /// BuildSecurityStatus
        /// </summary>
        /// <returns>Returns DashboardSecurityStatusType.</returns>
        private DashboardSecurityStatusType BuildSecurityStatus()
        {
            return BuildSecurityStatus(m_Settings);
        }
    }
}
