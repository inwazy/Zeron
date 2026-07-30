// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Microsoft.EntityFrameworkCore;
using Zeron.Server.Data;
using Zeron.Server.Data.Entities;
using Zeron.Server.ZCore;
using Zeron.Server.ZCore.Type;

namespace Zeron.Server.ZServers.Tests
{
    [TestClass()]
    public class AlertRuleServerTests
    {
        /// <summary>
        /// ProcessAgentOfflineAsync creates alert for stale agent.
        /// </summary>
        [TestMethod()]
        public async Task ProcessAgentOfflineAsyncCreatesAlertTest()
        {
            string dbName = Guid.NewGuid().ToString();
            await using ZeronServerDbContext dbContext = CreateContext(dbName);
            AlertRuleServer alertRuleServer = CreateAlertRuleServer(dbContext);

            AgentEntity agent = new()
            {
                Id = Guid.NewGuid(),
                AgentKey = "agent-offline-001",
                MachineName = "HOST-1",
                Status = "offline",
                LastHeartbeatAt = DateTime.UtcNow.AddMinutes(-5),
                RegisteredAt = DateTime.UtcNow.AddHours(-1)
            };

            int created = await alertRuleServer.ProcessAgentOfflineAsync([agent]);

            Assert.AreEqual(1, created);

            List<AlertEntity> alerts = await alertRuleServer.GetAlertsAsync(AlertStatusesType.Open, 10);

            Assert.AreEqual(1, alerts.Count);
            Assert.AreEqual(AlertRuleType.AgentOffline, alerts[0].RuleType);
            Assert.AreEqual("agent-offline-001", alerts[0].AgentKey);
        }

        /// <summary>
        /// ProcessAgentOfflineAsync deduplicates open alerts.
        /// </summary>
        [TestMethod()]
        public async Task ProcessAgentOfflineAsyncDeduplicatesOpenAlertsTest()
        {
            string dbName = Guid.NewGuid().ToString();
            await using ZeronServerDbContext dbContext = CreateContext(dbName);
            AlertRuleServer alertRuleServer = CreateAlertRuleServer(dbContext);

            AgentEntity agent = new()
            {
                Id = Guid.NewGuid(),
                AgentKey = "agent-offline-002",
                MachineName = "HOST-2",
                Status = "offline",
                LastHeartbeatAt = DateTime.UtcNow.AddMinutes(-5),
                RegisteredAt = DateTime.UtcNow.AddHours(-1)
            };

            int first = await alertRuleServer.ProcessAgentOfflineAsync([agent]);
            int second = await alertRuleServer.ProcessAgentOfflineAsync([agent]);

            Assert.AreEqual(1, first);
            Assert.AreEqual(0, second);
        }

        /// <summary>
        /// ResolveAgentOfflineAlertsAsync resolves open alerts when agent returns online.
        /// </summary>
        [TestMethod()]
        public async Task ResolveAgentOfflineAlertsAsyncResolvesAlertsTest()
        {
            string dbName = Guid.NewGuid().ToString();
            await using ZeronServerDbContext dbContext = CreateContext(dbName);
            AlertRuleServer alertRuleServer = CreateAlertRuleServer(dbContext);

            AgentEntity agent = new()
            {
                Id = Guid.NewGuid(),
                AgentKey = "agent-back-001",
                MachineName = "HOST-3",
                Status = "offline",
                LastHeartbeatAt = DateTime.UtcNow.AddMinutes(-5),
                RegisteredAt = DateTime.UtcNow.AddHours(-1)
            };

            await alertRuleServer.ProcessAgentOfflineAsync([agent]);
            int resolved = await alertRuleServer.ResolveAgentOfflineAlertsAsync("agent-back-001");

            Assert.AreEqual(1, resolved);

            int openCount = await alertRuleServer.GetOpenAlertCountAsync();

            Assert.AreEqual(0, openCount);
        }

        private static AlertRuleServer CreateAlertRuleServer(ZeronServerDbContext dbContext)
        {
            ServerSettings settings = new()
            {
                AlertEmailEnabled = false
            };

            AlertNotifierServer notifier = new(settings);

            return new AlertRuleServer(dbContext, notifier);
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
