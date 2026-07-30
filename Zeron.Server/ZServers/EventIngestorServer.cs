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
    /// EventIngestorServer
    /// </summary>
    public class EventIngestorServer
    {
        private readonly ZeronServerDbContext m_DbContext;

        /// <summary>
        /// EventIngestorServer
        /// </summary>
        /// <param name="dbContext"></param>
        /// <returns>Returns void.</returns>
        public EventIngestorServer(ZeronServerDbContext dbContext)
        {
            m_DbContext = dbContext;
        }

        /// <summary>
        /// IngestEventAsync
        /// </summary>
        /// <param name="report"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns bool.</returns>
        public async Task<bool> IngestEventAsync(AgentEventReportType report, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(report.AgentId) || string.IsNullOrWhiteSpace(report.Topic))
            {
                return false;
            }

            AgentEntity? agent = await m_DbContext.Agents
                .FirstOrDefaultAsync(item => item.AgentKey == report.AgentId, cancellationToken);

            if (agent == null)
            {
                return false;
            }

            m_DbContext.Events.Add(new EventEntity
            {
                AgentId = agent.Id,
                Topic = report.Topic,
                Payload = report.Payload ?? "",
                ReceivedAt = DateTime.UtcNow
            });

            agent.LastSeenAt = DateTime.UtcNow;

            await m_DbContext.SaveChangesAsync(cancellationToken);

            return true;
        }

        /// <summary>
        /// GetEventsAsync
        /// </summary>
        /// <param name="agentKey"></param>
        /// <param name="topic"></param>
        /// <param name="limit"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns events.</returns>
        public async Task<List<EventEntity>> GetEventsAsync(
            string? agentKey,
            string? topic,
            int limit,
            CancellationToken cancellationToken = default)
        {
            IQueryable<EventEntity> query = m_DbContext.Events
                .Include(evt => evt.Agent)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(agentKey))
            {
                query = query.Where(evt => evt.Agent!.AgentKey == agentKey);
            }

            if (!string.IsNullOrWhiteSpace(topic))
            {
                query = query.Where(evt => evt.Topic.Contains(topic));
            }

            return await query
                .OrderByDescending(evt => evt.ReceivedAt)
                .Take(limit)
                .ToListAsync(cancellationToken);
        }
    }
}
