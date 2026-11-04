// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Security.Claims;
using Zeron.Server.Data;
using Zeron.Server.ZCore;
using Zeron.Server.ZInterfaces;
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
        /// Scoped query hides agent rows outside the allow-list and keeps own non-agent rows.
        /// </summary>
        [TestMethod()]
        public async Task QueryAppliesAgentScopeTest()
        {
            await using ZeronServerDbContext dbContext = CreateContext();
            HttpContextAccessor accessor = CreateAccessor("ops");
            FixedAgentScopeFilter scope = new(new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "agent-allowed"
            });
            AuditLogServer audit = new(dbContext, scope, accessor);

            await audit.WriteAsync(
                "agent.patch",
                true,
                "allowed agent",
                new AuditActorType { Username = "admin", Role = ServerRoles.Admin },
                targetType: "agent",
                targetKey: "agent-allowed");
            await audit.WriteAsync(
                "agent.patch",
                true,
                "denied agent",
                new AuditActorType { Username = "admin", Role = ServerRoles.Admin },
                targetType: "agent",
                targetKey: "agent-other");
            await audit.WriteAsync(
                AuditActions.PackageDeploy,
                true,
                "scoped deploy",
                new AuditActorType { Username = "admin", Role = ServerRoles.Admin },
                targetType: "package",
                targetKey: "pkg@agent-allowed");
            await audit.WriteAsync(
                AuditActions.CatalogCreate,
                true,
                "own catalog",
                new AuditActorType { Username = "ops", Role = ServerRoles.Operator },
                targetType: "package",
                targetKey: "mypkg");
            await audit.WriteAsync(
                AuditActions.CatalogCreate,
                true,
                "other catalog",
                new AuditActorType { Username = "other", Role = ServerRoles.Operator },
                targetType: "package",
                targetKey: "otherpkg");

            List<AuditLogInfoType> rows = await audit.QueryAsync(limit: 50);
            List<string> summaries = rows
                .Select(row => row.Summary ?? "")
                .ToList();

            Assert.AreEqual(3, rows.Count);
            CollectionAssert.Contains(summaries, "allowed agent");
            CollectionAssert.Contains(summaries, "scoped deploy");
            CollectionAssert.Contains(summaries, "own catalog");
            CollectionAssert.DoesNotContain(summaries, "denied agent");
            CollectionAssert.DoesNotContain(summaries, "other catalog");
        }

        /// <summary>
        /// Empty allow-list returns only the caller's non-agent audit rows.
        /// </summary>
        [TestMethod()]
        public async Task QueryEmptyScopeKeepsOwnNonAgentRowsTest()
        {
            await using ZeronServerDbContext dbContext = CreateContext();
            HttpContextAccessor accessor = CreateAccessor("ops");
            AuditLogServer audit = new(dbContext, new FixedAgentScopeFilter(new HashSet<string>()), accessor);

            await audit.WriteAsync(
                "agent.patch",
                true,
                "any agent",
                new AuditActorType { Username = "admin", Role = ServerRoles.Admin },
                targetType: "agent",
                targetKey: "agent-a");
            await audit.WriteAsync(
                "opspro.sso.login",
                true,
                "own login",
                new AuditActorType { Username = "ops", Role = ServerRoles.Operator },
                targetType: "user",
                targetKey: "ops");

            List<AuditLogInfoType> rows = await audit.QueryAsync(limit: 50);

            Assert.AreEqual(1, rows.Count);
            Assert.AreEqual("own login", rows[0].Summary);
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

        /// <summary>
        /// CreateAccessor
        /// </summary>
        /// <param name="username"></param>
        /// <returns>Returns http context accessor.</returns>
        private static HttpContextAccessor CreateAccessor(
            string username)
        {
            DefaultHttpContext httpContext = new()
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [
                        new Claim(ClaimTypes.Name, username),
                        new Claim(ClaimTypes.Role, ServerRoles.Operator)
                    ],
                    authenticationType: "test"))
            };

            return new HttpContextAccessor
            {
                HttpContext = httpContext
            };
        }

        /// <summary>
        /// Fixed allow-list scope filter for unit tests.
        /// </summary>
        private sealed class FixedAgentScopeFilter : IAgentAccessScopeFilter
        {
            private readonly IReadOnlySet<string>? m_Allowed;

            public FixedAgentScopeFilter(
                IReadOnlySet<string>? allowed)
            {
                m_Allowed = allowed;
            }

            public Task<IReadOnlySet<string>?> GetAllowedAgentKeysAsync(
                ClaimsPrincipal? user,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(m_Allowed);
            }
        }
    }
}
