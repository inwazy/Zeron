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
    public class CatalogSyncHealthServerTests
    {
        /// <summary>
        /// GetHealthAsync classifies healthy / stale / never / offline agents.
        /// </summary>
        [TestMethod()]
        public async Task GetHealthAsyncClassifiesAgentsTest()
        {
            await using ZeronServerDbContext dbContext = CreateContext();
            DateTime now = DateTime.UtcNow;

            dbContext.Agents.AddRange(
                new AgentEntity
                {
                    Id = Guid.NewGuid(),
                    AgentKey = "healthy-agent",
                    MachineName = "H1",
                    Status = "online",
                    RegisteredAt = now,
                    LastSeenAt = now,
                    LastHeartbeatAt = now,
                    LastCatalogSyncAt = now.AddMinutes(-5)
                },
                new AgentEntity
                {
                    Id = Guid.NewGuid(),
                    AgentKey = "stale-agent",
                    MachineName = "S1",
                    Status = "online",
                    RegisteredAt = now,
                    LastSeenAt = now,
                    LastHeartbeatAt = now,
                    LastCatalogSyncAt = now.AddMinutes(-60)
                },
                new AgentEntity
                {
                    Id = Guid.NewGuid(),
                    AgentKey = "never-agent",
                    MachineName = "N1",
                    Status = "online",
                    RegisteredAt = now,
                    LastSeenAt = now,
                    LastHeartbeatAt = now,
                    LastCatalogSyncAt = null
                },
                new AgentEntity
                {
                    Id = Guid.NewGuid(),
                    AgentKey = "offline-agent",
                    MachineName = "O1",
                    Status = "offline",
                    RegisteredAt = now,
                    LastSeenAt = now.AddHours(-2),
                    LastHeartbeatAt = now.AddHours(-2),
                    LastCatalogSyncAt = now.AddMinutes(-1)
                });
            await dbContext.SaveChangesAsync();

            CatalogSyncHealthServer server = CreateServer(dbContext, staleMinutes: 15);
            CatalogSyncHealthSummaryType summary = await server.GetHealthAsync();

            Assert.AreEqual(1, summary.Healthy);
            Assert.AreEqual(1, summary.Stale);
            Assert.AreEqual(1, summary.NeverSynced);
            Assert.AreEqual(1, summary.Offline);
            Assert.AreEqual(0, summary.RecentlyFailed);
            Assert.AreEqual(15, summary.StaleThresholdMinutes);
            Assert.AreEqual("healthy", summary.Agents.Single(item => item.AgentKey == "healthy-agent").SyncState);
            Assert.AreEqual("stale", summary.Agents.Single(item => item.AgentKey == "stale-agent").SyncState);
            Assert.AreEqual("never", summary.Agents.Single(item => item.AgentKey == "never-agent").SyncState);
            Assert.AreEqual("offline", summary.Agents.Single(item => item.AgentKey == "offline-agent").SyncState);
        }

        /// <summary>
        /// GetHealthAsync marks agents with recent failed catalog sync audit as failed.
        /// </summary>
        [TestMethod()]
        public async Task GetHealthAsyncMarksRecentFailedSyncTest()
        {
            await using ZeronServerDbContext dbContext = CreateContext();
            DateTime now = DateTime.UtcNow;

            dbContext.Agents.Add(new AgentEntity
            {
                Id = Guid.NewGuid(),
                AgentKey = "fail-agent",
                MachineName = "F1",
                Status = "online",
                RegisteredAt = now,
                LastSeenAt = now,
                LastHeartbeatAt = now,
                LastCatalogSyncAt = now.AddMinutes(-2)
            });
            dbContext.AuditLogs.Add(new AuditLogEntity
            {
                Id = Guid.NewGuid(),
                OccurredAt = now.AddMinutes(-1),
                Action = AuditActions.PackageCatalogSync,
                Success = false,
                Source = "agent",
                ActorUsername = "fail-agent",
                Summary = "sync failed"
            });
            await dbContext.SaveChangesAsync();

            CatalogSyncHealthServer server = CreateServer(dbContext, staleMinutes: 15);
            CatalogSyncHealthSummaryType summary = await server.GetHealthAsync();

            Assert.AreEqual(1, summary.RecentlyFailed);
            Assert.AreEqual("failed", summary.Agents.Single().SyncState);
        }

        /// <summary>
        /// PushSyncAsync with OnlyUnhealthy targets online never/stale agents when publisher is absent (empty push).
        /// </summary>
        [TestMethod()]
        public async Task PushSyncAsyncOnlyUnhealthyWithoutPublisherReturnsEmptyTest()
        {
            await using ZeronServerDbContext dbContext = CreateContext();
            DateTime now = DateTime.UtcNow;

            dbContext.Agents.Add(new AgentEntity
            {
                Id = Guid.NewGuid(),
                AgentKey = "never-agent",
                Status = "online",
                RegisteredAt = now,
                LastSeenAt = now,
                LastHeartbeatAt = now
            });
            await dbContext.SaveChangesAsync();

            CatalogSyncHealthServer server = CreateServer(dbContext, staleMinutes: 15);
            CatalogSyncPushResponseType response = await server.PushSyncAsync(
                new CatalogSyncPushRequestType { OnlyUnhealthy = true });

            Assert.IsTrue(response.Success);
            Assert.AreEqual(0, response.PushedCount);
            StringAssert.Contains(response.Message, "No online agents");
        }

        /// <summary>
        /// CreateServer
        /// </summary>
        private static CatalogSyncHealthServer CreateServer(
            ZeronServerDbContext dbContext,
            int staleMinutes)
        {
            ManagedPackageCatalogServer catalog = new(dbContext);
            ServerSettings settings = new() { CatalogSyncStaleMinutes = staleMinutes };

            return new CatalogSyncHealthServer(dbContext, catalog, settings);
        }

        /// <summary>
        /// CreateContext
        /// </summary>
        private static ZeronServerDbContext CreateContext()
        {
            DbContextOptions<ZeronServerDbContext> options = new DbContextOptionsBuilder<ZeronServerDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new ZeronServerDbContext(options);
        }
    }
}
