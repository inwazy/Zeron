// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using Zeron.Server.Data;
using Zeron.Server.Data.Entities;
using Zeron.Server.ZInterfaces;
using Zeron.ZCore;
using Zeron.ZCore.Type;
using Zeron.ZCore.Utils;

namespace Zeron.Server.ZServers
{
    /// <summary>
    /// AuditLogServer - write and query attributed operation history.
    /// </summary>
    public class AuditLogServer
    {
        // Database context.
        private readonly ZeronServerDbContext m_DbContext;

        // Optional enterprise agent scope filter.
        private readonly IAgentAccessScopeFilter? m_ScopeFilter;

        // HTTP context accessor for caller principal.
        private readonly IHttpContextAccessor? m_HttpContextAccessor;

        /// <summary>
        /// AuditLogServer
        /// </summary>
        /// <param name="dbContext"></param>
        /// <param name="scopeFilter"></param>
        /// <param name="httpContextAccessor"></param>
        /// <returns>Returns void.</returns>
        public AuditLogServer(
            ZeronServerDbContext dbContext,
            IAgentAccessScopeFilter? scopeFilter = null,
            IHttpContextAccessor? httpContextAccessor = null)
        {
            m_DbContext = dbContext;
            m_ScopeFilter = scopeFilter;
            m_HttpContextAccessor = httpContextAccessor;
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

            AuditLogInfoType info = ToInfo(entity);
            ZeronEventBus.PublishObject(
                ZeronEventTopics.AuditWritten,
                info,
                source: "server",
                correlationId: entity.Id.ToString("N"));

            return info;
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
            return await QueryAsync(action, actorUsername, targetKey, source, limit, 0, cancellationToken);
        }

        /// <summary>
        /// QueryAsync
        /// </summary>
        /// <param name="action"></param>
        /// <param name="actorUsername"></param>
        /// <param name="targetKey"></param>
        /// <param name="source"></param>
        /// <param name="limit"></param>
        /// <param name="offset"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns audit rows.</returns>
        public async Task<List<AuditLogInfoType>> QueryAsync(
            string? action,
            string? actorUsername,
            string? targetKey,
            string? source,
            int limit,
            int offset,
            CancellationToken cancellationToken = default)
        {
            int take = Math.Clamp(limit, 1, 500);
            int skip = Math.Max(0, offset);
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

            IReadOnlySet<string>? allowed = await ResolveAllowedAgentKeysAsync(cancellationToken);

            if (allowed == null)
            {
                List<AuditLogEntity> unrestricted = await query
                    .OrderByDescending(item => item.OccurredAt)
                    .Skip(skip)
                    .Take(take)
                    .ToListAsync(cancellationToken);

                return unrestricted.Select(ToInfo).ToList();
            }

            // Scope predicates (e.g. package@agent) are applied in-memory for provider portability.
            string? callerUsername = ResolveCallerUsername();
            List<AuditLogEntity> candidates = await query
                .OrderByDescending(item => item.OccurredAt)
                .ToListAsync(cancellationToken);

            return candidates
                .Where(row => IsAuditVisibleForScope(row, allowed, callerUsername))
                .Skip(skip)
                .Take(take)
                .Select(ToInfo)
                .ToList();
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
        /// Returns whether an audit row is visible under the caller's agent allow-list.
        /// </summary>
        /// <param name="row"></param>
        /// <param name="allowed"></param>
        /// <param name="callerUsername"></param>
        /// <returns>True when visible.</returns>
        private static bool IsAuditVisibleForScope(
            AuditLogEntity row,
            IReadOnlySet<string> allowed,
            string? callerUsername)
        {
            if (allowed.Count == 0)
            {
                return !string.IsNullOrWhiteSpace(callerUsername)
                    && string.Equals(row.ActorUsername, callerUsername, StringComparison.Ordinal)
                    && !string.Equals(row.Source, "agent", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(row.TargetType, "agent", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(row.TargetType, "binding", StringComparison.OrdinalIgnoreCase);
            }

            if (row.TargetKey != null && allowed.Contains(row.TargetKey))
            {
                return true;
            }

            if (string.Equals(row.Source, "agent", StringComparison.OrdinalIgnoreCase)
                && row.ActorUsername != null
                && allowed.Contains(row.ActorUsername))
            {
                return true;
            }

            if (row.TargetKey != null)
            {
                foreach (string key in allowed)
                {
                    if (row.TargetKey.EndsWith("@" + key, StringComparison.Ordinal))
                    {
                        return true;
                    }
                }
            }

            return !string.IsNullOrWhiteSpace(callerUsername)
                && string.Equals(row.ActorUsername, callerUsername, StringComparison.Ordinal)
                && !string.Equals(row.Source, "agent", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(row.TargetType, "agent", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(row.TargetType, "binding", StringComparison.OrdinalIgnoreCase)
                && (row.TargetKey == null || !row.TargetKey.Contains('@'));
        }

        /// <summary>
        /// Resolves the caller's agent allow-list when a scope filter is registered.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns>Allow-list or null when unrestricted.</returns>
        private async Task<IReadOnlySet<string>?> ResolveAllowedAgentKeysAsync(
            CancellationToken cancellationToken)
        {
            if (m_ScopeFilter == null)
            {
                return null;
            }

            ClaimsPrincipal? user = m_HttpContextAccessor?.HttpContext?.User;

            return await m_ScopeFilter.GetAllowedAgentKeysAsync(user, cancellationToken);
        }

        /// <summary>
        /// Resolves the authenticated caller username from HTTP context.
        /// </summary>
        /// <returns>Username or null.</returns>
        private string? ResolveCallerUsername()
        {
            ClaimsPrincipal? user = m_HttpContextAccessor?.HttpContext?.User;

            if (user?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            return user.Identity?.Name
                ?? user.FindFirstValue(ClaimTypes.Name);
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
