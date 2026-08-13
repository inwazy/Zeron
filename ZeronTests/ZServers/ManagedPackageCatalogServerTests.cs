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
        /// ComparePackageVersionsAsync reports changed fields between versions and current.
        /// </summary>
        [TestMethod()]
        public async Task ComparePackageVersionsAsyncReportsChangesTest()
        {
            await using ZeronServerDbContext dbContext = CreateContext();
            ManagedPackageCatalogServer catalog = new(dbContext);

            (ManagedPackageInfoType? created, string? createError) = await catalog.CreatePackageAsync(new ManagedPackageUpsertRequestType
            {
                Name = "diff-pkg",
                Urlx64 = "https://example.com/old.exe",
                CmdInstallx64 = "/OLD",
                ScriptInstallBefore = "Write-Host old",
                IsEnabled = true
            });

            Assert.IsNull(createError);
            Guid packageId = Guid.Parse(created!.Id!);

            await catalog.UpdatePackageAsync(packageId, new ManagedPackageUpsertRequestType
            {
                Urlx64 = "https://example.com/new.exe",
                CmdInstallx64 = "/NEW",
                ScriptInstallBefore = "Write-Host new",
                IsEnabled = false
            });

            (ManagedPackageVersionDiffType? vsCurrent, string? currentError) = await catalog.ComparePackageVersionsAsync(
                packageId,
                leftVersionNumber: 1,
                rightVersionNumber: null);

            Assert.IsNull(currentError);
            Assert.IsNotNull(vsCurrent);
            Assert.AreEqual("v1", vsCurrent!.LeftLabel);
            Assert.AreEqual("current", vsCurrent.RightLabel);
            Assert.IsTrue(vsCurrent.ChangedCount >= 3);
            Assert.IsTrue(vsCurrent.Fields.Single(field => field.Field == "Urlx64").Changed);
            Assert.AreEqual("https://example.com/old.exe", vsCurrent.Fields.Single(field => field.Field == "Urlx64").Left);
            Assert.AreEqual("https://example.com/new.exe", vsCurrent.Fields.Single(field => field.Field == "Urlx64").Right);
            Assert.IsTrue(vsCurrent.Fields.Single(field => field.Field == "IsEnabled").Changed);
            Assert.IsTrue(vsCurrent.Fields.Single(field => field.Field == "ScriptInstallBefore").Changed);

            (ManagedPackageVersionDiffType? v1v2, string? pairError) = await catalog.ComparePackageVersionsAsync(
                packageId,
                leftVersionNumber: 1,
                rightVersionNumber: 2);

            Assert.IsNull(pairError);
            Assert.IsNotNull(v1v2);
            Assert.AreEqual("v2", v1v2!.RightLabel);
            Assert.IsTrue(v1v2.Fields.Single(field => field.Field == "CmdInstallx64").Changed);

            (ManagedPackageVersionDiffType? same, string? sameError) = await catalog.ComparePackageVersionsAsync(
                packageId,
                leftVersionNumber: 1,
                rightVersionNumber: 1);

            Assert.IsNull(same);
            Assert.AreEqual("Left and right versions must differ.", sameError);
        }

        /// <summary>
        /// ScriptEngine is snapshotted and restored on rollback.
        /// </summary>
        [TestMethod()]
        public async Task RollbackRestoresScriptEngineTest()
        {
            await using ZeronServerDbContext dbContext = CreateContext();
            ManagedPackageCatalogServer catalog = new(dbContext);

            (ManagedPackageInfoType? created, string? createError) = await catalog.CreatePackageAsync(new ManagedPackageUpsertRequestType
            {
                Name = "engine-pkg",
                Urlx64 = "https://example.com/a.exe",
                ScriptEngine = "powershell",
                IsEnabled = true
            });

            Assert.IsNull(createError);
            Assert.AreEqual("powershell", created!.ScriptEngine);
            Guid packageId = Guid.Parse(created.Id!);

            (ManagedPackageInfoType? updated, string? updateError) = await catalog.UpdatePackageAsync(
                packageId,
                new ManagedPackageUpsertRequestType
                {
                    ScriptEngine = "mytool"
                });

            Assert.IsNull(updateError);
            Assert.AreEqual("mytool", updated!.ScriptEngine);

            (ManagedPackageInfoType? restored, string? rollbackError) = await catalog.RollbackPackageAsync(
                packageId,
                versionNumber: 1);

            Assert.IsNull(rollbackError);
            Assert.AreEqual("powershell", restored!.ScriptEngine);

            List<ManagedPackageVersionInfoType> versions = await catalog.GetPackageVersionsAsync(packageId);

            Assert.AreEqual("powershell", versions[0].ScriptEngine);
            Assert.AreEqual("mytool", versions[1].ScriptEngine);
            Assert.AreEqual("powershell", versions[2].ScriptEngine);
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
