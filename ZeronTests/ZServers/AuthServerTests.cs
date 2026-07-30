// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Microsoft.EntityFrameworkCore;
using Zeron.Server.Data;
using Zeron.Server.ZCore;

namespace Zeron.Server.ZServers.Tests
{
    [TestClass()]
    public class AuthServerTests
    {
        /// <summary>
        /// LoginAsync succeeds for seeded admin user.
        /// </summary>
        [TestMethod()]
        public async Task LoginAsyncSucceedsForAdminTest()
        {
            string dbName = Guid.NewGuid().ToString();
            await using ZeronServerDbContext dbContext = CreateContext(dbName);
            ServerSettings settings = new()
            {
                DefaultAdminUsername = "admin",
                DefaultAdminPassword = "admin123"
            };

            AuthServer authServer = CreateAuthServer(dbContext, settings);
            await authServer.SeedDefaultUserAsync();

            var response = await authServer.LoginAsync("admin", "admin123");

            Assert.IsTrue(response.Success);
            Assert.IsNotNull(response.Token);
            Assert.AreEqual("admin", response.User?.Username);
            Assert.AreEqual(ServerRoles.Admin, response.User?.Role);
        }

        /// <summary>
        /// LoginAsync rejects invalid password.
        /// </summary>
        [TestMethod()]
        public async Task LoginAsyncRejectsInvalidPasswordTest()
        {
            string dbName = Guid.NewGuid().ToString();
            await using ZeronServerDbContext dbContext = CreateContext(dbName);
            ServerSettings settings = new()
            {
                DefaultAdminUsername = "admin",
                DefaultAdminPassword = "admin123"
            };

            AuthServer authServer = CreateAuthServer(dbContext, settings);
            await authServer.SeedDefaultUserAsync();

            var response = await authServer.LoginAsync("admin", "wrong-password");

            Assert.IsFalse(response.Success);
        }

        /// <summary>
        /// ValidateAgentApiKey matches configured key.
        /// </summary>
        [TestMethod()]
        public void ValidateAgentApiKeyTest()
        {
            string dbName = Guid.NewGuid().ToString();
            using ZeronServerDbContext dbContext = CreateContext(dbName);
            ServerSettings settings = new()
            {
                AgentApiKey = "zeron.testkey"
            };

            AuthServer authServer = CreateAuthServer(dbContext, settings);

            Assert.IsTrue(authServer.ValidateAgentApiKey("zeron.testkey"));
            Assert.IsFalse(authServer.ValidateAgentApiKey("invalid"));
        }

        private static AuthServer CreateAuthServer(ZeronServerDbContext dbContext, ServerSettings settings)
        {
            JwtTokenServer jwtTokenServer = new(settings);

            return new AuthServer(dbContext, settings, jwtTokenServer);
        }

        private static ZeronServerDbContext CreateContext(string dbName)
        {
            DbContextOptions<ZeronServerDbContext> options = new DbContextOptionsBuilder<ZeronServerDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;

            return new ZeronServerDbContext(options);
        }
    }
}
