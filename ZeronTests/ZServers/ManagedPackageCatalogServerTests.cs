// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zeron.Server.Data;
using Zeron.Server.Data.Entities;
using Zeron.Server.ZServers;
using Zeron.ZCore.Type;

namespace Zeron.Server.ZServers.Tests
{
    [TestClass()]
    public class ManagedPackageCatalogServerTests
    {
        /// <summary>
        /// CreatePackageAsync stores normalized package names.
        /// </summary>
        [TestMethod()]
        public async Task CreatePackageAsyncStoresNormalizedNameTest()
        {
            await using ZeronServerDbContext dbContext = CreateContext();
            ManagedPackageCatalogServer catalog = new(dbContext);

            (ManagedPackageInfoType? package, string? error) = await catalog.CreatePackageAsync(new ManagedPackageUpsertRequestType
            {
                Name = "CCleaner",
                Urlx64 = "https://example.com/ccleaner.exe",
                CmdInstallx64 = "/S",
                IsEnabled = true
            });

            Assert.IsNull(error);
            Assert.IsNotNull(package);
            Assert.AreEqual("ccleaner", package!.Name);
            Assert.IsTrue(package.IsEnabled);
        }

        /// <summary>
        /// CreatePackageAsync records version 1.
        /// </summary>
        [TestMethod()]
        public async Task CreatePackageAsyncRecordsVersionOneTest()
        {
            await using ZeronServerDbContext dbContext = CreateContext();
            ManagedPackageCatalogServer catalog = new(dbContext);

            (ManagedPackageInfoType? package, string? error) = await catalog.CreatePackageAsync(new ManagedPackageUpsertRequestType
            {
                Name = "pkg-ver",
                Urlx64 = "https://example.com/v1.exe",
                IsEnabled = true
            }, actor: new AuditActorType { Username = "alice" });

            Assert.IsNull(error);
            Guid packageId = Guid.Parse(package!.Id!);

            List<ManagedPackageVersionInfoType> versions = await catalog.GetPackageVersionsAsync(packageId);

            Assert.AreEqual(1, versions.Count);
            Assert.AreEqual(1, versions[0].VersionNumber);
            Assert.AreEqual("create", versions[0].ChangeKind);
            Assert.AreEqual("alice", versions[0].ActorUsername);
            Assert.AreEqual("https://example.com/v1.exe", versions[0].Urlx64);
        }

        /// <summary>
        /// Update then rollback restores the prior definition and appends a rollback version.
        /// </summary>
        [TestMethod()]
        public async Task UpdateAndRollbackRestoresPriorVersionTest()
        {
            await using ZeronServerDbContext dbContext = CreateContext();
            ManagedPackageCatalogServer catalog = new(dbContext);

            (ManagedPackageInfoType? created, string? createError) = await catalog.CreatePackageAsync(new ManagedPackageUpsertRequestType
            {
                Name = "rollback-pkg",
                Urlx64 = "https://example.com/old.exe",
                CmdInstallx64 = "/OLD",
                IsEnabled = true
            });

            Assert.IsNull(createError);
            Guid packageId = Guid.Parse(created!.Id!);

            (ManagedPackageInfoType? updated, string? updateError) = await catalog.UpdatePackageAsync(
                packageId,
                new ManagedPackageUpsertRequestType
                {
                    Urlx64 = "https://example.com/new.exe",
                    CmdInstallx64 = "/NEW",
                    IsEnabled = false
                });

            Assert.IsNull(updateError);
            Assert.AreEqual("https://example.com/new.exe", updated!.Urlx64);
            Assert.IsFalse(updated.IsEnabled);

            (ManagedPackageInfoType? restored, string? rollbackError) = await catalog.RollbackPackageAsync(
                packageId,
                versionNumber: 1,
                actor: new AuditActorType { Username = "bob" });

            Assert.IsNull(rollbackError);
            Assert.IsNotNull(restored);
            Assert.AreEqual("https://example.com/old.exe", restored!.Urlx64);
            Assert.AreEqual("/OLD", restored.CmdInstallx64);
            Assert.IsTrue(restored.IsEnabled);

            List<ManagedPackageVersionInfoType> versions = await catalog.GetPackageVersionsAsync(packageId);

            Assert.AreEqual(3, versions.Count);
            Assert.AreEqual(3, versions[0].VersionNumber);
            Assert.AreEqual("rollback", versions[0].ChangeKind);
            Assert.AreEqual(1, versions[0].RestoredFromVersion);
            Assert.AreEqual("bob", versions[0].ActorUsername);
            Assert.AreEqual("https://example.com/old.exe", versions[0].Urlx64);
        }

        /// <summary>
        /// DeletePackageAsync removes version history (cascade).
        /// </summary>
        [TestMethod()]
        public async Task DeletePackageAsyncRemovesVersionsTest()
        {
            await using ZeronServerDbContext dbContext = CreateContext();
            ManagedPackageCatalogServer catalog = new(dbContext);

            (ManagedPackageInfoType? package, string? _) = await catalog.CreatePackageAsync(new ManagedPackageUpsertRequestType
            {
                Name = "to-delete",
                IsEnabled = true
            });

            Guid packageId = Guid.Parse(package!.Id!);
            Assert.AreEqual(1, await dbContext.ManagedPackageVersions.CountAsync(item => item.PackageId == packageId));

            string? error = await catalog.DeletePackageAsync(packageId);

            Assert.IsNull(error);
            Assert.AreEqual(0, await dbContext.ManagedPackageVersions.CountAsync(item => item.PackageId == packageId));
        }

        /// <summary>
        /// GetCatalogSyncAsync returns all packages for agents.
        /// </summary>
        [TestMethod()]
        public async Task GetCatalogSyncAsyncReturnsPackagesTest()
        {
            await using ZeronServerDbContext dbContext = CreateContext();
            ManagedPackageCatalogServer catalog = new(dbContext);

            await catalog.CreatePackageAsync(new ManagedPackageUpsertRequestType
            {
                Name = "pkg-a",
                IsEnabled = true
            });
            await catalog.CreatePackageAsync(new ManagedPackageUpsertRequestType
            {
                Name = "pkg-b",
                IsEnabled = false
            });

            ManagedPackageCatalogSyncResponseType sync = await catalog.GetCatalogSyncAsync();

            Assert.IsTrue(sync.Success);
            Assert.AreEqual(2, sync.Packages.Count);
        }

        /// <summary>
        /// CreateContext
        /// </summary>
        /// <returns>Returns db context.</returns>
        private static ZeronServerDbContext CreateContext()
        {
            DbContextOptions<ZeronServerDbContext> options = new DbContextOptionsBuilder<ZeronServerDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new ZeronServerDbContext(options);
        }
    }
}
