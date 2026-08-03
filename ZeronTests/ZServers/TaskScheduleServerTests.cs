// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zeron.Server.Data;
using Zeron.Server.Data.Entities;
using Zeron.Server.ZCore;
using Zeron.Server.ZServers;
using Zeron.ZCore.Type;

namespace Zeron.Server.ZServers.Tests
{
    [TestClass()]
    public class TaskScheduleServerTests
    {
        /// <summary>
        /// CreateScheduleAsync stores schedule and computes NextRunAt.
        /// </summary>
        [TestMethod()]
        public async Task CreateScheduleAsyncComputesNextRunTest()
        {
            await using ZeronServerDbContext dbContext = CreateContext();
            TaskScheduleServer scheduleServer = CreateScheduleServer(dbContext);

            (TaskScheduleInfoType? schedule, string? error) = await scheduleServer.CreateScheduleAsync(
                new TaskScheduleCreateRequestType
                {
                    Name = "daily-health",
                    Cron = "0 8 * * *",
                    Enabled = true,
                    TargetApi = "HealthCheck",
                    TargetType = "all"
                });

            Assert.IsNull(error);
            Assert.IsNotNull(schedule);
            Assert.AreEqual("daily-health", schedule!.Name);
            Assert.IsTrue(schedule.Enabled);
            Assert.IsNotNull(schedule.NextRunAt);
        }

        /// <summary>
        /// CreateScheduleAsync rejects invalid cron.
        /// </summary>
        [TestMethod()]
        public async Task CreateScheduleAsyncRejectsInvalidCronTest()
        {
            await using ZeronServerDbContext dbContext = CreateContext();
            TaskScheduleServer scheduleServer = CreateScheduleServer(dbContext);

            (TaskScheduleInfoType? schedule, string? error) = await scheduleServer.CreateScheduleAsync(
                new TaskScheduleCreateRequestType
                {
                    Name = "bad-cron",
                    Cron = "not-a-cron",
                    TargetApi = "HealthCheck"
                });

            Assert.IsNull(schedule);
            Assert.IsNotNull(error);
            Assert.IsTrue(error!.StartsWith("Invalid cron", StringComparison.Ordinal));
        }

        /// <summary>
        /// TriggerNowAsync spawns a task via dispatcher.
        /// </summary>
        [TestMethod()]
        public async Task TriggerNowAsyncSpawnsTaskTest()
        {
            await using ZeronServerDbContext dbContext = CreateContext();

            dbContext.Agents.Add(new AgentEntity
            {
                Id = Guid.NewGuid(),
                AgentKey = "sched-agent",
                Status = "online",
                MachineName = "HOST",
                RegisteredAt = DateTime.UtcNow,
                LastHeartbeatAt = DateTime.UtcNow
            });
            await dbContext.SaveChangesAsync();

            TaskScheduleServer scheduleServer = CreateScheduleServer(dbContext);

            (TaskScheduleInfoType? schedule, string? _) = await scheduleServer.CreateScheduleAsync(
                new TaskScheduleCreateRequestType
                {
                    Name = "manual-run",
                    Cron = "*/5 * * * *",
                    Enabled = true,
                    TargetApi = "HealthCheck",
                    TargetType = "all"
                });

            (Guid? taskId, string? error) = await scheduleServer.TriggerNowAsync(schedule!.Id);

            Assert.IsNull(error);
            Assert.IsNotNull(taskId);

            TaskEntity? task = await dbContext.Tasks.FirstOrDefaultAsync(item => item.Id == taskId);

            Assert.IsNotNull(task);
            Assert.IsTrue(task!.Name.StartsWith("manual-run-", StringComparison.Ordinal));
            Assert.AreEqual("HealthCheck", task.TargetApi);
        }

        /// <summary>
        /// ProcessDueSchedulesAsync triggers overdue enabled schedules.
        /// </summary>
        [TestMethod()]
        public async Task ProcessDueSchedulesAsyncTriggersDueScheduleTest()
        {
            await using ZeronServerDbContext dbContext = CreateContext();

            dbContext.Agents.Add(new AgentEntity
            {
                Id = Guid.NewGuid(),
                AgentKey = "due-agent",
                Status = "online",
                MachineName = "HOST",
                RegisteredAt = DateTime.UtcNow,
                LastHeartbeatAt = DateTime.UtcNow
            });

            TaskScheduleEntity schedule = new()
            {
                Id = Guid.NewGuid(),
                Name = "due-now",
                Cron = "*/5 * * * *",
                Enabled = true,
                TargetApi = "ServerInfo",
                Command = "",
                TargetType = "all",
                NextRunAt = DateTime.UtcNow.AddMinutes(-1),
                CreatedAt = DateTime.UtcNow.AddHours(-1),
                UpdatedAt = DateTime.UtcNow.AddHours(-1)
            };

            dbContext.TaskSchedules.Add(schedule);
            await dbContext.SaveChangesAsync();

            TaskScheduleServer scheduleServer = CreateScheduleServer(dbContext);
            int triggered = await scheduleServer.ProcessDueSchedulesAsync();

            Assert.AreEqual(1, triggered);

            TaskScheduleEntity updated = await dbContext.TaskSchedules.SingleAsync();

            Assert.IsNotNull(updated.LastRunAt);
            Assert.IsNotNull(updated.LastTaskId);
            Assert.IsNotNull(updated.NextRunAt);
            Assert.IsTrue(updated.NextRunAt > DateTime.UtcNow.AddMinutes(-1));
        }

        private static TaskScheduleServer CreateScheduleServer(ZeronServerDbContext dbContext)
        {
            TaskDispatcherServer taskDispatcher = new(dbContext, new CommandPublisherServer(new ServerSettings()));

            return new TaskScheduleServer(dbContext, taskDispatcher);
        }

        private static ZeronServerDbContext CreateContext()
        {
            DbContextOptions<ZeronServerDbContext> options = new DbContextOptionsBuilder<ZeronServerDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new ZeronServerDbContext(options);
        }
    }
}
