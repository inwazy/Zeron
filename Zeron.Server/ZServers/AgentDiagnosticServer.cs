// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Microsoft.EntityFrameworkCore;
using Zeron.Server.Data;
using Zeron.Server.Data.Entities;
using Zeron.Server.ZCore;
using Zeron.Server.ZCore.Type;
using Zeron.ZCore.Type;

namespace Zeron.Server.ZServers
{
    /// <summary>
    /// AgentDiagnosticServer
    /// </summary>
    public class AgentDiagnosticServer
    {
        // Database Context
        private readonly ZeronServerDbContext m_DbContext;

        // Settings
        private readonly ServerSettings m_Settings;

        /// <summary>
        /// AgentDiagnosticServer
        /// </summary>
        /// <param name="dbContext"></param>
        /// <param name="settings"></param>
        /// <returns>Returns void.</returns>
        public AgentDiagnosticServer(ZeronServerDbContext dbContext, ServerSettings settings)
        {
            m_DbContext = dbContext;
            m_Settings = settings;
        }

        /// <summary>
        /// GetDiagnosticsAsync
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns diagnostics list.</returns>
        public async Task<List<AgentDiagnosticType>> GetDiagnosticsAsync(CancellationToken cancellationToken = default)
        {
            List<AgentEntity> agents = await m_DbContext.Agents
                .OrderByDescending(agent => agent.LastHeartbeatAt)
                .ToListAsync(cancellationToken);

            HashSet<string> openOfflineAlerts = await m_DbContext.Alerts
                .Where(alert => alert.RuleType == AlertRuleType.AgentOffline
                    && alert.Status == AlertStatusesType.Open
                    && alert.AgentKey != null)
                .Select(alert => alert.AgentKey!)
                .ToHashSetAsync(cancellationToken);

            return agents.Select(agent => BuildDiagnostic(agent, openOfflineAlerts.Contains(agent.AgentKey))).ToList();
        }

        /// <summary>
        /// GetDiagnosticAsync
        /// </summary>
        /// <param name="agentKey"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns diagnostic or null.</returns>
        public async Task<AgentDiagnosticType?> GetDiagnosticAsync(
            string agentKey,
            CancellationToken cancellationToken = default)
        {
            AgentEntity? agent = await m_DbContext.Agents
                .FirstOrDefaultAsync(item => item.AgentKey == agentKey, cancellationToken);

            if (agent == null)
            {
                return null;
            }

            bool hasOpenAlert = await m_DbContext.Alerts.AnyAsync(
                alert => alert.RuleType == AlertRuleType.AgentOffline
                    && alert.Status == AlertStatusesType.Open
                    && alert.AgentKey == agentKey,
                cancellationToken);

            return BuildDiagnostic(agent, hasOpenAlert);
        }

        /// <summary>
        /// BuildDiagnostic
        /// </summary>
        /// <param name="agent"></param>
        /// <param name="hasOpenOfflineAlert"></param>
        /// <returns>Returns AgentDiagnosticType.</returns>
        private AgentDiagnosticType BuildDiagnostic(AgentEntity agent, bool hasOpenOfflineAlert)
        {
            int secondsSinceHeartbeat = agent.LastHeartbeatAt == default
                ? int.MaxValue
                : Math.Max(0, (int)(DateTime.UtcNow - ToUtc(agent.LastHeartbeatAt)).TotalSeconds);

            string connectionState;
            string diagnosticMessage;
            string recommendedAction;

            if (agent.Status == "disabled")
            {
                connectionState = "disabled";
                diagnosticMessage = "Agent is disabled by administrator.";
                recommendedAction = "Enable the agent from the agent detail page if it should reconnect.";
            }
            else if (agent.LastHeartbeatAt == default)
            {
                connectionState = "never_seen";
                diagnosticMessage = "Agent registered but no heartbeat has been received.";
                recommendedAction = "Start Zeron.Demand, set server_enabled=true, and verify server_url and server_api_key.";
            }
            else if (agent.Status == "offline" || secondsSinceHeartbeat >= m_Settings.HeartbeatTimeoutSeconds)
            {
                connectionState = "offline";
                diagnosticMessage = string.Format(
                    "No heartbeat for {0}s (timeout {1}s).",
                    secondsSinceHeartbeat == int.MaxValue ? "N/A" : secondsSinceHeartbeat.ToString(),
                    m_Settings.HeartbeatTimeoutSeconds);
                recommendedAction = "Check that Zeron.Demand is running and can reach the server HTTP endpoint.";
            }
            else if (secondsSinceHeartbeat >= m_Settings.HeartbeatTimeoutSeconds / 2)
            {
                connectionState = "stale";
                diagnosticMessage = string.Format(
                    "Heartbeat is aging ({0}s ago). Timeout occurs at {1}s.",
                    secondsSinceHeartbeat,
                    m_Settings.HeartbeatTimeoutSeconds);
                recommendedAction = "Monitor the agent service and network connectivity.";
            }
            else
            {
                connectionState = "healthy";
                diagnosticMessage = string.Format("Heartbeat received {0}s ago.", secondsSinceHeartbeat);
                recommendedAction = "No action required.";
            }

            if (hasOpenOfflineAlert)
            {
                diagnosticMessage += " Open offline alert exists.";
            }

            return new AgentDiagnosticType
            {
                AgentKey = agent.AgentKey,
                MachineName = agent.MachineName,
                Status = agent.Status,
                ConnectionState = connectionState,
                SecondsSinceLastHeartbeat = secondsSinceHeartbeat == int.MaxValue ? -1 : secondsSinceHeartbeat,
                HeartbeatTimeoutSeconds = m_Settings.HeartbeatTimeoutSeconds,
                LastHeartbeatAt = agent.LastHeartbeatAt == default ? null : ToUtc(agent.LastHeartbeatAt),
                HasOpenOfflineAlert = hasOpenOfflineAlert,
                DiagnosticMessage = diagnosticMessage,
                RecommendedAction = recommendedAction
            };
        }

        /// <summary>
        /// ToUtc
        /// </summary>
        /// <param name="value"></param>
        /// <returns>Returns UTC DateTime.</returns>
        private static DateTime ToUtc(DateTime value)
        {
            return value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
            };
        }
    }
}
