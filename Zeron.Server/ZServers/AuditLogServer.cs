// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using Zeron.Server.Data;
using Zeron.Server.Data.Entities;
using Zeron.ZCore;
using Zeron.ZCore.Type;

namespace Zeron.Server.ZServers
{
    /// <summary>
    /// AuditLogServer - write and query attributed operation history.
    /// </summary>
    public class AuditLogServer
    {
        // Database context.
        private readonly ZeronServerDbContext m_DbContext;

        /// <summary>
        /// AuditLogServer
        /// </summary>
        /// <param name="dbContext"></param>
        /// <returns>Returns void.</returns>
        public AuditLogServer(
            ZeronServerDbContext dbContext)
        {
            m_DbContext = dbContext;
        }

        /// <summary>
        /// FromPrincipal
        /// </summary>
        /// <param name="principal"></param>
        /// <returns>Returns actor or null.</returns>
        public static AuditActorType? FromPrincipal(
            ClaimsPrincipal? principal)
        {
            if (principal?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            Guid? userId = null;
            string? idValue = principal.FindFirstValue(ClaimTypes.NameIdentifier);

            if (Guid.TryParse(idValue, out Guid parsed))
            {
                userId = parsed;
            }

            return new AuditActorType
            {
                UserId = userId,
                Username = principal.Identity?.Name
                    ?? principal.FindFirstValue(ClaimTypes.Name),
                Role = principal.FindFirstValue(ClaimTypes.Role),
                Source = "server"
            };
        }

        /// <summary>
        /// WriteAsync
        /// </summary>
        /// <param name="action"></param>
        /// <param name="success"></param>
        /// <param name="summary"></param>
        /// <param name="actor"></param>
        /// <param name="targetType"></param>
        /// <param name="targetKey"></param>
        /// <param name="details"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns created audit info.</returns>
        public async Task<AuditLogInfoType> WriteAsync(
            string action,
            bool success,
            string summary,
            AuditActorType? actor = null,
            string? targetType = null,
            string? targetKey = null,
            object? details = null,
            CancellationToken cancellationToken = default)
        {
            AuditLogEntity entity = new()
            {
                Id = Guid.NewGuid(),
                OccurredAt = DateTime.UtcNow,
                ActorUserId = actor?.UserId,
                ActorUsername = actor?.Username,
                ActorRole = actor?.Role,
                Action = action,
                TargetType = targetType,
                TargetKey = targetKey,
                Success = success,
                Summary = summary ?? "",
                DetailsJson = details == null
                    ? null
                    : JsonSerializer.Serialize(details),
                Source = string.IsNullOrWhiteSpace(actor?.Source) ? "server" : actor!.Source
            };

            m_DbContext.AuditLogs.Add(entity);
            await m_DbContext.SaveChangesAsync(cancellationToken);

            ZNLogger.Common.Info(string.Format(CultureInfo.InvariantCulture,
                "AuditLogServer {0} success={1} actor={2} target={3}/{4}",
                action,
                success,
                entity.ActorUsername ?? "-",
                targetType ?? "-",
                targetKey ?? "-"));

            return ToInfo(entity);
        }

        /// <summary>
        /// QueryAsync
        /// </summary>
        /// <param name="action"></param>
        /// <param name="actorUsername"></param>
        /// <param name="targetKey"></param>
        /// <param name="source"></param>
        /// <param name="limit"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns audit rows.</returns>
        public async Task<List<AuditLogInfoType>> QueryAsync(
            string? action = null,
            string? actorUsername = null,
            string? targetKey = null,
            string? source = null,
            int limit = 100,
            CancellationToken cancellationToken = default)
        {
            int take = Math.Clamp(limit, 1, 500);
            IQueryable<AuditLogEntity> query = m_DbContext.AuditLogs.AsQueryable();

            if (!string.IsNullOrWhiteSpace(action))
            {
                string actionFilter = action.Trim();
                query = query.Where(item => item.Action == actionFilter
                    || item.Action.StartsWith(actionFilter));
            }

            if (!string.IsNullOrWhiteSpace(actorUsername))
            {
                string actorFilter = actorUsername.Trim();
                query = query.Where(item => item.ActorUsername != null
                    && item.ActorUsername.Contains(actorFilter));
            }

            if (!string.IsNullOrWhiteSpace(targetKey))
            {
                string targetFilter = targetKey.Trim();
                query = query.Where(item => item.TargetKey != null
                    && item.TargetKey.Contains(targetFilter));
            }

            if (!string.IsNullOrWhiteSpace(source))
            {
                string sourceFilter = source.Trim();
                query = query.Where(item => item.Source == sourceFilter);
            }

            List<AuditLogEntity> rows = await query
                .OrderByDescending(item => item.OccurredAt)
                .Take(take)
                .ToListAsync(cancellationToken);

            return rows.Select(ToInfo).ToList();
        }

        /// <summary>
        /// ResolveUserActorAsync
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns actor.</returns>
        public async Task<AuditActorType?> ResolveUserActorAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            UserEntity? user = await m_DbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == userId, cancellationToken);

            if (user == null)
            {
                return null;
            }

            return new AuditActorType
            {
                UserId = user.Id,
                Username = user.Username,
                Role = user.Role,
                Source = "server"
            };
        }

        /// <summary>
        /// ToInfo
        /// </summary>
        /// <param name="entity"></param>
        /// <returns>Returns AuditLogInfoType.</returns>
        private static AuditLogInfoType ToInfo(
            AuditLogEntity entity)
        {
            return new AuditLogInfoType
            {
                Id = entity.Id.ToString(),
                OccurredAt = entity.OccurredAt,
                ActorUsername = entity.ActorUsername,
                ActorRole = entity.ActorRole,
                Action = entity.Action,
                TargetType = entity.TargetType,
                TargetKey = entity.TargetKey,
                Success = entity.Success,
                Summary = entity.Summary,
                DetailsJson = entity.DetailsJson,
                Source = entity.Source
            };
        }
    }
}
