// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Microsoft.EntityFrameworkCore;
using System.Globalization;
using Zeron.Server.Data;
using Zeron.Server.Data.Entities;
using Zeron.ZCore;
using Zeron.ZCore.Type;

namespace Zeron.Server.ZServers
{
    /// <summary>
    /// AgentManagerServer
    /// </summary>
    public class AgentManagerServer
    {
        private readonly ZeronServerDbContext m_DbContext;

        /// <summary>
        /// AgentManagerServer
        /// </summary>
        /// <param name="dbContext"></param>
        /// <returns>Returns void.</returns>
        public AgentManagerServer(ZeronServerDbContext dbContext)
        {
            m_DbContext = dbContext;
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

            agent.MachineName = request.MachineName;
            agent.IpAddress = ipAddress;
            agent.Version = request.Version;
            agent.Status = "online";
            agent.LastSeenAt = now;
            agent.LastHeartbeatAt = now;

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
        public async Task<List<AgentEntity>> GetAgentsAsync(CancellationToken cancellationToken = default)
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
        public async Task<AgentEntity?> GetAgentByKeyAsync(string agentKey, CancellationToken cancellationToken = default)
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
        public async Task<int> MarkOfflineAgentsAsync(int timeoutSeconds, CancellationToken cancellationToken = default)
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
            }

            return staleAgents.Count;
        }
    }
}
