// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Zeron.ZCore;
using Zeron.ZCore.Type;

namespace Zeron.ZCore.Utils.Tests
{
    [TestClass()]
    public class ScriptEventBridgeTests
    {
        [TestCleanup]
        public void Cleanup()
        {
            ZeronEventBus.Current.Clear();
        }

        [TestMethod()]
        public void ProcessControlLineRejectsGateAndSyncCommandsTest()
        {
            using ScriptEventBridge bridge = new();

            string? cancel = bridge.ProcessControlLine("""{"type":"cancel"}""");
            string? pauseGate = bridge.ProcessControlLine("""{"type":"pause_gate"}""");
            string? pauseSync = bridge.ProcessControlLine("""{"type":"pause_sync"}""");

            Assert.IsNotNull(cancel);
            Assert.IsTrue(cancel!.Contains("not_allowed", StringComparison.Ordinal));
            Assert.IsNotNull(pauseGate);
            Assert.IsTrue(pauseGate!.Contains("not_allowed", StringComparison.Ordinal));
            Assert.IsNotNull(pauseSync);
            Assert.IsTrue(pauseSync!.Contains("not_allowed", StringComparison.Ordinal));
        }

        [TestMethod()]
        public void PauseSelfQueuesEventsUntilResumeTest()
        {
            using ScriptEventBridge bridge = new();
            bridge.Configure(enabled: true, executablePath: "listener.exe", arguments: "");
            bridge.Start(launchListener: false);

            Assert.IsNull(bridge.ProcessControlLine("""{"type":"pause_self"}"""));
            Assert.IsTrue(bridge.IsPaused);

            ZeronEventBus.Current.Publish(ZeronEventTopics.PackageCatalogSync, "{\"applied\":1}", source: "agent");
            ZeronEventBus.Current.Publish("install.started", "{}", source: "agent");

            Assert.IsTrue(bridge.QueuedCount >= 2);

            Assert.IsNull(bridge.ProcessControlLine("""{"type":"resume_self"}"""));
            Assert.IsFalse(bridge.IsPaused);
            // No live process → flush is a no-op and events remain queued.
            Assert.IsTrue(bridge.QueuedCount >= 2);
        }

        [TestMethod()]
        public void AckDoesNotPauseTest()
        {
            using ScriptEventBridge bridge = new();

            Assert.IsNull(bridge.ProcessControlLine("""{"type":"ack","correlationId":"x"}"""));
            Assert.IsFalse(bridge.IsPaused);
        }
    }
}
