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
    /// TaskDispatcherServer
    /// </summary>
    public class TaskDispatcherServer
    {
        // DbContext
        private readonly ZeronServerDbContext m_DbContext;

        // CommandPublisher 
        private readonly CommandPublisherServer m_CommandPublisher;

        // Optional enterprise agent scope filter
        private readonly IAgentAccessScopeFilter? m_ScopeFilter;

        // HTTP context accessor for caller principal
        private readonly IHttpContextAccessor? m_HttpContextAccessor;

        /// <summary>
        /// TaskDispatcherServer
        /// </summary>
        /// <param name="dbContext"></param>
        /// <param name="commandPublisher"></param>
        /// <param name="scopeFilter"></param>
        /// <param name="httpContextAccessor"></param>
        /// <returns>Returns void.</returns>
        public TaskDispatcherServer(
            ZeronServerDbContext dbContext, 
            CommandPublisherServer commandPublisher,
            IAgentAccessScopeFilter? scopeFilter = null,
            IHttpContextAccessor? httpContextAccessor = null)
        {
            m_DbContext = dbContext;
            m_CommandPublisher = commandPublisher;
            m_ScopeFilter = scopeFilter;
            m_HttpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// CreateTaskAsync
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns TaskEntity.</returns>
        public async Task<TaskEntity> CreateTaskAsync(
            TaskCreateRequestType request, 
            CancellationToken cancellationToken = default)
        {
            List<AgentEntity> resolvedAgents = await ResolveTargetAgentsAsync(request, cancellationToken);
            IReadOnlySet<string>? allowedAgents = await ResolveAllowedAgentKeysAsync(cancellationToken);
            List<AgentEntity> targetAgents = allowedAgents == null
                ? resolvedAgents
                : resolvedAgents.Where(agent => allowedAgents.Contains(agent.AgentKey)).ToList();

            // Only fail when an active scope filter removed every candidate.
            // Unscoped callers may still create tasks with zero assignments (no online agents).
            if (allowedAgents != null && targetAgents.Count == 0)
            {
                throw new UnauthorizedAccessException(
                    "No target agents are allowed for the current user scope.");
            }

            TaskEntity task = new()
            {
                Id = Guid.NewGuid(),
                Name = request.Name ?? "task-" + DateTime.UtcNow.Ticks,
                Description = request.Description,
                TargetApi = request.TargetApi ?? "",
                Command = request.Command ?? "",
                TargetType = request.TargetType ?? "all",
                TargetFilterJson = JsonSerializer.Serialize(request),
                Status = "pending",
                CreatedAt = DateTime.UtcNow
            };

            m_DbContext.Tasks.Add(task);

            foreach (AgentEntity agent in targetAgents)
            {
                m_DbContext.TaskAssignments.Add(new TaskAssignmentEntity
                {
                    Id = Guid.NewGuid(),
                    TaskId = task.Id,
                    AgentId = agent.Id,
                    Status = "pending",
                    AssignedAt = DateTime.UtcNow
                });
            }

            await m_DbContext.SaveChangesAsync(cancellationToken);

            ZNLogger.Common.Info(string.Format(CultureInfo.InvariantCulture,
                "TaskDispatcherServer created task '{0}' for {1} agent(s).", task.Name, targetAgents.Count));

            return task;
        }

        /// <summary>
        /// DispatchPendingAssignmentsAsync
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns dispatched count.</returns>
        public async Task<int> DispatchPendingAssignmentsAsync(
            CancellationToken cancellationToken = default)
        {
            List<TaskAssignmentEntity> pendingAssignments = await m_DbContext.TaskAssignments
                .Include(assignment => assignment.Task)
                .Include(assignment => assignment.Agent)
                .Where(assignment => assignment.Status == "pending"
                    && assignment.Agent != null
                    && assignment.Agent.Status == "online")
                .ToListAsync(cancellationToken);

            int dispatched = 0;
            bool changed = false;

            foreach (TaskAssignmentEntity assignment in pendingAssignments)
            {
                if (assignment.Task == null || assignment.Agent == null)
                {
                    continue;
                }

                string gatePayload = JsonSerializer.Serialize(new
                {
                    assignmentId = assignment.Id,
                    agentKey = assignment.Agent.AgentKey,
                    targetApi = assignment.Task.TargetApi,
                    command = assignment.Task.Command
                });

                GateDecisionType gateDecision = ZeronGateServer.Evaluate(
                    ZeronEventTopics.GateDispatch,
                    gatePayload,
                    assignment.Id.ToString(),
                    ZeronGateServer.GetDefaultTimeoutMs());

                if (gateDecision == GateDecisionType.Cancel)
                {
                    FailAssignment(assignment, "Dispatch cancelled by .NET gate.");
                    changed = true;
                    continue;
                }

                bool published = m_CommandPublisher.PublishRemoteCommand(
                    assignment.Agent.AgentKey,
                    assignment.Id,
                    assignment.Task.TargetApi,
                    assignment.Task.Command);

                if (!published)
                {
                    continue;
                }

                assignment.Status = "dispatched";
                assignment.StartedAt = DateTime.UtcNow;
                dispatched++;
                changed = true;
            }

            if (changed)
            {
                await m_DbContext.SaveChangesAsync(cancellationToken);
            }

            return dispatched;
        }

        /// <summary>
        /// FailAssignment - mark pending assignment failed without dispatch.
        /// </summary>
        /// <param name="assignment"></param>
        /// <param name="errorMessage"></param>
        /// <returns>Returns void.</returns>
        private void FailAssignment(
            TaskAssignmentEntity assignment,
            string errorMessage)
        {
            assignment.Status = "failed";
            assignment.CompletedAt = DateTime.UtcNow;

            if (assignment.Result == null)
            {
                m_DbContext.TaskResults.Add(new TaskResultEntity
                {
                    Id = Guid.NewGuid(),
                    AssignmentId = assignment.Id,
                    Success = false,
                    ErrorMessage = errorMessage,
                    CompletedAt = DateTime.UtcNow
                });
            }

            if (assignment.Task != null && assignment.Task.Status != "cancelled")
            {
                assignment.Task.Status = "failed";
                PublishTaskCompleted(assignment.Task);
            }

            PublishAssignmentCompleted(assignment);

            ZNLogger.Common.Warn(string.Format(CultureInfo.InvariantCulture,
                "TaskDispatcherServer failed assignment {0}: {1}", assignment.Id, errorMessage));
        }

        /// <summary>
        /// ReportResultAsync
        /// </summary>
        /// <param name="report"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns bool.</returns>
        public async Task<bool> ReportResultAsync(
            TaskResultReportType report, 
            CancellationToken cancellationToken = default)
        {
            if (!Guid.TryParse(report.AssignmentId, out Guid assignmentId))
            {
                return false;
            }

            TaskAssignmentEntity? assignment = await m_DbContext.TaskAssignments
                .Include(item => item.Task)
                .Include(item => item.Agent)
                .Include(item => item.Result)
                .FirstOrDefaultAsync(item => item.Id == assignmentId, cancellationToken);

            if (assignment == null || assignment.Status == "cancelled")
            {
                return false;
            }

            // ManagedPackage queue acceptance: keep assignment running until install.* completes.
            if (IsManagedPackageQueued(assignment, report))
            {
                if (assignment.Status is "completed" or "failed")
                {
                    return true;
                }

                assignment.Status = "running";

                if (assignment.Task != null && assignment.Task.Status != "cancelled")
                {
                    assignment.Task.Status = "running";
                }

                await m_DbContext.SaveChangesAsync(cancellationToken);

                return true;
            }

            assignment.Status = report.Success ? "completed" : "failed";
            assignment.CompletedAt = DateTime.UtcNow;

            if (assignment.Result == null)
            {
                m_DbContext.TaskResults.Add(new TaskResultEntity
                {
                    Id = Guid.NewGuid(),
                    AssignmentId = assignment.Id,
                    Success = report.Success,
                    ResponseJson = report.ResponseJson,
                    ErrorMessage = report.ErrorMessage,
                    CompletedAt = DateTime.UtcNow
                });
            }
            else
            {
                assignment.Result.Success = report.Success;
                assignment.Result.ResponseJson = report.ResponseJson;
                assignment.Result.ErrorMessage = report.ErrorMessage;
                assignment.Result.CompletedAt = DateTime.UtcNow;
            }

            PublishAssignmentCompleted(assignment);

            if (assignment.Task != null && assignment.Task.Status != "cancelled")
            {
                await m_DbContext.SaveChangesAsync(cancellationToken);

                bool anyPending = await m_DbContext.TaskAssignments
                    .AnyAsync(item => item.TaskId == assignment.TaskId
                        && item.Status != "completed"
                        && item.Status != "failed"
                        && item.Status != "cancelled", cancellationToken);

                assignment.Task.Status = anyPending ? "running" : (report.Success ? "completed" : "failed");

                if (!anyPending)
                {
                    PublishTaskCompleted(assignment.Task);
                }
            }

            await m_DbContext.SaveChangesAsync(cancellationToken);

            return true;
        }

        /// <summary>
        /// IsManagedPackageQueued
        /// </summary>
        /// <param name="assignment"></param>
        /// <param name="report"></param>
        /// <returns>Returns bool.</returns>
        private static bool IsManagedPackageQueued(
            TaskAssignmentEntity assignment,
            TaskResultReportType report)
        {
            if (!report.Success
                || string.IsNullOrWhiteSpace(report.ResponseJson)
                || assignment.Task == null
                || !string.Equals(assignment.Task.TargetApi, "ManagedPackage", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string json = report.ResponseJson;

            return json.Contains("\"queued\":true", StringComparison.OrdinalIgnoreCase)
                || json.Contains("\"queued\": true", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// GetTasksAsync
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns task list.</returns>
        public async Task<List<TaskEntity>> GetTasksAsync(
            CancellationToken cancellationToken = default)
        {
            List<TaskEntity> tasks = await m_DbContext.Tasks
                .Include(task => task.Assignments)
                .ThenInclude(assignment => assignment.Agent)
                .OrderByDescending(task => task.CreatedAt)
                .ToListAsync(cancellationToken);

            IReadOnlySet<string>? allowed = await ResolveAllowedAgentKeysAsync(cancellationToken);

            if (allowed == null)
            {
                return tasks;
            }

            return tasks.Where(task => TaskTouchesAllowedAgents(task, allowed)).ToList();
        }

        /// <summary>
        /// GetTaskAsync
        /// </summary>
        /// <param name="taskId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns task or null.</returns>
        public async Task<TaskEntity?> GetTaskAsync(
            Guid taskId, 
            CancellationToken cancellationToken = default)
        {
            TaskEntity? task = await m_DbContext.Tasks
                .Include(task => task.Assignments)
                .ThenInclude(assignment => assignment.Result)
                .Include(task => task.Assignments)
                .ThenInclude(assignment => assignment.Agent)
                .FirstOrDefaultAsync(task => task.Id == taskId, cancellationToken);

            if (task == null)
            {
                return null;
            }

            IReadOnlySet<string>? allowed = await ResolveAllowedAgentKeysAsync(cancellationToken);

            if (allowed != null && !TaskTouchesAllowedAgents(task, allowed))
            {
                return null;
            }

            return task;
        }

        /// <summary>
        /// CancelTaskAsync
        /// </summary>
        /// <param name="taskId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns bool.</returns>
        public async Task<bool> CancelTaskAsync(
            Guid taskId, 
            CancellationToken cancellationToken = default)
        {
            TaskEntity? task = await m_DbContext.Tasks
                .Include(item => item.Assignments)
                .ThenInclude(assignment => assignment.Agent)
                .FirstOrDefaultAsync(item => item.Id == taskId, cancellationToken);

            if (task == null)
            {
                return false;
            }

            IReadOnlySet<string>? allowed = await ResolveAllowedAgentKeysAsync(cancellationToken);

            if (allowed != null && !TaskTouchesAllowedAgents(task, allowed))
            {
                return false;
            }

            if (task.Status is "completed" or "failed" or "cancelled")
            {
                return false;
            }

            task.Status = "cancelled";

            foreach (TaskAssignmentEntity assignment in task.Assignments)
            {
                if (assignment.Status is "pending" or "dispatched")
                {
                    assignment.Status = "cancelled";
                    assignment.CompletedAt = DateTime.UtcNow;
                }
            }

            await m_DbContext.SaveChangesAsync(cancellationToken);

            ZNLogger.Common.Info(string.Format(CultureInfo.InvariantCulture,
                "TaskDispatcherServer cancelled task '{0}'.", task.Name));

            return true;
        }

        /// <summary>
        /// ResolveTargetAgentsAsync
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns target agents.</returns>
        private async Task<List<AgentEntity>> ResolveTargetAgentsAsync(
            TaskCreateRequestType request,
            CancellationToken cancellationToken)
        {
            IQueryable<AgentEntity> query = m_DbContext.Agents.AsQueryable();
            string targetType = request.TargetType?.ToLowerInvariant() ?? "all";

            if (targetType == "agent" && request.AgentIds != null && request.AgentIds.Count > 0)
            {
                query = query.Where(agent => request.AgentIds.Contains(agent.AgentKey));
            }
            else if (targetType == "filter" && !string.IsNullOrWhiteSpace(request.HostnamePattern))
            {
                string pattern = request.HostnamePattern.Replace("*", "%");
                query = query.Where(agent => EF.Functions.Like(agent.MachineName ?? "", pattern));
            }
            else
            {
                query = query.Where(agent => agent.Status == "online");
            }

            return await query.ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Applies the caller's agent allow-list to target agents.
        /// </summary>
        /// <param name="agents"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Filtered agents.</returns>
        private async Task<List<AgentEntity>> ApplyAgentScopeAsync(
            List<AgentEntity> agents,
            CancellationToken cancellationToken)
        {
            IReadOnlySet<string>? allowed = await ResolveAllowedAgentKeysAsync(cancellationToken);

            if (allowed == null)
            {
                return agents;
            }

            return agents.Where(agent => allowed.Contains(agent.AgentKey)).ToList();
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
        /// Returns whether the task has at least one assignment on an allowed agent.
        /// </summary>
        /// <param name="task"></param>
        /// <param name="allowed"></param>
        /// <returns>True when visible.</returns>
        private static bool TaskTouchesAllowedAgents(
            TaskEntity task,
            IReadOnlySet<string> allowed)
        {
            if (task.Assignments == null || task.Assignments.Count == 0)
            {
                return false;
            }

            foreach (TaskAssignmentEntity assignment in task.Assignments)
            {
                string? key = assignment.Agent?.AgentKey;

                if (!string.IsNullOrWhiteSpace(key) && allowed.Contains(key))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Publishes task.completed for observability consumers (editions / OTel).
        /// </summary>
        /// <param name="task">Terminal central task.</param>
        /// <returns>Returns void.</returns>
        private static void PublishTaskCompleted(
            TaskEntity task)
        {
            if (task.Status is not ("completed" or "failed"))
            {
                return;
            }

            DateTime completedAt = DateTime.UtcNow;
            DateTime createdAt = ToUtc(task.CreatedAt);
            double durationMs = Math.Max(0, (completedAt - createdAt).TotalMilliseconds);

            ZeronEventBus.PublishObject(
                ZeronEventTopics.TaskCompleted,
                new TaskCompletedInfoType
                {
                    TaskId = task.Id,
                    TargetApi = task.TargetApi,
                    Status = task.Status,
                    CreatedAt = createdAt,
                    CompletedAt = completedAt,
                    DurationMs = durationMs
                },
                source: "server",
                correlationId: task.Id.ToString("N"));
        }

        /// <summary>
        /// Publishes assignment.completed for per-agent duration metrics.
        /// </summary>
        /// <param name="assignment">Terminal assignment.</param>
        /// <returns>Returns void.</returns>
        private static void PublishAssignmentCompleted(
            TaskAssignmentEntity assignment)
        {
            if (assignment.Status is not ("completed" or "failed"))
            {
                return;
            }

            DateTime completedAt = assignment.CompletedAt == null || assignment.CompletedAt == default
                ? DateTime.UtcNow
                : ToUtc(assignment.CompletedAt.Value);
            DateTime assignedAt = ToUtc(assignment.AssignedAt);
            double durationMs = Math.Max(0, (completedAt - assignedAt).TotalMilliseconds);

            ZeronEventBus.PublishObject(
                ZeronEventTopics.AssignmentCompleted,
                new AssignmentCompletedInfoType
                {
                    AssignmentId = assignment.Id,
                    TaskId = assignment.TaskId,
                    AgentKey = assignment.Agent?.AgentKey,
                    TargetApi = assignment.Task?.TargetApi,
                    Status = assignment.Status,
                    AssignedAt = assignedAt,
                    CompletedAt = completedAt,
                    DurationMs = durationMs
                },
                source: "server",
                correlationId: assignment.Id.ToString("N"));
        }

        /// <summary>
        /// Normalizes a DateTime to UTC.
        /// </summary>
        /// <param name="value">Input time.</param>
        /// <returns>UTC DateTime.</returns>
        private static DateTime ToUtc(
            DateTime value)
        {
            return value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
            };
        }
    }
}
