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
    public class EventIngestorPackageDeployTests
    {
        /// <summary>
        /// install.completed with assignmentId finalizes the ManagedPackage assignment.
        /// </summary>
        [TestMethod()]
        public async Task IngestInstallCompletedUpdatesAssignmentTest()
        {
            await using ZeronServerDbContext dbContext = CreateContext();
            Guid assignmentId = Guid.NewGuid();
            Guid taskId = Guid.NewGuid();
            Guid agentId = Guid.NewGuid();

            dbContext.Agents.Add(new AgentEntity
            {
                Id = agentId,
                AgentKey = "event-agent",
                Status = "online",
                RegisteredAt = DateTime.UtcNow,
                LastHeartbeatAt = DateTime.UtcNow
            });

            dbContext.Tasks.Add(new TaskEntity
            {
                Id = taskId,
                Name = "deploy-via-event",
                TargetApi = "ManagedPackage",
                Command = "install ccleaner",
                Status = "running",
                CreatedAt = DateTime.UtcNow
            });

            dbContext.TaskAssignments.Add(new TaskAssignmentEntity
            {
                Id = assignmentId,
                TaskId = taskId,
                AgentId = agentId,
                Status = "running",
                AssignedAt = DateTime.UtcNow,
                StartedAt = DateTime.UtcNow
            });

            await dbContext.SaveChangesAsync();

            TaskDispatcherServer taskDispatcher = new(dbContext, new CommandPublisherServer(new ServerSettings()));
            EventIngestorServer eventIngestor = new(dbContext, taskDispatcher);

            bool ingested = await eventIngestor.IngestEventAsync(new AgentEventReportType
            {
                AgentId = "event-agent",
                Topic = "install.completed",
                Payload = "{\"topic\":\"install.completed\",\"package\":\"ccleaner\",\"operation\":\"install\",\"success\":true,\"exitCode\":0,\"assignmentId\":\""
                    + assignmentId
                    + "\"}"
            });

            Assert.IsTrue(ingested);

            TaskAssignmentEntity? assignment = await dbContext.TaskAssignments
                .Include(item => item.Task)
                .Include(item => item.Result)
                .FirstOrDefaultAsync(item => item.Id == assignmentId);

            Assert.IsNotNull(assignment);
            Assert.AreEqual("completed", assignment!.Status);
            Assert.AreEqual("completed", assignment.Task!.Status);
            Assert.IsNotNull(assignment.Result);
            Assert.IsTrue(assignment.Result!.Success);
        }

        /// <summary>
        /// install.failed with assignmentId marks the assignment failed.
        /// </summary>
        [TestMethod()]
        public async Task IngestInstallFailedUpdatesAssignmentTest()
        {
            await using ZeronServerDbContext dbContext = CreateContext();
            Guid assignmentId = Guid.NewGuid();
            Guid taskId = Guid.NewGuid();
            Guid agentId = Guid.NewGuid();

            dbContext.Agents.Add(new AgentEntity
            {
                Id = agentId,
                AgentKey = "fail-agent",
                Status = "online",
                RegisteredAt = DateTime.UtcNow
            });

            dbContext.Tasks.Add(new TaskEntity
            {
                Id = taskId,
                Name = "deploy-fail",
                TargetApi = "ManagedPackage",
                Command = "install missing",
                Status = "running",
                CreatedAt = DateTime.UtcNow
            });

            dbContext.TaskAssignments.Add(new TaskAssignmentEntity
            {
                Id = assignmentId,
                TaskId = taskId,
                AgentId = agentId,
                Status = "running",
                AssignedAt = DateTime.UtcNow
            });

            await dbContext.SaveChangesAsync();

            TaskDispatcherServer taskDispatcher = new(dbContext, new CommandPublisherServer(new ServerSettings()));
            EventIngestorServer eventIngestor = new(dbContext, taskDispatcher);

            bool ingested = await eventIngestor.IngestEventAsync(new AgentEventReportType
            {
                AgentId = "fail-agent",
                Topic = "install.failed",
                Payload = "{\"package\":\"missing\",\"success\":false,\"exitCode\":1,\"assignmentId\":\""
                    + assignmentId
                    + "\"}"
            });

            Assert.IsTrue(ingested);

            TaskAssignmentEntity? assignment = await dbContext.TaskAssignments
                .Include(item => item.Task)
                .FirstOrDefaultAsync(item => item.Id == assignmentId);

            Assert.IsNotNull(assignment);
            Assert.AreEqual("failed", assignment!.Status);
            Assert.AreEqual("failed", assignment.Task!.Status);
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
