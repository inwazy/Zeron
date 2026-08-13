// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Microsoft.EntityFrameworkCore;
using Zeron.Server.Data;
using Zeron.Server.Data.Entities;
using Zeron.Server.ZCore;
using Zeron.ZCore;
using Zeron.ZCore.Type;
using Zeron.ZCore.Utils;
using Zeron.ZInterfaces;

namespace Zeron.Server.ZServers.Tests
{
    [TestClass()]
    public class TaskDispatcherServerTests
    {
        [TestCleanup]
        public void Cleanup()
        {
            ZeronGateServer.Current.Clear();
        }

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

        /// <summary>
        /// ManagedPackage queued result keeps assignment running until install completes.
        /// </summary>
        [TestMethod()]
        public async Task ReportResultAsyncManagedPackageQueuedThenCompletedTest()
        {
            string dbName = Guid.NewGuid().ToString();
            await using ZeronServerDbContext dbContext = CreateContext(dbName);
            Guid assignmentId = Guid.NewGuid();
            Guid taskId = Guid.NewGuid();
            Guid agentId = Guid.NewGuid();

            dbContext.Agents.Add(new AgentEntity
            {
                Id = agentId,
                AgentKey = "pkg-agent",
                Status = "online",
                RegisteredAt = DateTime.UtcNow
            });

            dbContext.Tasks.Add(new TaskEntity
            {
                Id = taskId,
                Name = "deploy-ccleaner",
                TargetApi = "ManagedPackage",
                Command = "install ccleaner",
                Status = "pending",
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

            bool queued = await taskDispatcher.ReportResultAsync(new TaskResultReportType
            {
                AssignmentId = assignmentId.ToString(),
                AgentId = "pkg-agent",
                Success = true,
                ResponseJson = "{\"success\":true,\"result\":{\"queued\":true,\"package\":\"ccleaner\"}}"
            });

            Assert.IsTrue(queued);

            TaskAssignmentEntity? runningAssignment = await dbContext.TaskAssignments
                .Include(item => item.Task)
                .Include(item => item.Result)
                .FirstOrDefaultAsync(item => item.Id == assignmentId);

            Assert.IsNotNull(runningAssignment);
            Assert.AreEqual("running", runningAssignment!.Status);
            Assert.AreEqual("running", runningAssignment.Task!.Status);
            Assert.IsNull(runningAssignment.Result);
            Assert.IsNull(runningAssignment.CompletedAt);

            bool completed = await taskDispatcher.ReportResultAsync(new TaskResultReportType
            {
                AssignmentId = assignmentId.ToString(),
                AgentId = "pkg-agent",
                Success = true,
                ResponseJson = "{\"success\":true,\"completed\":true,\"package\":\"ccleaner\",\"exitCode\":0}"
            });

            Assert.IsTrue(completed);

            TaskAssignmentEntity? finalAssignment = await dbContext.TaskAssignments
                .Include(item => item.Task)
                .Include(item => item.Result)
                .FirstOrDefaultAsync(item => item.Id == assignmentId);

            Assert.IsNotNull(finalAssignment);
            Assert.AreEqual("completed", finalAssignment!.Status);
            Assert.AreEqual("completed", finalAssignment.Task!.Status);
            Assert.IsNotNull(finalAssignment.Result);
            Assert.IsTrue(finalAssignment.Result!.Success);
            Assert.IsNotNull(finalAssignment.CompletedAt);
        }

        /// <summary>
        /// DispatchPendingAssignmentsAsync fails assignment when gate cancels.
        /// </summary>
        [TestMethod()]
        public async Task DispatchPendingAssignmentsAsyncGateCancelFailsAssignmentTest()
        {
            ZeronGateServer.Current.Clear();
            ZeronGateServer.Current.Register(new CancelDispatchGate());

            try
            {
                string dbName = Guid.NewGuid().ToString();
                await using ZeronServerDbContext dbContext = CreateContext(dbName);

                dbContext.Agents.Add(new AgentEntity
                {
                    Id = Guid.NewGuid(),
                    AgentKey = "gate-agent",
                    Status = "online",
                    MachineName = "HOST",
                    RegisteredAt = DateTime.UtcNow,
                    LastHeartbeatAt = DateTime.UtcNow
                });
                await dbContext.SaveChangesAsync();

                TaskDispatcherServer taskDispatcher = new(dbContext, new CommandPublisherServer(new ServerSettings()));
                TaskEntity task = await taskDispatcher.CreateTaskAsync(new TaskCreateRequestType
                {
                    Name = "gate-dispatch",
                    TargetApi = "HealthCheck",
                    Command = "",
                    TargetType = "all"
                });

                int dispatched = await taskDispatcher.DispatchPendingAssignmentsAsync();

                Assert.AreEqual(0, dispatched);

                TaskAssignmentEntity? assignment = await dbContext.TaskAssignments
                    .Include(item => item.Result)
                    .FirstOrDefaultAsync(item => item.TaskId == task.Id);

                Assert.IsNotNull(assignment);
                Assert.AreEqual("failed", assignment!.Status);
                Assert.IsNotNull(assignment.Result);
                Assert.IsTrue(assignment.Result!.ErrorMessage?.Contains("gate", StringComparison.OrdinalIgnoreCase));
            }
            finally
            {
                ZeronGateServer.Current.Clear();
            }
        }

        private sealed class CancelDispatchGate : IGateHandler
        {
            public void Handle(
                GateContextType context)
            {
                if (context.Topic == ZeronEventTopics.GateDispatch)
                {
                    context.Decision = GateDecisionType.Cancel;
                }
            }
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
