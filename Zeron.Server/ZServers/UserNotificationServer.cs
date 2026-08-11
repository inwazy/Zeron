// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Microsoft.EntityFrameworkCore;
using Zeron.Server.Data;
using Zeron.Server.Data.Entities;
using Zeron.ZCore.Type;

namespace Zeron.Server.ZServers
{
    /// <summary>
    /// UserNotificationServer - per-user dashboard notifications.
    /// </summary>
    public class UserNotificationServer
    {
        // Kind for self-service install results.
        public const string KindInstallResult = "install.result";

        // Database context.
        private readonly ZeronServerDbContext m_DbContext;

        /// <summary>
        /// UserNotificationServer
        /// </summary>
        /// <param name="dbContext"></param>
        /// <returns>Returns void.</returns>
        public UserNotificationServer(
            ZeronServerDbContext dbContext)
        {
            m_DbContext = dbContext;
        }

        /// <summary>
        /// GetNotificationsAsync
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="unreadOnly"></param>
        /// <param name="limit"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns notifications.</returns>
        public async Task<List<UserNotificationInfoType>> GetNotificationsAsync(
            Guid userId,
            bool unreadOnly = false,
            int limit = 20,
            CancellationToken cancellationToken = default)
        {
            int take = Math.Clamp(limit, 1, 100);
            IQueryable<UserNotificationEntity> query = m_DbContext.UserNotifications
                .AsNoTracking()
                .Where(item => item.UserId == userId);

            if (unreadOnly)
            {
                query = query.Where(item => item.ReadAt == null);
            }

            List<UserNotificationEntity> rows = await query
                .OrderByDescending(item => item.CreatedAt)
                .Take(take)
                .ToListAsync(cancellationToken);

            return rows.Select(ToInfo).ToList();
        }

        /// <summary>
        /// CreateAsync
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="kind"></param>
        /// <param name="title"></param>
        /// <param name="message"></param>
        /// <param name="agentKey"></param>
        /// <param name="packageName"></param>
        /// <param name="success"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns created notification.</returns>
        public async Task<UserNotificationInfoType> CreateAsync(
            Guid userId,
            string kind,
            string title,
            string message,
            string? agentKey = null,
            string? packageName = null,
            bool? success = null,
            CancellationToken cancellationToken = default)
        {
            UserNotificationEntity entity = new()
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Kind = kind,
                Title = title,
                Message = message,
                AgentKey = agentKey,
                PackageName = packageName,
                Success = success,
                CreatedAt = DateTime.UtcNow
            };

            m_DbContext.UserNotifications.Add(entity);
            await m_DbContext.SaveChangesAsync(cancellationToken);

            return ToInfo(entity);
        }

        /// <summary>
        /// MarkReadAsync
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="notificationId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns true when updated.</returns>
        public async Task<bool> MarkReadAsync(
            Guid userId,
            Guid notificationId,
            CancellationToken cancellationToken = default)
        {
            UserNotificationEntity? entity = await m_DbContext.UserNotifications
                .FirstOrDefaultAsync(
                    item => item.Id == notificationId && item.UserId == userId,
                    cancellationToken);

            if (entity == null)
            {
                return false;
            }

            if (entity.ReadAt == null)
            {
                entity.ReadAt = DateTime.UtcNow;
                await m_DbContext.SaveChangesAsync(cancellationToken);
            }

            return true;
        }

        /// <summary>
        /// MarkAllReadAsync
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns count marked.</returns>
        public async Task<int> MarkAllReadAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            List<UserNotificationEntity> unread = await m_DbContext.UserNotifications
                .Where(item => item.UserId == userId && item.ReadAt == null)
                .ToListAsync(cancellationToken);

            if (unread.Count == 0)
            {
                return 0;
            }

            DateTime now = DateTime.UtcNow;

            foreach (UserNotificationEntity item in unread)
            {
                item.ReadAt = now;
            }

            await m_DbContext.SaveChangesAsync(cancellationToken);

            return unread.Count;
        }

        /// <summary>
        /// ToInfo
        /// </summary>
        private static UserNotificationInfoType ToInfo(
            UserNotificationEntity entity)
        {
            return new UserNotificationInfoType
            {
                Id = entity.Id.ToString(),
                Kind = entity.Kind,
                Title = entity.Title,
                Message = entity.Message,
                AgentKey = entity.AgentKey,
                PackageName = entity.PackageName,
                Success = entity.Success,
                CreatedAt = entity.CreatedAt,
                IsRead = entity.ReadAt.HasValue
            };
        }
    }
}
