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
    public class UserAgentBindingServerTests
    {
        /// <summary>
        /// CreateBindingAsync links a user to an agent key.
        /// </summary>
        [TestMethod()]
        public async Task CreateBindingAsyncCreatesBindingTest()
        {
            await using ZeronServerDbContext dbContext = CreateContext();
            UserManagerServer userManager = new(dbContext);
            UserAgentBindingServer bindingServer = new(dbContext);

            (UserInfoType? user, string? _) = await userManager.CreateUserAsync(new UserCreateRequestType
            {
                Username = "owner1",
                Password = "secret1",
                Role = ServerRoles.DeviceOwner
            });

            dbContext.Agents.Add(new AgentEntity
            {
                Id = Guid.NewGuid(),
                AgentKey = "agent-1",
                MachineName = "PC-1",
                Status = "online",
                RegisteredAt = DateTime.UtcNow,
                LastHeartbeatAt = DateTime.UtcNow
            });
            await dbContext.SaveChangesAsync();

            (UserAgentBindingInfoType? binding, string? error) = await bindingServer.CreateBindingAsync(new UserAgentBindingRequestType
            {
                UserId = user!.Id,
                AgentKey = "agent-1"
            });

            Assert.IsNull(error);
            Assert.IsNotNull(binding);
            Assert.AreEqual("agent-1", binding!.AgentKey);
            Assert.AreEqual("PC-1", binding.MachineName);
            Assert.IsTrue(await bindingServer.IsUserBoundToAgentAsync(Guid.Parse(user.Id!), "agent-1"));
        }

        /// <summary>
        /// DevicePortalServer only returns bound agents.
        /// </summary>
        [TestMethod()]
        public async Task DevicePortalReturnsOnlyBoundAgentsTest()
        {
            await using ZeronServerDbContext dbContext = CreateContext();
            UserManagerServer userManager = new(dbContext);
            UserAgentBindingServer bindingServer = new(dbContext);
            PackageDeployServer deployServer = new(
                new TaskDispatcherServer(dbContext, new CommandPublisherServer(new ServerSettings())),
                dbContext);
            DevicePortalServer portal = new(dbContext, bindingServer, deployServer);

            (UserInfoType? user, string? _) = await userManager.CreateUserAsync(new UserCreateRequestType
            {
                Username = "owner2",
                Password = "secret1",
                Role = ServerRoles.DeviceOwner
            });

            Guid agentId = Guid.NewGuid();
            dbContext.Agents.Add(new AgentEntity
            {
                Id = agentId,
                AgentKey = "bound-agent",
                MachineName = "PC-2",
                Status = "online",
                RegisteredAt = DateTime.UtcNow,
                LastHeartbeatAt = DateTime.UtcNow
            });
            dbContext.Agents.Add(new AgentEntity
            {
                Id = Guid.NewGuid(),
                AgentKey = "other-agent",
                MachineName = "PC-3",
                Status = "online",
                RegisteredAt = DateTime.UtcNow,
                LastHeartbeatAt = DateTime.UtcNow
            });
            await dbContext.SaveChangesAsync();

            await bindingServer.CreateBindingAsync(new UserAgentBindingRequestType
            {
                UserId = user!.Id,
                AgentKey = "bound-agent"
            });

            List<DeviceAgentStatusType> devices = await portal.GetMyDevicesAsync(Guid.Parse(user.Id!));

            Assert.AreEqual(1, devices.Count);
            Assert.AreEqual("bound-agent", devices[0].AgentKey);
            Assert.IsNull(await portal.GetMyDeviceAsync(Guid.Parse(user.Id!), "other-agent"));
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
