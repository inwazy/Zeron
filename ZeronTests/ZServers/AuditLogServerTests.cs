// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zeron.Server.Data;
using Zeron.Server.ZCore;
using Zeron.Server.ZServers;
using Zeron.ZCore.Type;

namespace Zeron.Server.ZServers.Tests
{
    [TestClass()]
    public class AuditLogServerTests
    {
        /// <summary>
        /// Catalog create writes an audit row when actor is provided.
        /// </summary>
        [TestMethod()]
        public async Task CatalogCreateWritesAuditTest()
        {
            await using ZeronServerDbContext dbContext = CreateContext();
            AuditLogServer audit = new(dbContext);
            ManagedPackageCatalogServer catalog = new(dbContext, auditLogServer: audit);

            AuditActorType actor = new()
            {
                UserId = Guid.NewGuid(),
                Username = "ops",
                Role = ServerRoles.Operator,
                Source = "server"
            };

            (ManagedPackageInfoType? package, string? error) = await catalog.CreatePackageAsync(
                new ManagedPackageUpsertRequestType
                {
                    Name = "auditpkg",
                    IsEnabled = true
                },
                actor: actor);

            Assert.IsNull(error);
            Assert.IsNotNull(package);

            List<AuditLogInfoType> rows = await audit.QueryAsync(action: AuditActions.CatalogCreate);
            Assert.AreEqual(1, rows.Count);
            Assert.AreEqual("ops", rows[0].ActorUsername);
            Assert.AreEqual("auditpkg", rows[0].TargetKey);
            Assert.IsTrue(rows[0].Success);
        }

        /// <summary>
        /// Deploy writes package.deploy audit for staff actor.
        /// </summary>
        [TestMethod()]
        public async Task DeployWritesAuditTest()
        {
            await using ZeronServerDbContext dbContext = CreateContext();
            AuditLogServer audit = new(dbContext);
            ManagedPackageCatalogServer catalog = new(dbContext, auditLogServer: audit);
            await catalog.CreatePackageAsync(new ManagedPackageUpsertRequestType
            {
                Name = "deploypkg",
                IsEnabled = true
            });

            PackageDeployServer deploy = new(
                new TaskDispatcherServer(dbContext, new CommandPublisherServer(new ServerSettings())),
                dbContext,
                catalog,
                audit);

            PackageDeployResponseType response = await deploy.DeployAsync(
                new PackageDeployRequestType
                {
                    Operation = "install",
                    PackageName = "deploypkg",
                    TargetType = "all"
                },
                actor: new AuditActorType
                {
                    Username = "admin",
                    Role = ServerRoles.Admin
                });

            Assert.IsTrue(response.Success);

            List<AuditLogInfoType> rows = await audit.QueryAsync(action: AuditActions.PackageDeploy);
            Assert.AreEqual(1, rows.Count);
            Assert.AreEqual("admin", rows[0].ActorUsername);
            Assert.IsTrue(rows[0].Success);
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
