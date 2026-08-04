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

        /// <summary>
        /// EventIngestorServer
        /// </summary>
        /// <param name="dbContext"></param>
        /// <param name="taskDispatcher"></param>
        /// <param name="dashboardNotifier"></param>
        /// <returns>Returns void.</returns>
        public EventIngestorServer(
            ZeronServerDbContext dbContext,
            TaskDispatcherServer taskDispatcher,
            IDashboardNotifier? dashboardNotifier = null)
        {
            m_DbContext = dbContext;
            m_TaskDispatcher = taskDispatcher;
            m_DashboardNotifier = dashboardNotifier;
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
