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
    public class UserManagerServerTests
    {
        /// <summary>
        /// CreateUserAsync creates Operator account.
        /// </summary>
        [TestMethod()]
        public async Task CreateUserAsyncCreatesOperatorTest()
        {
            await using ZeronServerDbContext dbContext = CreateContext();
            UserManagerServer userManager = new(dbContext);

            (UserInfoType? user, string? error) = await userManager.CreateUserAsync(new UserCreateRequestType
            {
                Username = "ops1",
                Password = "secret1",
                Role = ServerRoles.Operator
            });

            Assert.IsNull(error);
            Assert.IsNotNull(user);
            Assert.AreEqual("ops1", user!.Username);
            Assert.AreEqual(ServerRoles.Operator, user.Role);
            Assert.IsTrue(user.IsActive);
            Assert.IsTrue(user.MustChangePassword);
        }

        /// <summary>
        /// UpdateUserAsync rejects deactivating the last admin.
        /// </summary>
        [TestMethod()]
        public async Task UpdateUserAsyncRejectsLastAdminDeactivateTest()
        {
            await using ZeronServerDbContext dbContext = CreateContext();
            UserManagerServer userManager = new(dbContext);
            AuthServer authServer = new(dbContext, new ServerSettings
            {
                DefaultAdminUsername = "admin",
                DefaultAdminPassword = "admin123"
            }, new JwtTokenServer(new ServerSettings()));

            await authServer.SeedDefaultUserAsync();

            List<UserInfoType> users = await userManager.GetUsersAsync();
            Guid adminId = Guid.Parse(users[0].Id!);

            (UserInfoType? updated, string? error) = await userManager.UpdateUserAsync(
                adminId,
                new UserUpdateRequestType { IsActive = false });

            Assert.IsNull(updated);
            Assert.AreEqual("Cannot deactivate the last active Admin.", error);
        }

        /// <summary>
        /// UpdateUserAsync rejects self-deactivation.
        /// </summary>
        [TestMethod()]
        public async Task UpdateUserAsyncRejectsSelfDeactivateTest()
        {
            await using ZeronServerDbContext dbContext = CreateContext();
            UserManagerServer userManager = new(dbContext);

            (UserInfoType? admin, string? _) = await userManager.CreateUserAsync(new UserCreateRequestType
            {
                Username = "admin2",
                Password = "admin123",
                Role = ServerRoles.Admin
            });

            (UserInfoType? operatorUser, string? _) = await userManager.CreateUserAsync(new UserCreateRequestType
            {
                Username = "ops2",
                Password = "secret1",
                Role = ServerRoles.Operator
            });

            Guid operatorId = Guid.Parse(operatorUser!.Id!);

            (UserInfoType? updated, string? error) = await userManager.UpdateUserAsync(
                operatorId,
                new UserUpdateRequestType { IsActive = false },
                operatorId);

            Assert.IsNull(updated);
            Assert.AreEqual("Cannot deactivate your own account.", error);
            Assert.IsNotNull(admin);
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
