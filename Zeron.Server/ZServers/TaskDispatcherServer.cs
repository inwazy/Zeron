// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text.Json;
using Zeron.Server.Data;
using Zeron.Server.Data.Entities;
using Zeron.ZCore;
using Zeron.ZCore.Type;

namespace Zeron.Server.ZServers
{
    /// <summary>
    /// TaskDispatcherServer
    /// </summary>
    public class TaskDispatcherServer
    {
        private readonly ZeronServerDbContext m_DbContext;
        private readonly CommandPublisherServer m_CommandPublisher;

        /// <summary>
        /// TaskDispatcherServer
        /// </summary>
        /// <param name="dbContext"></param>
        /// <param name="commandPublisher"></param>
        /// <returns>Returns void.</returns>
        public TaskDispatcherServer(ZeronServerDbContext dbContext, CommandPublisherServer commandPublisher)
        {
            m_DbContext = dbContext;
            m_CommandPublisher = commandPublisher;
        }

        /// <summary>
        /// CreateTaskAsync
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns TaskEntity.</returns>
        public async Task<TaskEntity> CreateTaskAsync(TaskCreateRequestType request, CancellationToken cancellationToken = default)
        {
            List<AgentEntity> targetAgents = await ResolveTargetAgentsAsync(request, cancellationToken);

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
        public async Task<int> DispatchPendingAssignmentsAsync(CancellationToken cancellationToken = default)
        {
            List<TaskAssignmentEntity> pendingAssignments = await m_DbContext.TaskAssignments
                .Include(assignment => assignment.Task)
                .Include(assignment => assignment.Agent)
                .Where(assignment => assignment.Status == "pending"
                    && assignment.Agent != null
                    && assignment.Agent.Status == "online")
                .ToListAsync(cancellationToken);

            int dispatched = 0;

            foreach (TaskAssignmentEntity assignment in pendingAssignments)
            {
                if (assignment.Task == null || assignment.Agent == null)
                {
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
            }

            if (dispatched > 0)
            {
                await m_DbContext.SaveChangesAsync(cancellationToken);
            }

            return dispatched;
        }

        /// <summary>
        /// ReportResultAsync
        /// </summary>
        /// <param name="report"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns bool.</returns>
        public async Task<bool> ReportResultAsync(TaskResultReportType report, CancellationToken cancellationToken = default)
        {
            if (!Guid.TryParse(report.AssignmentId, out Guid assignmentId))
            {
                return false;
            }

            TaskAssignmentEntity? assignment = await m_DbContext.TaskAssignments
                .Include(item => item.Task)
                .FirstOrDefaultAsync(item => item.Id == assignmentId, cancellationToken);

            if (assignment == null)
            {
                return false;
            }

            assignment.Status = report.Success ? "completed" : "failed";
            assignment.CompletedAt = DateTime.UtcNow;

            m_DbContext.TaskResults.Add(new TaskResultEntity
            {
                Id = Guid.NewGuid(),
                AssignmentId = assignment.Id,
                Success = report.Success,
                ResponseJson = report.ResponseJson,
                ErrorMessage = report.ErrorMessage,
                CompletedAt = DateTime.UtcNow
            });

            if (assignment.Task != null)
            {
                bool anyPending = await m_DbContext.TaskAssignments
                    .AnyAsync(item => item.TaskId == assignment.TaskId
                        && item.Status != "completed"
                        && item.Status != "failed", cancellationToken);

                assignment.Task.Status = anyPending ? "running" : (report.Success ? "completed" : "failed");
            }

            await m_DbContext.SaveChangesAsync(cancellationToken);

            return true;
        }

        /// <summary>
        /// GetTasksAsync
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns task list.</returns>
        public async Task<List<TaskEntity>> GetTasksAsync(CancellationToken cancellationToken = default)
        {
            return await m_DbContext.Tasks
                .Include(task => task.Assignments)
                .OrderByDescending(task => task.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// GetTaskAsync
        /// </summary>
        /// <param name="taskId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns task or null.</returns>
        public async Task<TaskEntity?> GetTaskAsync(Guid taskId, CancellationToken cancellationToken = default)
        {
            return await m_DbContext.Tasks
                .Include(task => task.Assignments)
                .ThenInclude(assignment => assignment.Result)
                .FirstOrDefaultAsync(task => task.Id == taskId, cancellationToken);
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
    }
}
