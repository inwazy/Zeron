// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Zeron.ZCore;
using Zeron.ZCore.Type;

namespace Zeron.ZCore.Utils.Tests
{
    [TestClass()]
    public class ZeronEventBusTests
    {
        [TestCleanup]
        public void Cleanup()
        {
            ZeronEventBus.Current.Clear();
        }

        [TestMethod()]
        public void PublishDeliversToExactAndWildcardSubscribersTest()
        {
            List<string> topics = [];

            using IDisposable all = ZeronEventBus.Current.Subscribe("*", evt => topics.Add("all:" + evt.Topic));
            using IDisposable install = ZeronEventBus.Current.Subscribe("install.*", evt => topics.Add("install:" + evt.Topic));
            using IDisposable exact = ZeronEventBus.Current.Subscribe(
                ZeronEventTopics.PackageCatalogSync,
                evt => topics.Add("exact:" + evt.Topic));

            ZeronEventBus.Current.Publish("install.started", "{}");
            ZeronEventBus.Current.Publish(ZeronEventTopics.PackageCatalogSync, "{\"applied\":1}");
            ZeronEventBus.Current.Publish("task.started", "{}");

            CollectionAssert.Contains(topics, "all:install.started");
            CollectionAssert.Contains(topics, "install:install.started");
            CollectionAssert.Contains(topics, "exact:package.catalog.sync");
            CollectionAssert.Contains(topics, "all:package.catalog.sync");
            CollectionAssert.Contains(topics, "all:task.started");
            CollectionAssert.DoesNotContain(topics, "install:task.started");
        }

        [TestMethod()]
        public void MatchesPrefixFilterTest()
        {
            Assert.IsTrue(ZeronEventBus.Matches("install.*", "install.started"));
            Assert.IsTrue(ZeronEventBus.Matches("*", "anything"));
            Assert.IsFalse(ZeronEventBus.Matches("install.*", "task.started"));
        }

        [TestMethod()]
        public void InstallEventPublisherDualWritesToBusTest()
        {
            ZeronEventType? captured = null;

            using IDisposable sub = ZeronEventBus.Current.Subscribe(
                ZeronEventTopics.PackageCatalogSync,
                evt => captured = evt);

            InstallEventPublisher.PublishObject(ZeronEventTopics.PackageCatalogSync, new { applied = 2, success = true });

            Assert.IsNotNull(captured);
            Assert.AreEqual(ZeronEventTopics.PackageCatalogSync, captured!.Topic);
            Assert.AreEqual("agent", captured.Source);
            Assert.IsTrue(captured.PayloadJson?.Contains("applied", StringComparison.Ordinal) == true);
        }
    }
}
