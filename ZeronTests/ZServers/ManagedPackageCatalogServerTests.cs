// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zeron.Server.Data;
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
