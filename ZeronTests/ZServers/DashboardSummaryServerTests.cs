// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zeron.Server.Data;
using Zeron.Server.Data.Entities;
using Zeron.Server.ZCore;
using Zeron.Server.ZCore.Type;
using Zeron.Server.ZServers;
using Zeron.ZCore.Type;

namespace Zeron.Server.ZServers.Tests
{
    [TestClass()]
    public class DashboardSummaryServerTests
    {
        /// <summary>
        /// GetSummaryAsync returns agent and alert counts.
        /// </summary>
        [TestMethod()]
        public async Task GetSummaryAsyncReturnsCountsTest()
        {
            string dbName = Guid.NewGuid().ToString();
            await using ZeronServerDbContext dbContext = CreateContext(dbName);

            dbContext.Agents.AddRange(
                new AgentEntity
                {
                    Id = Guid.NewGuid(),
                    AgentKey = "online-1",
                    Status = "online",
                    MachineName = "HOST-A",
                    RegisteredAt = DateTime.UtcNow.AddHours(-1),
                    LastHeartbeatAt = DateTime.UtcNow
                },
                new AgentEntity
                {
                    Id = Guid.NewGuid(),
                    AgentKey = "offline-1",
                    Status = "offline",
                    MachineName = "HOST-B",
                    RegisteredAt = DateTime.UtcNow.AddHours(-2),
                    LastHeartbeatAt = DateTime.UtcNow.AddMinutes(-10)
                });

            dbContext.Alerts.Add(new AlertEntity
            {
                Id = Guid.NewGuid(),
                RuleType = AlertRuleType.AgentOffline,
                AgentKey = "offline-1",
                Title = "Agent offline",
                Message = "offline-1 is offline",
                Severity = "warning",
                Status = AlertStatusesType.Open,
                CreatedAt = DateTime.UtcNow
            });

            dbContext.Tasks.Add(new TaskEntity
            {
                Id = Guid.NewGuid(),
                Name = "summary-task",
                TargetApi = "HealthCheck",
                Command = "",
                TargetType = "all",
                Status = "pending",
                CreatedAt = DateTime.UtcNow
            });

            await dbContext.SaveChangesAsync();

            Guid ownerId = Guid.NewGuid();
            dbContext.Users.Add(new UserEntity
            {
                Id = ownerId,
                Username = "owner-dash",
                PasswordHash = "x",
                Role = ServerRoles.DeviceOwner,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
            dbContext.UserNotifications.AddRange(
                new UserNotificationEntity
                {
                    Id = Guid.NewGuid(),
                    UserId = ownerId,
                    Kind = UserNotificationServer.KindInstallResult,
                    Title = "install failed",
                    Message = "failed",
                    Success = false,
                    CreatedAt = DateTime.UtcNow
                },
                new UserNotificationEntity
                {
                    Id = Guid.NewGuid(),
                    UserId = ownerId,
                    Kind = UserNotificationServer.KindInstallResult,
                    Title = "install ok",
                    Message = "ok",
                    Success = true,
                    CreatedAt = DateTime.UtcNow
                },
                new UserNotificationEntity
                {
                    Id = Guid.NewGuid(),
                    UserId = ownerId,
                    Kind = UserNotificationServer.KindInstallResult,
                    Title = "already read",
                    Message = "read",
                    Success = false,
                    CreatedAt = DateTime.UtcNow,
                    ReadAt = DateTime.UtcNow
                });
            await dbContext.SaveChangesAsync();

            DashboardSummaryServer summaryServer = CreateSummaryServer(dbContext);
            DashboardSummaryType summary = await summaryServer.GetSummaryAsync();

            Assert.AreEqual(1, summary.AgentsOnline);
            Assert.AreEqual(1, summary.AgentsOffline);
            Assert.AreEqual(2, summary.AgentsTotal);
            Assert.AreEqual(1, summary.OpenAlerts);
            Assert.AreEqual(1, summary.ActiveTasks);
            Assert.AreEqual(1, summary.RecentAlerts.Count);
            Assert.AreEqual(1, summary.RecentTasks.Count);
            Assert.AreEqual(2, summary.RecentAgents.Count);
            Assert.AreEqual(0, summary.CatalogSyncHealthy);
            Assert.AreEqual(1, summary.CatalogSyncUnhealthy);
            Assert.AreEqual(1, summary.CatalogSyncOffline);
            Assert.AreEqual(2, summary.UnreadInstallNotifications);
            Assert.AreEqual(1, summary.UnreadInstallFailures);
            Assert.IsNotNull(summary.Security);
            Assert.AreEqual("insecure", summary.Security.OverallStatus);
            Assert.IsFalse(summary.Security.CurveEnabled);
            Assert.IsFalse(summary.Security.AgentHmacRequired);
            Assert.IsTrue(summary.Security.Recommendations.Count > 0);
        }

        /// <summary>
        /// BuildSecurityStatus reports hardened when CURVE and HMAC are enabled.
        /// </summary>
        [TestMethod()]
        public void BuildSecurityStatusHardenedTest()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "zeron-sec-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            string publicKeyPath = Path.Combine(tempDir, "curve-server.public");

            try
            {
                File.WriteAllBytes(publicKeyPath, new byte[32]);

                DashboardSecurityStatusType status = DashboardSummaryServer.BuildSecurityStatus(new ServerSettings
                {
                    CurveEnabled = true,
                    CurvePublicKeyPath = publicKeyPath,
                    AgentHmacRequired = true,
                    RequireHttpsAgents = true
                });

                Assert.AreEqual("hardened", status.OverallStatus);
                Assert.IsTrue(status.CurveEnabled);
                Assert.IsTrue(status.CurvePublicKeyPresent);
                Assert.IsTrue(status.AgentHmacRequired);
                Assert.IsTrue(status.RequireHttpsAgents);
                Assert.AreEqual(0, status.Recommendations.Count);
            }
            finally
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }

        /// <summary>
        /// BuildSecurityStatus reports partial when only one transport control is on.
        /// </summary>
        [TestMethod()]
        public void BuildSecurityStatusPartialTest()
        {
            DashboardSecurityStatusType status = DashboardSummaryServer.BuildSecurityStatus(new ServerSettings
            {
                CurveEnabled = true,
                AgentHmacRequired = false,
                RequireHttpsAgents = false
            });

            Assert.AreEqual("partial", status.OverallStatus);
            Assert.IsTrue(status.Recommendations.Any(item => item.Contains("AgentHmacRequired", StringComparison.Ordinal)));
        }

        private static DashboardSummaryServer CreateSummaryServer(ZeronServerDbContext dbContext)
        {
            ServerSettings settings = new() { HeartbeatTimeoutSeconds = 90 };
            AgentManagerServer agentManager = new(dbContext);
            AgentDiagnosticServer diagnosticServer = new(dbContext, settings);
            TaskDispatcherServer taskDispatcher = new(dbContext, new CommandPublisherServer(settings));
            EventIngestorServer eventIngestor = new(dbContext, taskDispatcher);
            AlertNotifierServer alertNotifier = new(settings);
            AlertRuleServer alertRuleServer = new(dbContext, alertNotifier);
            ManagedPackageCatalogServer catalog = new(dbContext);
            CatalogSyncHealthServer catalogSyncHealth = new(dbContext, catalog, settings);
            UserNotificationServer userNotifications = new(dbContext);

            return new DashboardSummaryServer(
                agentManager,
                diagnosticServer,
                taskDispatcher,
                eventIngestor,
                alertRuleServer,
                settings,
                catalogSyncHealth,
                userNotifications);
        }

        private static ZeronServerDbContext CreateContext(string dbName)
        {
            DbContextOptions<ZeronServerDbContext> options = new DbContextOptionsBuilder<ZeronServerDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;

            return new ZeronServerDbContext(options);
        }
    }
}
