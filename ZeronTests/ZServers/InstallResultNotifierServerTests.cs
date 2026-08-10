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
    public class InstallResultNotifierServerTests
    {
        /// <summary>
        /// Self-service install.completed creates a DeviceOwner dashboard notification.
        /// </summary>
        [TestMethod()]
        public async Task NotifyFromSelfInstallCompletedCreatesNotificationTest()
        {
            await using ZeronServerDbContext dbContext = CreateContext();
            (Guid userId, Guid assignmentId) = await SeedSelfInstallAsync(dbContext, "owner-a", "notify-agent");

            InstallResultNotifierServer notifier = CreateNotifier(dbContext, notify: true, email: false);
            int count = await notifier.NotifyFromInstallEventAsync(new AgentEventReportType
            {
                AgentId = "notify-agent",
                Topic = "install.completed",
                Payload = "{\"package\":\"ccleaner\",\"success\":true,\"exitCode\":0,\"assignmentId\":\""
                    + assignmentId
                    + "\"}"
            });

            Assert.AreEqual(1, count);

            List<UserNotificationEntity> notes = await dbContext.UserNotifications.ToListAsync();

            Assert.AreEqual(1, notes.Count);
            Assert.AreEqual(userId, notes[0].UserId);
            Assert.AreEqual(UserNotificationServer.KindInstallResult, notes[0].Kind);
            Assert.IsTrue(notes[0].Success);
            Assert.AreEqual("ccleaner", notes[0].PackageName);
        }

        /// <summary>
        /// Staff (non-self) deploys do not notify DeviceOwners.
        /// </summary>
        [TestMethod()]
        public async Task NotifySkipsNonSelfDeployTest()
        {
            await using ZeronServerDbContext dbContext = CreateContext();
            (_, Guid assignmentId) = await SeedSelfInstallAsync(
                dbContext,
                "owner-b",
                "staff-agent",
                taskName: "deploy-ccleaner");

            InstallResultNotifierServer notifier = CreateNotifier(dbContext, notify: true, email: false);
            int count = await notifier.NotifyFromInstallEventAsync(new AgentEventReportType
            {
                AgentId = "staff-agent",
                Topic = "install.completed",
                Payload = "{\"package\":\"ccleaner\",\"success\":true,\"exitCode\":0,\"assignmentId\":\""
                    + assignmentId
                    + "\"}"
            });

            Assert.AreEqual(0, count);
            Assert.AreEqual(0, await dbContext.UserNotifications.CountAsync());
        }

        /// <summary>
        /// Event ingest of self-service install.failed creates an unread notification.
        /// </summary>
        [TestMethod()]
        public async Task EventIngestCreatesInstallResultNotificationTest()
        {
            await using ZeronServerDbContext dbContext = CreateContext();
            (Guid userId, Guid assignmentId) = await SeedSelfInstallAsync(dbContext, "owner-c", "fail-agent");

            TaskDispatcherServer taskDispatcher = new(dbContext, new CommandPublisherServer(new ServerSettings()));
            UserNotificationServer notificationServer = new(dbContext);
            InstallResultNotifierServer notifier = CreateNotifier(dbContext, notify: true, email: false);
            EventIngestorServer eventIngestor = new(dbContext, taskDispatcher, installResultNotifier: notifier);

            bool ingested = await eventIngestor.IngestEventAsync(new AgentEventReportType
            {
                AgentId = "fail-agent",
                Topic = "install.failed",
                Payload = "{\"package\":\"badpkg\",\"success\":false,\"exitCode\":1,\"assignmentId\":\""
                    + assignmentId
                    + "\"}"
            });

            Assert.IsTrue(ingested);

            List<UserNotificationInfoType> unread = await notificationServer.GetNotificationsAsync(userId, unreadOnly: true);

            Assert.AreEqual(1, unread.Count);
            Assert.IsFalse(unread[0].Success);
            Assert.AreEqual("badpkg", unread[0].PackageName);
        }

        /// <summary>
        /// SeedSelfInstallAsync
        /// </summary>
        private static async Task<(Guid UserId, Guid AssignmentId)> SeedSelfInstallAsync(
            ZeronServerDbContext dbContext,
            string username,
            string agentKey,
            string taskName = "self-install-ccleaner-20260810120000")
        {
            Guid userId = Guid.NewGuid();
            Guid agentId = Guid.NewGuid();
            Guid taskId = Guid.NewGuid();
            Guid assignmentId = Guid.NewGuid();
            DateTime now = DateTime.UtcNow;

            dbContext.Users.Add(new UserEntity
            {
                Id = userId,
                Username = username,
                PasswordHash = "x",
                Role = ServerRoles.DeviceOwner,
                IsActive = true,
                CreatedAt = now,
                Email = username + "@example.com"
            });

            dbContext.Agents.Add(new AgentEntity
            {
                Id = agentId,
                AgentKey = agentKey,
                Status = "online",
                RegisteredAt = now,
                LastSeenAt = now,
                LastHeartbeatAt = now
            });

            dbContext.UserAgentBindings.Add(new UserAgentBindingEntity
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                AgentKey = agentKey,
                BoundAt = now
            });

            dbContext.Tasks.Add(new TaskEntity
            {
                Id = taskId,
                Name = taskName,
                TargetApi = "ManagedPackage",
                Command = "install ccleaner",
                Status = "running",
                CreatedAt = now
            });

            dbContext.TaskAssignments.Add(new TaskAssignmentEntity
            {
                Id = assignmentId,
                TaskId = taskId,
                AgentId = agentId,
                Status = "running",
                AssignedAt = now
            });

            await dbContext.SaveChangesAsync();

            return (userId, assignmentId);
        }

        /// <summary>
        /// CreateNotifier
        /// </summary>
        private static InstallResultNotifierServer CreateNotifier(
            ZeronServerDbContext dbContext,
            bool notify,
            bool email)
        {
            return new InstallResultNotifierServer(
                dbContext,
                new UserNotificationServer(dbContext),
                new ServerSettings
                {
                    InstallResultNotifyEnabled = notify,
                    InstallResultEmailEnabled = email
                });
        }

        /// <summary>
        /// CreateContext
        /// </summary>
        private static ZeronServerDbContext CreateContext()
        {
            DbContextOptions<ZeronServerDbContext> options = new DbContextOptionsBuilder<ZeronServerDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new ZeronServerDbContext(options);
        }
    }
}
