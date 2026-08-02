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

            LoginResponseType response = await authServer.LoginAsync("admin", "admin123");

            Assert.IsTrue(response.Success);
            Assert.IsNotNull(response.Token);
            Assert.AreEqual("admin", response.User?.Username);
            Assert.AreEqual(ServerRoles.Admin, response.User?.Role);
            Assert.IsTrue(response.User!.MustChangePassword);
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

            LoginResponseType response = await authServer.LoginAsync("admin", "wrong-password");

            Assert.IsFalse(response.Success);
        }

        /// <summary>
        /// ChangePasswordAsync clears MustChangePassword flag.
        /// </summary>
        [TestMethod()]
        public async Task ChangePasswordAsyncClearsMustChangePasswordTest()
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

            LoginResponseType login = await authServer.LoginAsync("admin", "admin123");
            Guid userId = Guid.Parse(login.User!.Id!);

            (UserInfoType? updated, string? error) = await authServer.ChangePasswordAsync(
                userId,
                "admin123",
                "new-secure-password");

            Assert.IsNull(error);
            Assert.IsNotNull(updated);
            Assert.IsFalse(updated!.MustChangePassword);

            LoginResponseType afterChange = await authServer.LoginAsync("admin", "new-secure-password");

            Assert.IsTrue(afterChange.Success);
            Assert.IsFalse(afterChange.User!.MustChangePassword);
        }

        /// <summary>
        /// ChangePasswordAsync rejects incorrect current password.
        /// </summary>
        [TestMethod()]
        public async Task ChangePasswordAsyncRejectsWrongCurrentPasswordTest()
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

            LoginResponseType login = await authServer.LoginAsync("admin", "admin123");
            Guid userId = Guid.Parse(login.User!.Id!);

            (UserInfoType? updated, string? error) = await authServer.ChangePasswordAsync(
                userId,
                "wrong-password",
                "new-secure-password");

            Assert.IsNull(updated);
            Assert.AreEqual("Current password is incorrect.", error);
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
