// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Microsoft.EntityFrameworkCore;
using Zeron.Server.Data;
using Zeron.Server.Data.Entities;
using Zeron.Server.ZCore;
using Zeron.ZCore;
using Zeron.ZCore.Type;
using Zeron.ZCore.Utils;

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

        /// <summary>
        /// ProcessHeartbeatAsync publishes agent.connected when coming online.
        /// </summary>
        [TestMethod()]
        public async Task ProcessHeartbeatAsyncPublishesAgentConnectedTest()
        {
            ZeronEventBus.Current.Clear();
            List<string> topics = [];

            using IDisposable sub = ZeronEventBus.Current.Subscribe(
                ZeronEventTopics.AgentConnected,
                evt => topics.Add(evt.Topic));

            string dbName = Guid.NewGuid().ToString();
            await using ZeronServerDbContext dbContext = CreateContext(dbName);
            AgentManagerServer agentManager = new(dbContext);

            await agentManager.ProcessHeartbeatAsync(
                new AgentHeartbeatRequestType
                {
                    AgentId = "agent-connected-001",
                    MachineName = "HOST",
                    UptimeSeconds = 1,
                    Version = "1.0.0"
                },
                "127.0.0.1");

            Assert.AreEqual(1, topics.Count);
            Assert.AreEqual(ZeronEventTopics.AgentConnected, topics[0]);

            await agentManager.ProcessHeartbeatAsync(
                new AgentHeartbeatRequestType
                {
                    AgentId = "agent-connected-001",
                    MachineName = "HOST",
                    UptimeSeconds = 2,
                    Version = "1.0.0"
                },
                "127.0.0.1");

            Assert.AreEqual(1, topics.Count);
            ZeronEventBus.Current.Clear();
        }

        /// <summary>
        /// ProcessHeartbeatAsync persists supportedEngines JSON from the agent.
        /// </summary>
        [TestMethod()]
        public async Task ProcessHeartbeatAsyncStoresSupportedEnginesTest()
        {
            string dbName = Guid.NewGuid().ToString();
            await using ZeronServerDbContext dbContext = CreateContext(dbName);
            AgentManagerServer agentManager = new(dbContext);

            AgentHeartbeatResponseType response = await agentManager.ProcessHeartbeatAsync(
                new AgentHeartbeatRequestType
                {
                    AgentId = "agent-engines-001",
                    MachineName = "ENGINEHOST",
                    UptimeSeconds = 10,
                    Version = "1.0.0",
                    SupportedEngines =
                    [
                        new ScriptEngineInfoType
                        {
                            Id = "powershell",
                            DisplayName = "PowerShell",
                            Platforms = ["windows"],
                            Available = true
                        }
                    ]
                },
                "127.0.0.1");

            Assert.IsTrue(response.Success);

            AgentEntity? agent = await agentManager.GetAgentByKeyAsync("agent-engines-001");

            Assert.IsNotNull(agent);
            Assert.IsFalse(string.IsNullOrWhiteSpace(agent!.SupportedEnginesJson));
            Assert.IsTrue(agent.SupportedEnginesJson!.Contains("powershell", StringComparison.OrdinalIgnoreCase));
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
