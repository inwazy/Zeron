// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Microsoft.EntityFrameworkCore;
using System.Globalization;
using Zeron.Server.Data;
using Zeron.Server.Data.Entities;
using Zeron.Server.ZCore;
using Zeron.ZCore;
using Zeron.ZCore.Type;

namespace Zeron.Server.ZServers
{
    /// <summary>
    /// DataRetentionServer - prune old audit logs, notifications, and catalog versions.
    /// </summary>
    public class DataRetentionServer
    {
        // Batch size per delete round.
        private const int BatchSize = 500;

        // Database context.
        private readonly ZeronServerDbContext m_DbContext;

        // Settings.
        private readonly ServerSettings m_Settings;

        /// <summary>
        /// DataRetentionServer
        /// </summary>
        /// <param name="dbContext"></param>
        /// <param name="settings"></param>
        /// <returns>Returns void.</returns>
        public DataRetentionServer(
            ZeronServerDbContext dbContext,
            ServerSettings settings)
        {
            m_DbContext = dbContext;
            m_Settings = settings;
        }

        /// <summary>
        /// PruneAsync
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns deletion counts.</returns>
        public async Task<DataRetentionResultType> PruneAsync(
            CancellationToken cancellationToken = default)
        {
            if (!m_Settings.RetentionEnabled)
            {
                return new DataRetentionResultType { Skipped = true };
            }

            DataRetentionResultType result = new()
            {
                AuditLogsDeleted = await PruneAuditLogsAsync(cancellationToken),
                NotificationsDeleted = await PruneNotificationsAsync(cancellationToken),
                CatalogVersionsDeleted = await PruneCatalogVersionsAsync(cancellationToken)
            };

            if (result.AuditLogsDeleted + result.NotificationsDeleted + result.CatalogVersionsDeleted > 0)
            {
                ZNLogger.Common.Info(string.Format(CultureInfo.InvariantCulture,
                    "DataRetentionServer pruned audit={0} notifications={1} catalogVersions={2}.",
                    result.AuditLogsDeleted,
                    result.NotificationsDeleted,
                    result.CatalogVersionsDeleted));
            }

            return result;
        }

        /// <summary>
        /// PruneAuditLogsAsync
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns deleted count.</returns>
        private async Task<int> PruneAuditLogsAsync(
            CancellationToken cancellationToken)
        {
            int days = m_Settings.AuditLogRetentionDays;

            if (days <= 0)
            {
                return 0;
            }

            DateTime cutoff = DateTime.UtcNow.AddDays(-days);

            return await DeleteInBatchesAsync(
                () => m_DbContext.AuditLogs
                    .Where(item => item.OccurredAt < cutoff)
                    .OrderBy(item => item.OccurredAt)
                    .Take(BatchSize)
                    .ToListAsync(cancellationToken),
                cancellationToken);
        }

        /// <summary>
        /// PruneNotificationsAsync
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns deleted count.</returns>
        private async Task<int> PruneNotificationsAsync(
            CancellationToken cancellationToken)
        {
            int days = m_Settings.UserNotificationRetentionDays;

            if (days <= 0)
            {
                return 0;
            }

            DateTime cutoff = DateTime.UtcNow.AddDays(-days);

            return await DeleteInBatchesAsync(
                () => m_DbContext.UserNotifications
                    .Where(item => item.CreatedAt < cutoff)
                    .OrderBy(item => item.CreatedAt)
                    .Take(BatchSize)
                    .ToListAsync(cancellationToken),
                cancellationToken);
        }

        /// <summary>
        /// PruneCatalogVersionsAsync - keep the newest N snapshots per package.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns deleted count.</returns>
        private async Task<int> PruneCatalogVersionsAsync(
            CancellationToken cancellationToken)
        {
            int keepCount = m_Settings.CatalogVersionKeepCount;

            if (keepCount <= 0)
            {
                return 0;
            }

            List<Guid> packageIds = await m_DbContext.ManagedPackageVersions
                .Select(item => item.PackageId)
                .Distinct()
                .ToListAsync(cancellationToken);

            int deleted = 0;

            foreach (Guid packageId in packageIds)
            {
                List<ManagedPackageVersionEntity> extra = await m_DbContext.ManagedPackageVersions
                    .Where(item => item.PackageId == packageId)
                    .OrderByDescending(item => item.VersionNumber)
                    .Skip(keepCount)
                    .ToListAsync(cancellationToken);

                if (extra.Count == 0)
                {
                    continue;
                }

                m_DbContext.ManagedPackageVersions.RemoveRange(extra);
                await m_DbContext.SaveChangesAsync(cancellationToken);
                deleted += extra.Count;
            }

            return deleted;
        }

        /// <summary>
        /// DeleteInBatchesAsync
        /// </summary>
        /// <param name="loadBatchAsync"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns deleted count.</returns>
        private async Task<int> DeleteInBatchesAsync<TEntity>(
            Func<Task<List<TEntity>>> loadBatchAsync,
            CancellationToken cancellationToken)
            where TEntity : class
        {
            int deleted = 0;

            while (!cancellationToken.IsCancellationRequested)
            {
                List<TEntity> batch = await loadBatchAsync();

                if (batch.Count == 0)
                {
                    break;
                }

                m_DbContext.Set<TEntity>().RemoveRange(batch);
                await m_DbContext.SaveChangesAsync(cancellationToken);
                deleted += batch.Count;
            }

            return deleted;
        }
    }
}
