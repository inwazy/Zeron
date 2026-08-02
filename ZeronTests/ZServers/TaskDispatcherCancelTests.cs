// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Microsoft.EntityFrameworkCore;
using Zeron.Server.Data;

namespace Zeron.Server.ZServers.Tests
{
    [TestClass()]
    public class TaskDispatcherCancelTests
    {
        /// <summary>
        /// CancelTaskAsync marks pending assignments cancelled.
        /// </summary>
        [TestMethod()]
        public async Task CancelTaskAsyncMarksAssignmentsCancelledTest()
        {
            string dbName = Guid.NewGuid().ToString();
            await using ZeronServerDbContext dbContext = CreateContext(dbName);
            Guid taskId = Guid.NewGuid();
            Guid assignmentId = Guid.NewGuid();

            dbContext.Tasks.Add(new Zeron.Server.Data.Entities.TaskEntity
            {
                Id = taskId,
                Name = "cancel-test",
                TargetApi = "HealthCheck",
                Command = "",
                Status = "pending",
                CreatedAt = DateTime.UtcNow
            });

            dbContext.TaskAssignments.Add(new Zeron.Server.Data.Entities.TaskAssignmentEntity
            {
                Id = assignmentId,
                TaskId = taskId,
                AgentId = Guid.NewGuid(),
                Status = "pending",
                AssignedAt = DateTime.UtcNow
            });

            await dbContext.SaveChangesAsync();

            TaskDispatcherServer taskDispatcher = new(dbContext, new CommandPublisherServer(new Zeron.Server.ZCore.ServerSettings()));
            bool cancelled = await taskDispatcher.CancelTaskAsync(taskId);

            Assert.IsTrue(cancelled);

            var task = await taskDispatcher.GetTaskAsync(taskId);

            Assert.IsNotNull(task);
            Assert.AreEqual("cancelled", task!.Status);
            Assert.AreEqual("cancelled", task.Assignments.First().Status);
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
