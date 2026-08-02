// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Zeron.ZServers;

namespace Zeron.ZCore.Tests
{
    [TestClass()]
    public class InstallEventPublisherTests
    {
        [TestMethod()]
        public void EnrichMessageAddsAgentIdTest()
        {
            typeof(AgentServer)
                .GetProperty("AgentId")!
                .SetValue(null, "test-agent-001");

            string enriched = InstallEventPublisher.EnrichMessage("{\"topic\":\"install.completed\"}");

            Assert.IsTrue(enriched.Contains("test-agent-001", StringComparison.Ordinal));
            Assert.IsTrue(enriched.Contains("timestamp", StringComparison.Ordinal));
        }
    }
}
