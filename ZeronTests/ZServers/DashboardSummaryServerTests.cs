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
        }

        private static DashboardSummaryServer CreateSummaryServer(ZeronServerDbContext dbContext)
        {
            ServerSettings settings = new() { HeartbeatTimeoutSeconds = 90 };
            AgentManagerServer agentManager = new(dbContext);
            AgentDiagnosticServer diagnosticServer = new(dbContext, settings);
            TaskDispatcherServer taskDispatcher = new(dbContext, new CommandPublisherServer(settings));
            EventIngestorServer eventIngestor = new(dbContext);
            AlertNotifierServer alertNotifier = new(settings);
            AlertRuleServer alertRuleServer = new(dbContext, alertNotifier);

            return new DashboardSummaryServer(
                agentManager,
                diagnosticServer,
                taskDispatcher,
                eventIngestor,
                alertRuleServer);
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
