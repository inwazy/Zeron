// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Microsoft.EntityFrameworkCore;
using System.Globalization;
using Zeron.Server.Data;
using Zeron.Server.Data.Entities;
using Zeron.Server.ZCore.Type;
using Zeron.ZCore;

namespace Zeron.Server.ZServers
{
    /// <summary>
    /// AlertRuleServer
    /// </summary>
    public class AlertRuleServer
    {
        // Database Context
        private readonly ZeronServerDbContext m_DbContext;

        // Alert Notifier
        private readonly AlertNotifierServer m_AlertNotifier;

        /// <summary>
        /// AlertRuleServer
        /// </summary>
        /// <param name="dbContext"></param>
        /// <param name="alertNotifier"></param>
        /// <returns>Returns void.</returns>
        public AlertRuleServer(
            ZeronServerDbContext dbContext, 
            AlertNotifierServer alertNotifier)
        {
            m_DbContext = dbContext;
            m_AlertNotifier = alertNotifier;
        }

        /// <summary>
        /// ProcessAgentOfflineAsync
        /// </summary>
        /// <param name="agents"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns created alert count.</returns>
        public async Task<int> ProcessAgentOfflineAsync(
            IEnumerable<AgentEntity> agents,
            CancellationToken cancellationToken = default)
        {
            int created = 0;

            foreach (AgentEntity agent in agents)
            {
                bool hasOpenAlert = await m_DbContext.Alerts.AnyAsync(
                    alert => alert.RuleType == AlertRuleType.AgentOffline
                        && alert.AgentKey == agent.AgentKey
                        && alert.Status == AlertStatusesType.Open,
                    cancellationToken);

                if (hasOpenAlert)
                {
                    continue;
                }

                DateTime now = DateTime.UtcNow;
                AlertEntity alert = new()
                {
                    Id = Guid.NewGuid(),
                    RuleType = AlertRuleType.AgentOffline,
                    AgentKey = agent.AgentKey,
                    AgentId = agent.Id,
                    Title = "Agent offline: " + agent.AgentKey,
                    Message = string.Format(CultureInfo.InvariantCulture,
                        "Agent '{0}' ({1}) missed heartbeat and was marked offline. Last heartbeat: {2:u}",
                        agent.AgentKey,
                        agent.MachineName ?? "unknown",
                        agent.LastHeartbeatAt),
                    Severity = AlertSeveritiesType.Warning,
                    Status = AlertStatusesType.Open,
                    CreatedAt = now
                };

                m_DbContext.Alerts.Add(alert);
                await m_DbContext.SaveChangesAsync(cancellationToken);

                await m_AlertNotifier.NotifyAsync(alert);

                if (alert.NotifiedAt.HasValue)
                {
                    await m_DbContext.SaveChangesAsync(cancellationToken);
                }

                created++;

                ZNLogger.Common.Info(string.Format(CultureInfo.InvariantCulture,
                    "AlertRuleServer created offline alert for agent {0}", agent.AgentKey));
            }

            return created;
        }

        /// <summary>
        /// ResolveAgentOfflineAlertsAsync
        /// </summary>
        /// <param name="agentKey"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns resolved count.</returns>
        public async Task<int> ResolveAgentOfflineAlertsAsync(
            string agentKey,
            CancellationToken cancellationToken = default)
        {
            List<AlertEntity> openAlerts = await m_DbContext.Alerts
                .Where(alert => alert.RuleType == AlertRuleType.AgentOffline
                    && alert.AgentKey == agentKey
                    && alert.Status == AlertStatusesType.Open)
                .ToListAsync(cancellationToken);

            if (openAlerts.Count == 0)
            {
                return 0;
            }

            DateTime now = DateTime.UtcNow;

            foreach (AlertEntity alert in openAlerts)
            {
                alert.Status = AlertStatusesType.Resolved;
                alert.ResolvedAt = now;
            }

            await m_DbContext.SaveChangesAsync(cancellationToken);

            ZNLogger.Common.Info(string.Format(CultureInfo.InvariantCulture,
                "AlertRuleServer resolved {0} offline alert(s) for agent {1}", openAlerts.Count, agentKey));

            return openAlerts.Count;
        }

        /// <summary>
        /// GetAlertsAsync
        /// </summary>
        /// <param name="status"></param>
        /// <param name="limit"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns alerts.</returns>
        public async Task<List<AlertEntity>> GetAlertsAsync(
            string? status,
            int limit,
            CancellationToken cancellationToken = default)
        {
            return await GetAlertsAsync(status, limit, 0, cancellationToken);
        }

        /// <summary>
        /// GetAlertsAsync
        /// </summary>
        /// <param name="status"></param>
        /// <param name="limit"></param>
        /// <param name="offset"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns alerts.</returns>
        public async Task<List<AlertEntity>> GetAlertsAsync(
            string? status,
            int limit,
            int offset,
            CancellationToken cancellationToken = default)
        {
            int take = Math.Clamp(limit, 1, 500);
            int skip = Math.Max(0, offset);
            IQueryable<AlertEntity> query = m_DbContext.Alerts.AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(alert => alert.Status == status);
            }

            return await query
                .OrderByDescending(alert => alert.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// GetOpenAlertCountAsync
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns count.</returns>
        public async Task<int> GetOpenAlertCountAsync(
            CancellationToken cancellationToken = default)
        {
            return await m_DbContext.Alerts
                .CountAsync(alert => alert.Status == AlertStatusesType.Open, cancellationToken);
        }

        /// <summary>
        /// AcknowledgeAlertAsync
        /// </summary>
        /// <param name="alertId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns bool.</returns>
        public async Task<bool> AcknowledgeAlertAsync(
            Guid alertId, 
            CancellationToken cancellationToken = default)
        {
            AlertEntity? alert = await m_DbContext.Alerts
                .FirstOrDefaultAsync(item => item.Id == alertId, cancellationToken);

            if (alert == null || alert.Status != AlertStatusesType.Open)
            {
                return false;
            }

            alert.Status = AlertStatusesType.Acknowledged;
            await m_DbContext.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}
