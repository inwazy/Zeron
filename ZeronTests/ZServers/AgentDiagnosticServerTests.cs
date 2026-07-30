// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Microsoft.EntityFrameworkCore;
using Zeron.Server.Data;
using Zeron.Server.ZCore;

namespace Zeron.Server.ZServers.Tests
{
    [TestClass()]
    public class AgentDiagnosticServerTests
    {
        /// <summary>
        /// Healthy agent returns healthy connection state.
        /// </summary>
        [TestMethod()]
        public async Task GetDiagnosticAsyncReturnsHealthyForRecentHeartbeatTest()
        {
            string dbName = Guid.NewGuid().ToString();
            await using ZeronServerDbContext dbContext = CreateContext(dbName);
            AgentManagerServer agentManager = new(dbContext);
            AgentDiagnosticServer diagnosticServer = new(dbContext, CreateSettings());

            await agentManager.ProcessHeartbeatAsync(
                new Zeron.ZCore.Type.AgentHeartbeatRequestType
                {
                    AgentId = "diag-agent-001",
                    MachineName = "DIAGHOST",
                    UptimeSeconds = 30,
                    Version = "1.0.0"
                },
                "127.0.0.1");

            var diagnostic = await diagnosticServer.GetDiagnosticAsync("diag-agent-001");

            Assert.IsNotNull(diagnostic);
            Assert.AreEqual("healthy", diagnostic!.ConnectionState);
        }

        /// <summary>
        /// Disabled agent returns disabled connection state.
        /// </summary>
        [TestMethod()]
        public async Task GetDiagnosticAsyncReturnsDisabledForDisabledAgentTest()
        {
            string dbName = Guid.NewGuid().ToString();
            await using ZeronServerDbContext dbContext = CreateContext(dbName);
            AgentManagerServer agentManager = new(dbContext);
            AgentDiagnosticServer diagnosticServer = new(dbContext, CreateSettings());

            await agentManager.ProcessHeartbeatAsync(
                new Zeron.ZCore.Type.AgentHeartbeatRequestType
                {
                    AgentId = "diag-agent-disabled",
                    MachineName = "DIAGHOST",
                    UptimeSeconds = 30,
                    Version = "1.0.0"
                },
                "127.0.0.1");

            await agentManager.UpdateAgentAsync("diag-agent-disabled", new Zeron.ZCore.Type.AgentUpdateRequestType
            {
                Status = "disabled"
            });

            var diagnostic = await diagnosticServer.GetDiagnosticAsync("diag-agent-disabled");

            Assert.IsNotNull(diagnostic);
            Assert.AreEqual("disabled", diagnostic!.ConnectionState);
        }

        private static ZeronServerDbContext CreateContext(string dbName)
        {
            DbContextOptions<ZeronServerDbContext> options = new DbContextOptionsBuilder<ZeronServerDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;

            return new ZeronServerDbContext(options);
        }

        private static ServerSettings CreateSettings()
        {
            return new ServerSettings
            {
                HeartbeatTimeoutSeconds = 90
            };
        }
    }
}
