// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text.Json;
using Zeron.Server.Data;
using Zeron.Server.Data.Entities;
using Zeron.Server.ZCore;
using Zeron.Server.ZInterfaces;
using Zeron.ZCore;
using Zeron.ZCore.Type;

namespace Zeron.Server.ZServers
{
    /// <summary>
    /// EventIngestorServer
    /// </summary>
    public class EventIngestorServer
    {
        // DbContext
        private readonly ZeronServerDbContext m_DbContext;

        // TaskDispatcher
        private readonly TaskDispatcherServer m_TaskDispatcher;

        // DashboardNotifier
        private readonly IDashboardNotifier? m_DashboardNotifier;

        // Optional audit log for agent-originated package ops.
        private readonly AuditLogServer? m_AuditLogServer;

        /// <summary>
        /// EventIngestorServer
        /// </summary>
        /// <param name="dbContext"></param>
        /// <param name="taskDispatcher"></param>
        /// <param name="dashboardNotifier"></param>
        /// <param name="auditLogServer"></param>
        /// <returns>Returns void.</returns>
        public EventIngestorServer(
            ZeronServerDbContext dbContext,
            TaskDispatcherServer taskDispatcher,
            IDashboardNotifier? dashboardNotifier = null,
            AuditLogServer? auditLogServer = null)
        {
            m_DbContext = dbContext;
            m_TaskDispatcher = taskDispatcher;
            m_DashboardNotifier = dashboardNotifier;
            m_AuditLogServer = auditLogServer;
        }

        /// <summary>
        /// IngestEventAsync
        /// </summary>
        /// <param name="report"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns bool.</returns>
        public async Task<bool> IngestEventAsync(
            AgentEventReportType report, 
            CancellationToken cancellationToken = default)
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

            await TryCompletePackageDeployFromInstallEventAsync(report, cancellationToken);
            await TryWritePackageAuditFromEventAsync(report, agent, cancellationToken);

            if (m_DashboardNotifier != null)
            {
                EventEntity? savedEvent = await m_DbContext.Events
                    .Include(evt => evt.Agent)
                    .OrderByDescending(evt => evt.Id)
                    .FirstOrDefaultAsync(evt => evt.AgentId == agent.Id && evt.Topic == report.Topic, cancellationToken);

                if (savedEvent != null)
                {
                    await m_DashboardNotifier.NotifyEventAsync(savedEvent);
                }
            }

            return true;
        }

        /// <summary>
        /// TryCompletePackageDeployFromInstallEventAsync
        /// </summary>
        /// <param name="report"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns void.</returns>
        private async Task TryCompletePackageDeployFromInstallEventAsync(
            AgentEventReportType report,
            CancellationToken cancellationToken)
        {
            bool isCompleted = string.Equals(report.Topic, "install.completed", StringComparison.OrdinalIgnoreCase);
            bool isFailed = string.Equals(report.Topic, "install.failed", StringComparison.OrdinalIgnoreCase);

            if (!isCompleted && !isFailed)
            {
                return;
            }

            if (!TryReadInstallCompletion(report.Payload, out string? assignmentId, out bool? success, out int? exitCode, out string? package))
            {
                return;
            }

            bool finalSuccess = isCompleted && success != false;

            string responseJson = JsonSerializer.Serialize(new
            {
                success = finalSuccess,
                completed = true,
                package,
                topic = report.Topic,
                exitCode,
                source = "install-event"
            });

            string? errorMessage = finalSuccess
                ? null
                : string.Format(CultureInfo.InvariantCulture,
                    "Install event '{0}' for package '{1}' (exitCode={2}).",
                    report.Topic,
                    package ?? "unknown",
                    exitCode?.ToString(CultureInfo.InvariantCulture) ?? "n/a");

            bool updated = await m_TaskDispatcher.ReportResultAsync(new TaskResultReportType
            {
                AssignmentId = assignmentId,
                AgentId = report.AgentId,
                Success = finalSuccess,
                ResponseJson = responseJson,
                ErrorMessage = errorMessage
            }, cancellationToken);

            if (updated)
            {
                ZNLogger.Common.Info(string.Format(CultureInfo.InvariantCulture,
                    "EventIngestorServer completed assignment {0} from {1}.", assignmentId, report.Topic));
            }
        }

        /// <summary>
        /// TryReadInstallCompletion
        /// </summary>
        /// <param name="payload"></param>
        /// <param name="assignmentId"></param>
        /// <param name="success"></param>
        /// <param name="exitCode"></param>
        /// <param name="package"></param>
        /// <returns>Returns bool.</returns>
        private static bool TryReadInstallCompletion(
            string? payload,
            out string? assignmentId,
            out bool? success,
            out int? exitCode,
            out string? package)
        {
            assignmentId = null;
            success = null;
            exitCode = null;
            package = null;

            if (string.IsNullOrWhiteSpace(payload))
            {
                return false;
            }

            try
            {
                using JsonDocument document = JsonDocument.Parse(payload);
                JsonElement root = document.RootElement;

                if (root.TryGetProperty("assignmentId", out JsonElement assignmentElement)
                    && assignmentElement.ValueKind == JsonValueKind.String)
                {
                    assignmentId = assignmentElement.GetString();
                }

                if (string.IsNullOrWhiteSpace(assignmentId))
                {
                    return false;
                }

                if (root.TryGetProperty("success", out JsonElement successElement)
                    && (successElement.ValueKind == JsonValueKind.True || successElement.ValueKind == JsonValueKind.False))
                {
                    success = successElement.GetBoolean();
                }

                if (root.TryGetProperty("exitCode", out JsonElement exitElement)
                    && exitElement.ValueKind == JsonValueKind.Number
                    && exitElement.TryGetInt32(out int parsedExit))
                {
                    exitCode = parsedExit;
                }

                if (root.TryGetProperty("package", out JsonElement packageElement)
                    && packageElement.ValueKind == JsonValueKind.String)
                {
                    package = packageElement.GetString();
                }

                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        /// <summary>
        /// TryWritePackageAuditFromEventAsync - mirror Demand package ops into AuditLog.
        /// </summary>
        /// <param name="report"></param>
        /// <param name="agent"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns void.</returns>
        private async Task TryWritePackageAuditFromEventAsync(
            AgentEventReportType report,
            AgentEntity agent,
            CancellationToken cancellationToken)
        {
            if (m_AuditLogServer == null || string.IsNullOrWhiteSpace(report.Topic))
            {
                return;
            }

            string? action = report.Topic.ToLowerInvariant() switch
            {
                "package.override" => AuditActions.PackageOverride,
                "package.clear-override" => AuditActions.PackageClearOverride,
                "package.catalog.sync" => AuditActions.PackageCatalogSync,
                _ => null
            };

            if (action == null)
            {
                return;
            }

            string? packageName = null;
            bool success = true;

            try
            {
                if (!string.IsNullOrWhiteSpace(report.Payload))
                {
                    using JsonDocument document = JsonDocument.Parse(report.Payload);

                    if (document.RootElement.TryGetProperty("package", out JsonElement packageElement))
                    {
                        packageName = packageElement.GetString();
                    }

                    if (document.RootElement.TryGetProperty("success", out JsonElement successElement)
                        && successElement.ValueKind is JsonValueKind.True or JsonValueKind.False)
                    {
                        success = successElement.GetBoolean();
                    }
                    else if (document.RootElement.TryGetProperty("overridden", out JsonElement overridden)
                        && overridden.ValueKind is JsonValueKind.True or JsonValueKind.False)
                    {
                        success = overridden.GetBoolean();
                    }
                    else if (document.RootElement.TryGetProperty("cleared", out JsonElement cleared)
                        && cleared.ValueKind is JsonValueKind.True or JsonValueKind.False)
                    {
                        success = cleared.GetBoolean();
                    }
                    else if (document.RootElement.TryGetProperty("synced", out JsonElement synced)
                        && synced.ValueKind is JsonValueKind.True or JsonValueKind.False)
                    {
                        success = synced.GetBoolean();
                    }
                }
            }
            catch (JsonException)
            {
            }

            AuditActorType actor = new()
            {
                Username = agent.AgentKey,
                Role = "Agent",
                Source = "agent"
            };

            await m_AuditLogServer.WriteAsync(
                action,
                success,
                $"{action} on agent '{agent.AgentKey}'.",
                actor,
                targetType: string.IsNullOrWhiteSpace(packageName) ? "agent" : "package",
                targetKey: string.IsNullOrWhiteSpace(packageName) ? agent.AgentKey : packageName + "@" + agent.AgentKey,
                details: new
                {
                    agent.AgentKey,
                    agent.MachineName,
                    report.Topic,
                    report.Payload
                },
                cancellationToken: cancellationToken);
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
