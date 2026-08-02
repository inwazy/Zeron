// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Microsoft.EntityFrameworkCore;
using NCrontab;
using System.Globalization;
using System.Text.Json;
using Zeron.Server.Data;
using Zeron.Server.Data.Entities;
using Zeron.ZCore;
using Zeron.ZCore.Type;

namespace Zeron.Server.ZServers
{
    /// <summary>
    /// TaskScheduleServer
    /// </summary>
    public class TaskScheduleServer
    {
        // Database context
        private readonly ZeronServerDbContext m_DbContext;

        // Task dispatcher server
        private readonly TaskDispatcherServer m_TaskDispatcher;

        /// <summary>
        /// TaskScheduleServer
        /// </summary>
        /// <param name="dbContext"></param>
        /// <param name="taskDispatcher"></param>
        /// <returns>Returns void.</returns>
        public TaskScheduleServer(
            ZeronServerDbContext dbContext, 
            TaskDispatcherServer taskDispatcher)
        {
            m_DbContext = dbContext;
            m_TaskDispatcher = taskDispatcher;
        }

        /// <summary>
        /// GetSchedulesAsync
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns schedule list.</returns>
        public async Task<List<TaskScheduleInfoType>> GetSchedulesAsync(
            CancellationToken cancellationToken = default)
        {
            List<TaskScheduleEntity> schedules = await m_DbContext.TaskSchedules
                .OrderBy(schedule => schedule.Name)
                .ToListAsync(cancellationToken);

            return schedules.Select(ToInfo).ToList();
        }

        /// <summary>
        /// GetScheduleAsync
        /// </summary>
        /// <param name="scheduleId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns schedule or null.</returns>
        public async Task<TaskScheduleInfoType?> GetScheduleAsync(
            Guid scheduleId,
            CancellationToken cancellationToken = default)
        {
            TaskScheduleEntity? schedule = await m_DbContext.TaskSchedules
                .FirstOrDefaultAsync(item => item.Id == scheduleId, cancellationToken);

            return schedule == null ? null : ToInfo(schedule);
        }

        /// <summary>
        /// CreateScheduleAsync
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns created schedule or error.</returns>
        public async Task<(TaskScheduleInfoType? Schedule, string? Error)> CreateScheduleAsync(
            TaskScheduleCreateRequestType request,
            CancellationToken cancellationToken = default)
        {
            string? error = ValidateCreateRequest(request, out CrontabSchedule? cronSchedule);

            if (error != null || cronSchedule == null)
            {
                return (null, error ?? "Invalid schedule.");
            }

            string name = request.Name!.Trim();
            bool exists = await m_DbContext.TaskSchedules
                .AnyAsync(item => item.Name == name, cancellationToken);

            if (exists)
            {
                return (null, "Schedule name already exists.");
            }

            DateTime now = DateTime.UtcNow;
            TaskScheduleEntity schedule = new()
            {
                Id = Guid.NewGuid(),
                Name = name,
                Description = request.Description,
                Cron = request.Cron!.Trim(),
                Enabled = request.Enabled,
                TargetApi = request.TargetApi!.Trim(),
                Command = request.Command ?? "",
                TargetType = NormalizeTargetType(request.TargetType),
                TargetFilterJson = SerializeFilter(request),
                NextRunAt = request.Enabled ? ComputeNextRunUtc(cronSchedule) : null,
                CreatedAt = now,
                UpdatedAt = now
            };

            m_DbContext.TaskSchedules.Add(schedule);
            await m_DbContext.SaveChangesAsync(cancellationToken);

            ZNLogger.Common.Info(string.Format(CultureInfo.InvariantCulture,
                "TaskScheduleServer created schedule '{0}' ({1}).", schedule.Name, schedule.Cron));

            return (ToInfo(schedule), null);
        }

        /// <summary>
        /// UpdateScheduleAsync
        /// </summary>
        /// <param name="scheduleId"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns updated schedule or error.</returns>
        public async Task<(TaskScheduleInfoType? Schedule, string? Error)> UpdateScheduleAsync(
            Guid scheduleId,
            TaskScheduleUpdateRequestType request,
            CancellationToken cancellationToken = default)
        {
            TaskScheduleEntity? schedule = await m_DbContext.TaskSchedules
                .FirstOrDefaultAsync(item => item.Id == scheduleId, cancellationToken);

            if (schedule == null)
            {
                return (null, "Schedule not found.");
            }

            if (request.Name != null)
            {
                string name = request.Name.Trim();

                if (string.IsNullOrWhiteSpace(name))
                {
                    return (null, "Name is required.");
                }

                bool exists = await m_DbContext.TaskSchedules.AnyAsync(
                    item => item.Name == name && item.Id != scheduleId,
                    cancellationToken);

                if (exists)
                {
                    return (null, "Schedule name already exists.");
                }

                schedule.Name = name;
            }

            if (request.Description != null)
            {
                schedule.Description = request.Description;
            }

            if (request.Cron != null)
            {
                if (!TryParseCron(request.Cron, out CrontabSchedule? _, out string? cronError))
                {
                    return (null, cronError);
                }

                schedule.Cron = request.Cron.Trim();
            }

            if (request.TargetApi != null)
            {
                if (string.IsNullOrWhiteSpace(request.TargetApi))
                {
                    return (null, "Target API is required.");
                }

                schedule.TargetApi = request.TargetApi.Trim();
            }

            if (request.Command != null)
            {
                schedule.Command = request.Command;
            }

            if (request.TargetType != null
                || request.AgentIds != null
                || request.HostnamePattern != null)
            {
                schedule.TargetType = NormalizeTargetType(request.TargetType ?? schedule.TargetType);
                schedule.TargetFilterJson = SerializeFilter(new TaskScheduleCreateRequestType
                {
                    TargetType = schedule.TargetType,
                    AgentIds = request.AgentIds ?? DeserializeFilter(schedule.TargetFilterJson).AgentIds,
                    HostnamePattern = request.HostnamePattern
                        ?? DeserializeFilter(schedule.TargetFilterJson).HostnamePattern
                });
            }

            if (request.Enabled.HasValue)
            {
                schedule.Enabled = request.Enabled.Value;
            }

            if (!TryParseCron(schedule.Cron, out CrontabSchedule? cronSchedule, out string? parseError)
                || cronSchedule == null)
            {
                return (null, parseError ?? "Invalid cron expression.");
            }

            schedule.NextRunAt = schedule.Enabled ? ComputeNextRunUtc(cronSchedule) : null;
            schedule.UpdatedAt = DateTime.UtcNow;

            await m_DbContext.SaveChangesAsync(cancellationToken);

            return (ToInfo(schedule), null);
        }

        /// <summary>
        /// DeleteScheduleAsync
        /// </summary>
        /// <param name="scheduleId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns bool.</returns>
        public async Task<bool> DeleteScheduleAsync(
            Guid scheduleId, 
            CancellationToken cancellationToken = default)
        {
            TaskScheduleEntity? schedule = await m_DbContext.TaskSchedules
                .FirstOrDefaultAsync(item => item.Id == scheduleId, cancellationToken);

            if (schedule == null)
            {
                return false;
            }

            m_DbContext.TaskSchedules.Remove(schedule);
            await m_DbContext.SaveChangesAsync(cancellationToken);

            return true;
        }

        /// <summary>
        /// SetEnabledAsync
        /// </summary>
        /// <param name="scheduleId"></param>
        /// <param name="enabled"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns updated schedule or error.</returns>
        public async Task<(TaskScheduleInfoType? Schedule, string? Error)> SetEnabledAsync(
            Guid scheduleId,
            bool enabled,
            CancellationToken cancellationToken = default)
        {
            return await UpdateScheduleAsync(
                scheduleId,
                new TaskScheduleUpdateRequestType { Enabled = enabled },
                cancellationToken);
        }

        /// <summary>
        /// TriggerNowAsync
        /// </summary>
        /// <param name="scheduleId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns spawned task id or error.</returns>
        public async Task<(Guid? TaskId, string? Error)> TriggerNowAsync(
            Guid scheduleId,
            CancellationToken cancellationToken = default)
        {
            TaskScheduleEntity? schedule = await m_DbContext.TaskSchedules
                .FirstOrDefaultAsync(item => item.Id == scheduleId, cancellationToken);

            if (schedule == null)
            {
                return (null, "Schedule not found.");
            }

            TaskEntity task = await SpawnTaskAsync(schedule, cancellationToken);

            return (task.Id, null);
        }

        /// <summary>
        /// ProcessDueSchedulesAsync
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns triggered count.</returns>
        public async Task<int> ProcessDueSchedulesAsync(
            CancellationToken cancellationToken = default)
        {
            DateTime nowUtc = DateTime.UtcNow;
            List<TaskScheduleEntity> dueSchedules = await m_DbContext.TaskSchedules
                .Where(schedule => schedule.Enabled
                    && schedule.NextRunAt != null
                    && schedule.NextRunAt <= nowUtc)
                .ToListAsync(cancellationToken);

            int triggered = 0;

            foreach (TaskScheduleEntity schedule in dueSchedules)
            {
                if (!TryParseCron(schedule.Cron, out CrontabSchedule? cronSchedule, out _)
                    || cronSchedule == null)
                {
                    schedule.Enabled = false;
                    schedule.NextRunAt = null;
                    schedule.UpdatedAt = nowUtc;
                    continue;
                }

                await SpawnTaskAsync(schedule, cancellationToken, cronSchedule);
                triggered++;
            }

            if (dueSchedules.Count > 0)
            {
                await m_DbContext.SaveChangesAsync(cancellationToken);
            }

            return triggered;
        }

        /// <summary>
        /// SpawnTaskAsync
        /// </summary>
        /// <param name="schedule"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="cronSchedule"></param>
        /// <returns>Returns TaskEntity.</returns>
        private async Task<TaskEntity> SpawnTaskAsync(
            TaskScheduleEntity schedule,
            CancellationToken cancellationToken,
            CrontabSchedule? cronSchedule = null)
        {
            TaskScheduleCreateRequestType filter = DeserializeFilter(schedule.TargetFilterJson);
            string stamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);

            TaskEntity task = await m_TaskDispatcher.CreateTaskAsync(new TaskCreateRequestType
            {
                Name = $"{schedule.Name}-{stamp}",
                Description = schedule.Description ?? $"Scheduled from '{schedule.Name}'",
                TargetApi = schedule.TargetApi,
                Command = schedule.Command,
                TargetType = schedule.TargetType,
                AgentIds = filter.AgentIds,
                HostnamePattern = filter.HostnamePattern
            }, cancellationToken);

            DateTime nowUtc = DateTime.UtcNow;
            schedule.LastRunAt = nowUtc;
            schedule.LastTaskId = task.Id;
            schedule.UpdatedAt = nowUtc;

            if (cronSchedule == null)
            {
                TryParseCron(schedule.Cron, out cronSchedule, out _);
            }

            schedule.NextRunAt = schedule.Enabled && cronSchedule != null
                ? ComputeNextRunUtc(cronSchedule)
                : null;

            await m_DbContext.SaveChangesAsync(cancellationToken);

            ZNLogger.Common.Info(string.Format(CultureInfo.InvariantCulture,
                "TaskScheduleServer spawned task '{0}' from schedule '{1}'.", task.Name, schedule.Name));

            return task;
        }

        /// <summary>
        /// ValidateCreateRequest
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cronSchedule"></param>
        /// <returns>Returns error or null.</returns>
        private static string? ValidateCreateRequest(
            TaskScheduleCreateRequestType request,
            out CrontabSchedule? cronSchedule)
        {
            cronSchedule = null;

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return "Name is required.";
            }

            if (string.IsNullOrWhiteSpace(request.TargetApi))
            {
                return "Target API is required.";
            }

            if (!TryParseCron(request.Cron, out cronSchedule, out string? cronError))
            {
                return cronError;
            }

            return null;
        }

        /// <summary>
        /// TryParseCron
        /// </summary>
        /// <param name="cron"></param>
        /// <param name="schedule"></param>
        /// <param name="error"></param>
        /// <returns>Returns bool.</returns>
        internal static bool TryParseCron(
            string? cron, 
            out CrontabSchedule? schedule, 
            out string? error)
        {
            schedule = null;
            error = null;

            if (string.IsNullOrWhiteSpace(cron))
            {
                error = "Cron expression is required.";
                return false;
            }

            try
            {
                schedule = CrontabSchedule.Parse(cron.Trim());
                return true;
            }
            catch (Exception ex)
            {
                error = "Invalid cron expression: " + ex.Message;
                return false;
            }
        }

        /// <summary>
        /// ComputeNextRunUtc
        /// </summary>
        /// <param name="schedule"></param>
        /// <returns>Returns next run UTC.</returns>
        internal static DateTime ComputeNextRunUtc(
            CrontabSchedule schedule)
        {
            DateTime nextLocal = schedule.GetNextOccurrence(DateTime.Now);

            return nextLocal.Kind == DateTimeKind.Utc
                ? nextLocal
                : nextLocal.ToUniversalTime();
        }

        /// <summary>
        /// NormalizeTargetType
        /// </summary>
        /// <param name="targetType"></param>
        /// <returns>Returns normalized target type.</returns>
        private static string NormalizeTargetType(
            string? targetType)
        {
            string value = targetType?.Trim().ToLowerInvariant() ?? "all";

            return value is "agent" or "filter" ? value : "all";
        }

        /// <summary>
        /// SerializeFilter
        /// </summary>
        /// <param name="request"></param>
        /// <returns>Returns JSON.</returns>
        private static string SerializeFilter(
            TaskScheduleCreateRequestType request)
        {
            return JsonSerializer.Serialize(new
            {
                request.TargetType,
                request.AgentIds,
                request.HostnamePattern
            });
        }

        /// <summary>
        /// DeserializeFilter
        /// </summary>
        /// <param name="json"></param>
        /// <returns>Returns filter request.</returns>
        private static TaskScheduleCreateRequestType DeserializeFilter(
            string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new TaskScheduleCreateRequestType();
            }

            try
            {
                return JsonSerializer.Deserialize<TaskScheduleCreateRequestType>(json)
                    ?? new TaskScheduleCreateRequestType();
            }
            catch
            {
                return new TaskScheduleCreateRequestType();
            }
        }

        /// <summary>
        /// ToInfo
        /// </summary>
        /// <param name="schedule"></param>
        /// <returns>Returns TaskScheduleInfoType.</returns>
        private static TaskScheduleInfoType ToInfo(
            TaskScheduleEntity schedule)
        {
            TaskScheduleCreateRequestType filter = DeserializeFilter(schedule.TargetFilterJson);

            return new TaskScheduleInfoType
            {
                Id = schedule.Id,
                Name = schedule.Name,
                Description = schedule.Description,
                Cron = schedule.Cron,
                Enabled = schedule.Enabled,
                TargetApi = schedule.TargetApi,
                Command = schedule.Command,
                TargetType = schedule.TargetType,
                AgentIds = filter.AgentIds,
                HostnamePattern = filter.HostnamePattern,
                LastRunAt = schedule.LastRunAt,
                NextRunAt = schedule.NextRunAt,
                LastTaskId = schedule.LastTaskId,
                CreatedAt = schedule.CreatedAt,
                UpdatedAt = schedule.UpdatedAt
            };
        }
    }
}
