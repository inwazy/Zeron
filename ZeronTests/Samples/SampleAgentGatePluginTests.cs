// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using SampleAgentGatePlugin;
using SampleGatePlugin = SampleAgentGatePlugin.SampleAgentGatePlugin;
using Zeron.ZCore;
using Zeron.ZCore.Type;
using Zeron.ZCore.Utils;

namespace Zeron.Samples.Tests
{
    [TestClass()]
    public class SampleAgentGatePluginTests
    {
        [TestCleanup]
        public void Cleanup()
        {
            ZeronGateServer.Current.Clear();
            ZeronEventBus.Current.Clear();
        }

        [TestMethod()]
        public void NormalizeModeAliasesTest()
        {
            Assert.AreEqual(SampleAgentGateOptions.ModeProceed, SampleAgentGateOptions.NormalizeMode(null));
            Assert.AreEqual(SampleAgentGateOptions.ModePauseResume, SampleAgentGateOptions.NormalizeMode("pause"));
            Assert.AreEqual(SampleAgentGateOptions.ModeCancel, SampleAgentGateOptions.NormalizeMode("CANCEL"));
        }

        [TestMethod()]
        public void MatchesPackageFilterTest()
        {
            SampleAgentGateOptions all = new();
            SampleAgentGateOptions filtered = new() { PackageFilter = "ccleaner" };

            Assert.IsTrue(all.MatchesPackage("{\"package\":\"anything\"}"));
            Assert.IsTrue(filtered.MatchesPackage("{\"package\":\"CCleaner\"}"));
            Assert.IsFalse(filtered.MatchesPackage("{\"package\":\"other\"}"));
        }

        [TestMethod()]
        public void HandleIgnoresGateCommandTest()
        {
            SampleGatePlugin plugin = new(new SampleAgentGateOptions
            {
                Mode = SampleAgentGateOptions.ModeCancel
            });
            GateContextType context = new()
            {
                Topic = ZeronEventTopics.GateCommand,
                PayloadJson = "{}",
                CorrelationId = "c-cmd"
            };

            plugin.Handle(context);

            Assert.AreEqual(GateDecisionType.Proceed, context.Decision);
        }

        [TestMethod()]
        public void HandleProceedLeavesDecisionTest()
        {
            SampleGatePlugin plugin = new(new SampleAgentGateOptions
            {
                Mode = SampleAgentGateOptions.ModeProceed
            });
            GateContextType context = new()
            {
                Topic = ZeronEventTopics.GateInstall,
                PayloadJson = "{\"package\":\"pkg\"}",
                CorrelationId = "c-proceed"
            };

            plugin.Handle(context);

            Assert.AreEqual(GateDecisionType.Proceed, context.Decision);
        }

        [TestMethod()]
        public void HandleCancelSetsCancelTest()
        {
            SampleGatePlugin plugin = new(new SampleAgentGateOptions
            {
                Mode = SampleAgentGateOptions.ModeCancel
            });
            plugin.Initialize(ZeronEventBus.Current, ZeronGateServer.Current);

            try
            {
                GateDecisionType decision = ZeronGateServer.Evaluate(
                    ZeronEventTopics.GateInstall,
                    "{\"package\":\"pkg\"}",
                    "c-cancel");

                Assert.AreEqual(GateDecisionType.Cancel, decision);
            }
            finally
            {
                plugin.Stop();
            }
        }

        [TestMethod()]
        public void HandlePauseResumeAutoResumesTest()
        {
            SampleGatePlugin plugin = new(new SampleAgentGateOptions
            {
                Mode = SampleAgentGateOptions.ModePauseResume,
                DelayMs = 50
            });
            plugin.Initialize(ZeronEventBus.Current, ZeronGateServer.Current);

            try
            {
                GateDecisionType decision = ZeronGateServer.Evaluate(
                    ZeronEventTopics.GateInstall,
                    "{\"package\":\"pkg\"}",
                    "c-pause",
                    timeoutMs: 5000);

                Assert.AreEqual(GateDecisionType.Proceed, decision);
            }
            finally
            {
                plugin.Stop();
            }
        }

        [TestMethod()]
        public void HandleIgnoresNonMatchingPackageTest()
        {
            SampleGatePlugin plugin = new(new SampleAgentGateOptions
            {
                Mode = SampleAgentGateOptions.ModeCancel,
                PackageFilter = "ccleaner"
            });
            plugin.Initialize(ZeronEventBus.Current, ZeronGateServer.Current);

            try
            {
                GateDecisionType decision = ZeronGateServer.Evaluate(
                    ZeronEventTopics.GateInstall,
                    "{\"package\":\"other\"}",
                    "c-skip");

                Assert.AreEqual(GateDecisionType.Proceed, decision);
            }
            finally
            {
                plugin.Stop();
            }
        }

        [TestMethod()]
        public void InitializeObservesInstallEventsTest()
        {
            SampleGatePlugin plugin = new(new SampleAgentGateOptions
            {
                Mode = SampleAgentGateOptions.ModeProceed
            });
            plugin.Initialize(ZeronEventBus.Current, ZeronGateServer.Current);

            try
            {
                ZeronEventBus.Current.Publish("install.started", "{\"package\":\"pkg\"}", source: "test", correlationId: "obs-1");
            }
            finally
            {
                plugin.Stop();
            }
        }
    }
}
