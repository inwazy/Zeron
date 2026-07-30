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
    public class AgentManagerServerTests
    {
        /// <summary>
        /// ProcessHeartbeatAsync registers agent and returns pending tasks.
        /// </summary>
        [TestMethod()]
        public async Task ProcessHeartbeatAsyncRegistersAgentAndReturnsPendingTasksTest()
        {
            string dbName = Guid.NewGuid().ToString();
            await using ZeronServerDbContext dbContext = CreateContext(dbName);
            AgentManagerServer agentManager = new(dbContext);
            TaskDispatcherServer taskDispatcher = new(dbContext, new CommandPublisherServer(new ServerSettings()));

            AgentHeartbeatResponseType firstHeartbeat = await agentManager.ProcessHeartbeatAsync(
                new AgentHeartbeatRequestType
                {
                    AgentId = "agent-test-001",
                    MachineName = "TESTHOST",
                    UptimeSeconds = 120,
                    Version = "1.0.0"
                },
                "127.0.0.1");

            Assert.IsTrue(firstHeartbeat.Success);

            await taskDispatcher.CreateTaskAsync(new TaskCreateRequestType
            {
                Name = "heartbeat-test",
                TargetApi = "HealthCheck",
                Command = "",
                TargetType = "agent",
                AgentIds = ["agent-test-001"]
            });

            AgentHeartbeatResponseType response = await agentManager.ProcessHeartbeatAsync(
                new AgentHeartbeatRequestType
                {
                    AgentId = "agent-test-001",
                    MachineName = "TESTHOST",
                    UptimeSeconds = 130,
                    Version = "1.0.0",
                    InstallQueueCount = 0,
                    InstallRunning = false,
                    SchedulerTaskCount = 1
                },
                "127.0.0.1");

            Assert.IsTrue(response.Success);
            Assert.IsNotNull(response.PendingTasks);
            Assert.AreEqual(1, response.PendingTasks!.Count);
            Assert.AreEqual("HealthCheck", response.PendingTasks[0].TargetApi);

            AgentEntity? agent = await agentManager.GetAgentByKeyAsync("agent-test-001");

            Assert.IsNotNull(agent);
            Assert.AreEqual("online", agent!.Status);
            Assert.AreEqual("TESTHOST", agent.MachineName);
        }

        /// <summary>
        /// MarkOfflineAgentsAsync marks stale online agents offline.
        /// </summary>
        [TestMethod()]
        public async Task MarkOfflineAgentsAsyncMarksStaleAgentsTest()
        {
            string dbName = Guid.NewGuid().ToString();
            await using ZeronServerDbContext dbContext = CreateContext(dbName);
            AgentManagerServer agentManager = new(dbContext);

            dbContext.Agents.Add(new AgentEntity
            {
                Id = Guid.NewGuid(),
                AgentKey = "stale-agent",
                Status = "online",
                LastHeartbeatAt = DateTime.UtcNow.AddMinutes(-5),
                RegisteredAt = DateTime.UtcNow.AddHours(-1)
            });

            await dbContext.SaveChangesAsync();

            int affected = await agentManager.MarkOfflineAgentsAsync(60);

            Assert.AreEqual(1, affected);

            AgentEntity? agent = await agentManager.GetAgentByKeyAsync("stale-agent");

            Assert.IsNotNull(agent);
            Assert.AreEqual("offline", agent!.Status);
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
