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
    public class PackageDeployServerTests
    {
        /// <summary>
        /// DeployAsync creates ManagedPackage task.
        /// </summary>
        [TestMethod()]
        public async Task DeployAsyncCreatesManagedPackageTaskTest()
        {
            await using ZeronServerDbContext dbContext = CreateContext();

            dbContext.Agents.Add(new AgentEntity
            {
                Id = Guid.NewGuid(),
                AgentKey = "pkg-agent",
                Status = "online",
                MachineName = "HOST",
                RegisteredAt = DateTime.UtcNow,
                LastHeartbeatAt = DateTime.UtcNow
            });
            await dbContext.SaveChangesAsync();

            PackageDeployServer deployServer = CreateDeployServer(dbContext);

            PackageDeployResponseType response = await deployServer.DeployAsync(new PackageDeployRequestType
            {
                Operation = "install",
                PackageName = "ccleaner",
                ExtraArgs = "/S",
                TargetType = "all"
            });

            Assert.IsTrue(response.Success);
            Assert.IsNotNull(response.TaskId);
            Assert.AreEqual("install ccleaner /S", response.Command);

            TaskEntity? task = await dbContext.Tasks.FirstOrDefaultAsync(item => item.Id == response.TaskId);

            Assert.IsNotNull(task);
            Assert.AreEqual("ManagedPackage", task!.TargetApi);
            Assert.AreEqual("install ccleaner /S", task.Command);
            Assert.AreEqual(1, task.Assignments.Count);
        }

        /// <summary>
        /// DeployAsync rejects invalid operation.
        /// </summary>
        [TestMethod()]
        public async Task DeployAsyncRejectsInvalidOperationTest()
        {
            await using ZeronServerDbContext dbContext = CreateContext();
            PackageDeployServer deployServer = CreateDeployServer(dbContext);

            PackageDeployResponseType response = await deployServer.DeployAsync(new PackageDeployRequestType
            {
                Operation = "upgrade",
                PackageName = "ccleaner"
            });

            Assert.IsFalse(response.Success);
            Assert.IsNull(response.TaskId);
        }

        /// <summary>
        /// DeployAsync rejects packages missing from the Server catalog when catalog is wired.
        /// </summary>
        [TestMethod()]
        public async Task DeployAsyncRejectsMissingCatalogPackageTest()
        {
            await using ZeronServerDbContext dbContext = CreateContext();
            ManagedPackageCatalogServer catalog = new(dbContext);
            PackageDeployServer deployServer = CreateDeployServer(dbContext, catalog);

            PackageDeployResponseType response = await deployServer.DeployAsync(new PackageDeployRequestType
            {
                Operation = "install",
                PackageName = "missing-pkg"
            });

            Assert.IsFalse(response.Success);
            Assert.IsTrue(response.Message!.Contains("not found", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// BuildCommand formats install/uninstall commands.
        /// </summary>
        [TestMethod()]
        public void BuildCommandFormatsCommandTest()
        {
            Assert.AreEqual("install ccleaner", PackageDeployServer.BuildCommand("install", "ccleaner", null));
            Assert.AreEqual("uninstall 7zip /S", PackageDeployServer.BuildCommand("uninstall", "7zip", "/S"));
        }

        private static PackageDeployServer CreateDeployServer(
            ZeronServerDbContext dbContext,
            ManagedPackageCatalogServer? catalogServer = null)
        {
            TaskDispatcherServer taskDispatcher = new(dbContext, new CommandPublisherServer(new ServerSettings()));

            return new PackageDeployServer(taskDispatcher, dbContext, catalogServer);
        }

        private static ZeronServerDbContext CreateContext()
        {
            DbContextOptions<ZeronServerDbContext> options = new DbContextOptionsBuilder<ZeronServerDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new ZeronServerDbContext(options);
        }
    }
}
