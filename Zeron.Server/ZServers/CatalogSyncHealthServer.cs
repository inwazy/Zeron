// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text.Json;
using Zeron.Server.Data;
using Zeron.Server.Data.Entities;
using Zeron.Server.ZCore;
using Zeron.ZCore;
using Zeron.ZCore.Type;

namespace Zeron.Server.ZServers
{
    /// <summary>
    /// CatalogSyncHealthServer - ManagedPackage catalog sync health and push.
    /// </summary>
    public class CatalogSyncHealthServer
    {
        // Database context.
        private readonly ZeronServerDbContext m_DbContext;

        // Catalog server (push sync).
        private readonly ManagedPackageCatalogServer m_CatalogServer;

        // Settings.
        private readonly ServerSettings m_Settings;

        // Optional audit.
        private readonly AuditLogServer? m_AuditLogServer;

        /// <summary>
        /// CatalogSyncHealthServer
        /// </summary>
        /// <param name="dbContext"></param>
        /// <param name="catalogServer"></param>
        /// <param name="settings"></param>
        /// <param name="auditLogServer"></param>
        /// <returns>Returns void.</returns>
        public CatalogSyncHealthServer(
            ZeronServerDbContext dbContext,
            ManagedPackageCatalogServer catalogServer,
            ServerSettings settings,
            AuditLogServer? auditLogServer = null)
        {
            m_DbContext = dbContext;
            m_CatalogServer = catalogServer;
            m_Settings = settings;
            m_AuditLogServer = auditLogServer;
        }

        /// <summary>
        /// GetHealthAsync
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns sync health summary.</returns>
        public async Task<CatalogSyncHealthSummaryType> GetHealthAsync(
            CancellationToken cancellationToken = default)
        {
            int staleMinutes = Math.Max(1, m_Settings.CatalogSyncStaleMinutes);
            DateTime now = DateTime.UtcNow;
            DateTime staleBefore = now.AddMinutes(-staleMinutes);
            DateTime failedLookback = now.AddHours(-24);

            List<AgentEntity> agents = await m_DbContext.Agents
                .AsNoTracking()
                .OrderBy(agent => agent.MachineName)
                .ThenBy(agent => agent.AgentKey)
                .ToListAsync(cancellationToken);

            Dictionary<string, DateTime> recentFailures = await LoadRecentFailuresAsync(
                failedLookback,
                cancellationToken);

            List<CatalogSyncHealthItemType> items = [];

            foreach (AgentEntity agent in agents)
            {
                items.Add(BuildItem(agent, now, staleBefore, recentFailures));
            }

            return new CatalogSyncHealthSummaryType
            {
                Healthy = items.Count(item => item.SyncState == "healthy"),
                Stale = items.Count(item => item.SyncState == "stale"),
                NeverSynced = items.Count(item => item.SyncState == "never"),
                Offline = items.Count(item => item.SyncState == "offline"),
                RecentlyFailed = items.Count(item => item.SyncState == "failed"),
                StaleThresholdMinutes = staleMinutes,
                GeneratedAtUtc = now,
                Agents = items
            };
        }

        /// <summary>
        /// PushSyncAsync
        /// </summary>
        /// <param name="request"></param>
        /// <param name="actor"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns push response.</returns>
        public async Task<CatalogSyncPushResponseType> PushSyncAsync(
            CatalogSyncPushRequestType? request = null,
            AuditActorType? actor = null,
            CancellationToken cancellationToken = default)
        {
            request ??= new CatalogSyncPushRequestType();
            List<string> targets;

            if (request.AgentKeys != null && request.AgentKeys.Count > 0)
            {
                targets = await m_CatalogServer.RequestCatalogSyncAsync(request.AgentKeys, cancellationToken);
            }
            else if (request.OnlyUnhealthy)
            {
                CatalogSyncHealthSummaryType health = await GetHealthAsync(cancellationToken);
                List<string> unhealthyOnline = health.Agents
                    .Where(item => item.Status == "online"
                        && item.SyncState is "stale" or "never" or "failed"
                        && !string.IsNullOrWhiteSpace(item.AgentKey))
                    .Select(item => item.AgentKey!)
                    .ToList();

                targets = await m_CatalogServer.RequestCatalogSyncAsync(unhealthyOnline, cancellationToken);
            }
            else
            {
                targets = await m_CatalogServer.RequestCatalogSyncAsync(null, cancellationToken);
            }

            if (m_AuditLogServer != null && actor != null)
            {
                await m_AuditLogServer.WriteAsync(
                    AuditActions.CatalogSyncPush,
                    true,
                    $"Pushed catalog sync to {targets.Count} agent(s).",
                    actor,
                    targetType: "catalog",
                    targetKey: "sync-push",
                    details: new
                    {
                        targets.Count,
                        targets,
                        request.OnlyUnhealthy,
                        requested = request.AgentKeys
                    },
                    cancellationToken: cancellationToken);
            }

            ZNLogger.Common.Info(string.Format(CultureInfo.InvariantCulture,
                "CatalogSyncHealthServer pushed sync to {0} agent(s).", targets.Count));

            return new CatalogSyncPushResponseType
            {
                Success = true,
                Message = targets.Count == 0
                    ? "No online agents matched the push criteria."
                    : $"Catalog sync requested on {targets.Count} agent(s).",
                PushedCount = targets.Count,
                AgentKeys = targets
            };
        }

        /// <summary>
        /// BuildItem
        /// </summary>
        private static CatalogSyncHealthItemType BuildItem(
            AgentEntity agent,
            DateTime now,
            DateTime staleBefore,
            Dictionary<string, DateTime> recentFailures)
        {
            recentFailures.TryGetValue(agent.AgentKey, out DateTime failedAt);
            DateTime? lastFailed = failedAt == default ? null : failedAt;
            int? ageMinutes = agent.LastCatalogSyncAt.HasValue
                ? (int)Math.Max(0, (now - agent.LastCatalogSyncAt.Value).TotalMinutes)
                : null;

            string syncState;
            string message;

            if (string.Equals(agent.Status, "offline", StringComparison.OrdinalIgnoreCase)
                || string.Equals(agent.Status, "disabled", StringComparison.OrdinalIgnoreCase))
            {
                syncState = "offline";
                message = agent.Status == "disabled"
                    ? "Agent is disabled."
                    : "Agent is offline; sync cannot be pushed until it reconnects.";
            }
            else if (lastFailed.HasValue
                && (!agent.LastCatalogSyncAt.HasValue || lastFailed > agent.LastCatalogSyncAt))
            {
                syncState = "failed";
                message = string.Format(CultureInfo.InvariantCulture,
                    "Last reported catalog sync failed at {0:u}.",
                    lastFailed.Value);
            }
            else if (!agent.LastCatalogSyncAt.HasValue)
            {
                syncState = "never";
                message = "No successful catalog sync reported yet.";
            }
            else if (agent.LastCatalogSyncAt.Value < staleBefore)
            {
                syncState = "stale";
                message = string.Format(CultureInfo.InvariantCulture,
                    "Last sync was {0} minute(s) ago.",
                    ageMinutes);
            }
            else
            {
                syncState = "healthy";
                message = string.Format(CultureInfo.InvariantCulture,
                    "Synced {0} minute(s) ago.",
                    ageMinutes);
            }

            return new CatalogSyncHealthItemType
            {
                AgentKey = agent.AgentKey,
                MachineName = agent.MachineName,
                Status = agent.Status,
                LastCatalogSyncAt = agent.LastCatalogSyncAt,
                LastHeartbeatAt = agent.LastHeartbeatAt == default ? null : agent.LastHeartbeatAt,
                SyncState = syncState,
                DiagnosticMessage = message,
                LastFailedSyncAt = lastFailed,
                AgeMinutes = ageMinutes
            };
        }

        /// <summary>
        /// LoadRecentFailuresAsync - map agentKey -> latest failed sync time from audit.
        /// </summary>
        private async Task<Dictionary<string, DateTime>> LoadRecentFailuresAsync(
            DateTime sinceUtc,
            CancellationToken cancellationToken)
        {
            List<AuditLogEntity> rows = await m_DbContext.AuditLogs
                .AsNoTracking()
                .Where(log => log.Action == AuditActions.PackageCatalogSync
                    && !log.Success
                    && log.OccurredAt >= sinceUtc)
                .OrderByDescending(log => log.OccurredAt)
                .Take(500)
                .ToListAsync(cancellationToken);

            Dictionary<string, DateTime> map = new(StringComparer.OrdinalIgnoreCase);

            foreach (AuditLogEntity row in rows)
            {
                string? agentKey = ExtractAgentKey(row);

                if (string.IsNullOrWhiteSpace(agentKey) || map.ContainsKey(agentKey))
                {
                    continue;
                }

                map[agentKey] = row.OccurredAt;
            }

            return map;
        }

        /// <summary>
        /// ExtractAgentKey
        /// </summary>
        private static string? ExtractAgentKey(
            AuditLogEntity row)
        {
            if (!string.IsNullOrWhiteSpace(row.ActorUsername)
                && string.Equals(row.Source, "agent", StringComparison.OrdinalIgnoreCase))
            {
                return row.ActorUsername;
            }

            if (!string.IsNullOrWhiteSpace(row.TargetKey))
            {
                int at = row.TargetKey.LastIndexOf('@');

                if (at >= 0 && at < row.TargetKey.Length - 1)
                {
                    return row.TargetKey[(at + 1)..];
                }

                if (string.Equals(row.TargetType, "agent", StringComparison.OrdinalIgnoreCase))
                {
                    return row.TargetKey;
                }
            }

            if (!string.IsNullOrWhiteSpace(row.DetailsJson))
            {
                try
                {
                    using JsonDocument document = JsonDocument.Parse(row.DetailsJson);

                    if (document.RootElement.TryGetProperty("AgentKey", out JsonElement agentKey)
                        || document.RootElement.TryGetProperty("agentKey", out agentKey))
                    {
                        return agentKey.GetString();
                    }
                }
                catch (JsonException)
                {
                }
            }

            return null;
        }
    }
}
