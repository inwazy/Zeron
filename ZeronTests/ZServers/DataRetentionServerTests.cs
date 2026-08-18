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
    public class DataRetentionServerTests
    {
        /// <summary>
        /// PruneAsync skips when RetentionEnabled is false.
        /// </summary>
        [TestMethod()]
        public async Task PruneAsyncSkipsWhenDisabledTest()
        {
            await using ZeronServerDbContext dbContext = CreateContext();
            dbContext.AuditLogs.Add(CreateAudit(DateTime.UtcNow.AddDays(-400)));
            await dbContext.SaveChangesAsync();

            DataRetentionServer server = new(dbContext, new ServerSettings
            {
                RetentionEnabled = false,
                AuditLogRetentionDays = 90
            });

            DataRetentionResultType result = await server.PruneAsync();

            Assert.IsTrue(result.Skipped);
            Assert.AreEqual(1, await dbContext.AuditLogs.CountAsync());
        }

        /// <summary>
        /// PruneAsync deletes old audit logs and notifications, keeps recent rows.
        /// </summary>
        [TestMethod()]
        public async Task PruneAsyncDeletesExpiredAuditAndNotificationsTest()
        {
            await using ZeronServerDbContext dbContext = CreateContext();
            DateTime now = DateTime.UtcNow;
            Guid userId = Guid.NewGuid();

            dbContext.Users.Add(new UserEntity
            {
                Id = userId,
                Username = "retain-user",
                PasswordHash = "x",
                Role = ServerRoles.DeviceOwner,
                IsActive = true,
                CreatedAt = now
            });
            dbContext.AuditLogs.AddRange(
                CreateAudit(now.AddDays(-10), "keep-audit"),
                CreateAudit(now.AddDays(-40), "drop-audit"));
            dbContext.UserNotifications.AddRange(
                new UserNotificationEntity
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Kind = UserNotificationServer.KindInstallResult,
                    Title = "keep",
                    Message = "keep",
                    CreatedAt = now.AddDays(-2)
                },
                new UserNotificationEntity
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Kind = UserNotificationServer.KindInstallResult,
                    Title = "drop",
                    Message = "drop",
                    CreatedAt = now.AddDays(-40)
                });
            await dbContext.SaveChangesAsync();

            DataRetentionServer server = new(dbContext, new ServerSettings
            {
                RetentionEnabled = true,
                AuditLogRetentionDays = 30,
                UserNotificationRetentionDays = 14,
                CatalogVersionKeepCount = 20
            });

            DataRetentionResultType result = await server.PruneAsync();

            Assert.IsFalse(result.Skipped);
            Assert.AreEqual(1, result.AuditLogsDeleted);
            Assert.AreEqual(1, result.NotificationsDeleted);
            Assert.AreEqual(1, await dbContext.AuditLogs.CountAsync());
            Assert.AreEqual("keep-audit", (await dbContext.AuditLogs.SingleAsync()).Summary);
            Assert.AreEqual(1, await dbContext.UserNotifications.CountAsync());
            Assert.AreEqual("keep", (await dbContext.UserNotifications.SingleAsync()).Title);
        }

        /// <summary>
        /// PruneAsync keeps the newest catalog versions per package.
        /// </summary>
        [TestMethod()]
        public async Task PruneAsyncKeepsNewestCatalogVersionsTest()
        {
            await using ZeronServerDbContext dbContext = CreateContext();
            Guid packageId = Guid.NewGuid();
            DateTime now = DateTime.UtcNow;

            dbContext.ManagedPackages.Add(new ManagedPackageEntity
            {
                Id = packageId,
                Name = "retain-pkg",
                IsEnabled = true,
                CreatedAt = now,
                UpdatedAt = now
            });

            for (int version = 1; version <= 5; version++)
            {
                dbContext.ManagedPackageVersions.Add(new ManagedPackageVersionEntity
                {
                    Id = Guid.NewGuid(),
                    PackageId = packageId,
                    VersionNumber = version,
                    ChangeKind = version == 1 ? "create" : "update",
                    Name = "retain-pkg",
                    CreatedAt = now.AddMinutes(version),
                    IsEnabled = true
                });
            }

            await dbContext.SaveChangesAsync();

            DataRetentionServer server = new(dbContext, new ServerSettings
            {
                RetentionEnabled = true,
                AuditLogRetentionDays = 0,
                UserNotificationRetentionDays = 0,
                CatalogVersionKeepCount = 2
            });

            DataRetentionResultType result = await server.PruneAsync();

            Assert.AreEqual(3, result.CatalogVersionsDeleted);

            List<int> remaining = await dbContext.ManagedPackageVersions
                .Where(item => item.PackageId == packageId)
                .Select(item => item.VersionNumber)
                .OrderBy(item => item)
                .ToListAsync();

            CollectionAssert.AreEqual(new[] { 4, 5 }, remaining);
        }

        /// <summary>
        /// CreateAudit
        /// </summary>
        private static AuditLogEntity CreateAudit(
            DateTime occurredAt,
            string summary = "old")
        {
            return new AuditLogEntity
            {
                Id = Guid.NewGuid(),
                OccurredAt = occurredAt,
                Action = AuditActions.CatalogCreate,
                Success = true,
                Summary = summary,
                Source = "server"
            };
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
