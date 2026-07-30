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
    public class TaskDispatcherServerTests
    {
        /// <summary>
        /// CreateTaskAsync assigns task to online agents.
        /// </summary>
        [TestMethod()]
        public async Task CreateTaskAsyncAssignsToOnlineAgentsTest()
        {
            string dbName = Guid.NewGuid().ToString();
            await using ZeronServerDbContext dbContext = CreateContext(dbName);

            dbContext.Agents.Add(new AgentEntity
            {
                Id = Guid.NewGuid(),
                AgentKey = "online-agent",
                Status = "online",
                MachineName = "HOST-A",
                RegisteredAt = DateTime.UtcNow,
                LastHeartbeatAt = DateTime.UtcNow
            });

            await dbContext.SaveChangesAsync();

            TaskDispatcherServer taskDispatcher = new(dbContext, new CommandPublisherServer(new ServerSettings()));
            TaskEntity task = await taskDispatcher.CreateTaskAsync(new TaskCreateRequestType
            {
                Name = "dispatch-test",
                TargetApi = "ServerInfo",
                Command = "",
                TargetType = "all"
            });

            Assert.AreEqual("pending", task.Status);
            Assert.AreEqual(1, task.Assignments.Count);
            Assert.AreEqual("pending", task.Assignments.First().Status);
        }

        /// <summary>
        /// ReportResultAsync completes assignment and stores result.
        /// </summary>
        [TestMethod()]
        public async Task ReportResultAsyncCompletesAssignmentTest()
        {
            string dbName = Guid.NewGuid().ToString();
            await using ZeronServerDbContext dbContext = CreateContext(dbName);
            Guid assignmentId = Guid.NewGuid();
            Guid taskId = Guid.NewGuid();
            Guid agentId = Guid.NewGuid();

            dbContext.Agents.Add(new AgentEntity
            {
                Id = agentId,
                AgentKey = "agent-001",
                Status = "online",
                RegisteredAt = DateTime.UtcNow
            });

            dbContext.Tasks.Add(new TaskEntity
            {
                Id = taskId,
                Name = "result-test",
                TargetApi = "HealthCheck",
                Command = "",
                Status = "running",
                CreatedAt = DateTime.UtcNow
            });

            dbContext.TaskAssignments.Add(new TaskAssignmentEntity
            {
                Id = assignmentId,
                TaskId = taskId,
                AgentId = agentId,
                Status = "dispatched",
                AssignedAt = DateTime.UtcNow
            });

            await dbContext.SaveChangesAsync();

            TaskDispatcherServer taskDispatcher = new(dbContext, new CommandPublisherServer(new ServerSettings()));
            bool reported = await taskDispatcher.ReportResultAsync(new TaskResultReportType
            {
                AssignmentId = assignmentId.ToString(),
                AgentId = "agent-001",
                Success = true,
                ResponseJson = "{\"success\":true}"
            });

            Assert.IsTrue(reported);

            TaskAssignmentEntity? assignment = await dbContext.TaskAssignments
                .Include(item => item.Result)
                .FirstOrDefaultAsync(item => item.Id == assignmentId);

            Assert.IsNotNull(assignment);
            Assert.AreEqual("completed", assignment!.Status);
            Assert.IsNotNull(assignment.Result);
            Assert.IsTrue(assignment.Result!.Success);
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
