// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text.Json;
using Zeron.Server.Data;
using Zeron.Server.Data.Entities;
using Zeron.Server.ZInterfaces;
using Zeron.ZCore;
using Zeron.ZCore.Type;

namespace Zeron.Server.ZServers
{
    /// <summary>
    /// AgentManagerServer
    /// </summary>
    public class AgentManagerServer
    {
        // DbContext
        private readonly ZeronServerDbContext m_DbContext;

        // DashboardNotifier
        private readonly IDashboardNotifier? m_DashboardNotifier;

        // AlertRuleServer
        private readonly AlertRuleServer? m_AlertRuleServer;

        /// <summary>
        /// AgentManagerServer
        /// </summary>
        /// <param name="dbContext"></param>
        /// <param name="dashboardNotifier"></param>
        /// <param name="alertRuleServer"></param>
        /// <returns>Returns void.</returns>
        public AgentManagerServer(
            ZeronServerDbContext dbContext,
            IDashboardNotifier? dashboardNotifier = null,
            AlertRuleServer? alertRuleServer = null)
        {
            m_DbContext = dbContext;
            m_DashboardNotifier = dashboardNotifier;
            m_AlertRuleServer = alertRuleServer;
        }

        /// <summary>
        /// ProcessHeartbeatAsync
        /// </summary>
        /// <param name="request"></param>
        /// <param name="ipAddress"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns AgentHeartbeatResponseType.</returns>
        public async Task<AgentHeartbeatResponseType> ProcessHeartbeatAsync(
            AgentHeartbeatRequestType request,
            string? ipAddress,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.AgentId))
            {
                return new AgentHeartbeatResponseType { Success = false };
            }

            DateTime now = DateTime.UtcNow;
            AgentEntity? agent = await m_DbContext.Agents
                .FirstOrDefaultAsync(item => item.AgentKey == request.AgentId, cancellationToken);

            if (agent == null)
            {
                agent = new AgentEntity
                {
                    Id = Guid.NewGuid(),
                    AgentKey = request.AgentId,
                    RegisteredAt = now
                };

                m_DbContext.Agents.Add(agent);
            }

            bool wasOffline = agent.Status == "offline";

            agent.MachineName = request.MachineName;
            agent.IpAddress = ipAddress;
            agent.Version = request.Version;
            agent.Status = "online";
            agent.LastSeenAt = now;
            agent.LastHeartbeatAt = now;
            agent.SupportedEnginesJson = request.SupportedEngines == null || request.SupportedEngines.Count == 0
                ? agent.SupportedEnginesJson
                : JsonSerializer.Serialize(request.SupportedEngines);

            m_DbContext.AgentHeartbeats.Add(new AgentHeartbeatEntity
            {
                AgentId = agent.Id,
                ReportedAt = now,
                UptimeSeconds = request.UptimeSeconds,
                InstallQueueCount = request.InstallQueueCount,
                InstallRunning = request.InstallRunning,
                SchedulerTaskCount = request.SchedulerTaskCount
            });

            await m_DbContext.SaveChangesAsync(cancellationToken);

            ZNLogger.Common.Info(string.Format(CultureInfo.InvariantCulture,
                "AgentManagerServer heartbeat from {0} ({1}), status=online",
                agent.AgentKey,
                agent.MachineName));

            if (m_DashboardNotifier != null)
            {
                await m_DashboardNotifier.NotifyAgentStatusAsync(agent);
            }

            if (wasOffline && m_AlertRuleServer != null)
            {
                await m_AlertRuleServer.ResolveAgentOfflineAlertsAsync(agent.AgentKey, cancellationToken);
            }

            List<PendingTaskType> pendingTasks = await m_DbContext.TaskAssignments
                .Include(assignment => assignment.Task)
                .Where(assignment => assignment.AgentId == agent.Id && assignment.Status == "pending")
                .OrderBy(assignment => assignment.AssignedAt)
                .Select(assignment => new PendingTaskType
                {
                    AssignmentId = assignment.Id.ToString(),
                    TargetApi = assignment.Task!.TargetApi,
                    Command = assignment.Task.Command
                })
                .ToListAsync(cancellationToken);

            return new AgentHeartbeatResponseType
            {
                Success = true,
                ServerTime = now.ToString("o", CultureInfo.InvariantCulture),
                PendingTasks = pendingTasks
            };
        }

        /// <summary>
        /// GetAgentsAsync
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns agent list.</returns>
        public async Task<List<AgentEntity>> GetAgentsAsync(
            CancellationToken cancellationToken = default)
        {
            return await m_DbContext.Agents
                .OrderByDescending(agent => agent.LastHeartbeatAt)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// GetAgentByKeyAsync
        /// </summary>
        /// <param name="agentKey"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns agent or null.</returns>
        public async Task<AgentEntity?> GetAgentByKeyAsync(
            string agentKey, 
            CancellationToken cancellationToken = default)
        {
            return await m_DbContext.Agents
                .FirstOrDefaultAsync(agent => agent.AgentKey == agentKey, cancellationToken);
        }

        /// <summary>
        /// MarkOfflineAgentsAsync
        /// </summary>
        /// <param name="timeoutSeconds"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns affected count.</returns>
        public async Task<int> MarkOfflineAgentsAsync(
            int timeoutSeconds, 
            CancellationToken cancellationToken = default)
        {
            DateTime threshold = DateTime.UtcNow.AddSeconds(-timeoutSeconds);
            List<AgentEntity> staleAgents = await m_DbContext.Agents
                .Where(agent => agent.Status == "online" && agent.LastHeartbeatAt < threshold)
                .ToListAsync(cancellationToken);

            foreach (AgentEntity agent in staleAgents)
            {
                agent.Status = "offline";
            }

            if (staleAgents.Count > 0)
            {
                await m_DbContext.SaveChangesAsync(cancellationToken);
                ZNLogger.Common.Info(string.Format(CultureInfo.InvariantCulture,
                    "AgentManagerServer marked {0} agent(s) offline.", staleAgents.Count));

                if (m_DashboardNotifier != null)
                {
                    foreach (AgentEntity agent in staleAgents)
                    {
                        await m_DashboardNotifier.NotifyAgentStatusAsync(agent);
                    }
                }

                if (m_AlertRuleServer != null)
                {
                    await m_AlertRuleServer.ProcessAgentOfflineAsync(staleAgents, cancellationToken);
                }
            }

            return staleAgents.Count;
        }

        /// <summary>
        /// GetAgentHeartbeatsAsync
        /// </summary>
        /// <param name="agentKey"></param>
        /// <param name="limit"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns heartbeat history.</returns>
        public async Task<List<AgentHeartbeatEntity>> GetAgentHeartbeatsAsync(
            string agentKey,
            int limit,
            CancellationToken cancellationToken = default)
        {
            return await m_DbContext.AgentHeartbeats
                .Include(item => item.Agent)
                .Where(item => item.Agent!.AgentKey == agentKey)
                .OrderByDescending(item => item.ReportedAt)
                .Take(limit)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// UpdateAgentAsync
        /// </summary>
        /// <param name="agentKey"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns updated agent or null.</returns>
        public async Task<AgentEntity?> UpdateAgentAsync(
            string agentKey,
            AgentUpdateRequestType request,
            CancellationToken cancellationToken = default)
        {
            AgentEntity? agent = await m_DbContext.Agents
                .FirstOrDefaultAsync(item => item.AgentKey == agentKey, cancellationToken);

            if (agent == null)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                agent.Status = request.Status;
            }

            await m_DbContext.SaveChangesAsync(cancellationToken);

            if (m_DashboardNotifier != null)
            {
                await m_DashboardNotifier.NotifyAgentStatusAsync(agent);
            }

            return agent;
        }
    }
}
