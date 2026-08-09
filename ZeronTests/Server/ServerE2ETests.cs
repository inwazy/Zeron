// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using Zeron.Server.Data.Entities;
using Zeron.ZCore.Type;

namespace Zeron.Server.Tests
{
    [TestClass()]
    public class ServerE2ETests
    {
        private static ZeronServerWebApplicationFactory? s_Factory;
        private const string AgentApiKey = "zeron.testkey";
        private const string TestAgentId = "e2e-agent-001";

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            s_Factory = new ZeronServerWebApplicationFactory();
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            s_Factory?.Dispose();
        }

        /// <summary>
        /// Heartbeat without API key returns 401.
        /// </summary>
        [TestMethod()]
        public async Task HeartbeatWithoutApiKeyReturnsUnauthorizedTest()
        {
            using HttpClient client = s_Factory!.CreateClient();
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/agents/heartbeat", CreateHeartbeatRequest());

            Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        /// <summary>
        /// Full E2E flow: heartbeat, task create, pending task, result report.
        /// </summary>
        [TestMethod()]
        public async Task AgentHeartbeatTaskDispatchFlowTest()
        {
            using HttpClient agentClient = s_Factory!.CreateClient();
            using HttpClient dashboardClient = s_Factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                HandleCookies = true
            });

            agentClient.DefaultRequestHeaders.Add("X-Zeron-Agent-Key", AgentApiKey);

            AgentHeartbeatResponseType heartbeat = await PostHeartbeatAsync(agentClient);

            Assert.IsTrue(heartbeat.Success);

            await LoginAsync(dashboardClient);

            List<AgentEntity>? agents = await dashboardClient.GetFromJsonAsync<List<AgentEntity>>("/api/agents");

            Assert.IsNotNull(agents);
            Assert.IsTrue(agents!.Any(agent => agent.AgentKey == TestAgentId && agent.Status == "online"));

            TaskCreateRequestType taskRequest = new()
            {
                Name = "e2e-health-check",
                TargetApi = "HealthCheck",
                Command = "",
                TargetType = "agent",
                AgentIds = [TestAgentId]
            };

            HttpResponseMessage createResponse = await dashboardClient.PostAsJsonAsync("/api/tasks", taskRequest);

            Assert.AreEqual(HttpStatusCode.Created, createResponse.StatusCode);

            AgentHeartbeatResponseType secondHeartbeat = await PostHeartbeatAsync(agentClient);

            Assert.IsNotNull(secondHeartbeat.PendingTasks);
            Assert.IsTrue(secondHeartbeat.PendingTasks!.Count > 0);

            PendingTaskType pendingTask = secondHeartbeat.PendingTasks[0];

            TaskResultReportType resultReport = new()
            {
                AssignmentId = pendingTask.AssignmentId,
                AgentId = TestAgentId,
                Success = true,
                ResponseJson = "{\"success\":true}"
            };

            HttpResponseMessage resultResponse = await agentClient.PostAsJsonAsync("/api/tasks/results", resultReport);

            Assert.AreEqual(HttpStatusCode.OK, resultResponse.StatusCode);
        }

        /// <summary>
        /// Health endpoints return healthy status without authentication.
        /// </summary>
        [TestMethod()]
        public async Task HealthEndpointsReturnHealthyTest()
        {
            using HttpClient client = s_Factory!.CreateClient();

            HttpResponseMessage healthResponse = await client.GetAsync("/health");
            HttpResponseMessage readyResponse = await client.GetAsync("/ready");

            Assert.AreEqual(HttpStatusCode.OK, healthResponse.StatusCode);
            Assert.AreEqual(HttpStatusCode.OK, readyResponse.StatusCode);

            HealthStatusType? health = await healthResponse.Content.ReadFromJsonAsync<HealthStatusType>();
            HealthStatusType? ready = await readyResponse.Content.ReadFromJsonAsync<HealthStatusType>();

            Assert.AreEqual("healthy", health?.Status);
            Assert.AreEqual("healthy", ready?.Status);
            Assert.AreEqual("healthy", ready?.Checks?["database"]);
        }

        /// <summary>
        /// Operator can deploy ManagedPackage via packages API.
        /// </summary>
        [TestMethod()]
        public async Task PackageDeployCreatesTaskTest()
        {
            using HttpClient agentClient = s_Factory!.CreateClient();
            using HttpClient dashboardClient = s_Factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                HandleCookies = true
            });

            agentClient.DefaultRequestHeaders.Add("X-Zeron-Agent-Key", AgentApiKey);
            await PostHeartbeatAsync(agentClient);
            await LoginAsync(dashboardClient);

            string packageName = "ccleaner-" + Guid.NewGuid().ToString("N")[..8];
            HttpResponseMessage catalogResponse = await dashboardClient.PostAsJsonAsync(
                "/api/packages/catalog",
                new ManagedPackageUpsertRequestType
                {
                    Name = packageName,
                    Urlx64 = "https://example.com/ccleaner.exe",
                    CmdInstallx64 = "/S",
                    IsEnabled = true
                });

            string catalogBody = await catalogResponse.Content.ReadAsStringAsync();
            Assert.AreEqual(HttpStatusCode.Created, catalogResponse.StatusCode, catalogBody);

            HttpResponseMessage syncResponse = await agentClient.GetAsync("/api/packages/catalog/sync");
            Assert.AreEqual(HttpStatusCode.OK, syncResponse.StatusCode);

            ManagedPackageCatalogSyncResponseType? sync = await syncResponse.Content
                .ReadFromJsonAsync<ManagedPackageCatalogSyncResponseType>();

            Assert.IsNotNull(sync);
            Assert.IsTrue(sync!.Success);
            Assert.IsTrue(sync.Packages.Any(package => package.Name == packageName));

            HttpResponseMessage deployResponse = await dashboardClient.PostAsJsonAsync(
                "/api/packages/deploy",
                new PackageDeployRequestType
                {
                    Operation = "install",
                    PackageName = packageName,
                    ExtraArgs = "/S",
                    TargetType = "agent",
                    AgentIds = [TestAgentId]
                });

            Assert.AreEqual(HttpStatusCode.Created, deployResponse.StatusCode);

            PackageDeployResponseType? deploy = await deployResponse.Content
                .ReadFromJsonAsync<PackageDeployResponseType>();

            Assert.IsNotNull(deploy);
            Assert.IsTrue(deploy!.Success);
            Assert.AreEqual("install " + packageName + " /S", deploy.Command);
            Assert.IsNotNull(deploy.TaskId);
        }

        /// <summary>
        /// Operator can create and list task schedules.
        /// </summary>
        [TestMethod()]
        public async Task ScheduleCreateAndListTest()
        {
            using HttpClient dashboardClient = s_Factory!.CreateClient(new WebApplicationFactoryClientOptions
            {
                HandleCookies = true
            });

            await LoginAsync(dashboardClient);

            string name = "e2e-sched-" + Guid.NewGuid().ToString("N")[..8];
            HttpResponseMessage createResponse = await dashboardClient.PostAsJsonAsync(
                "/api/schedules",
                new TaskScheduleCreateRequestType
                {
                    Name = name,
                    Cron = "*/15 * * * *",
                    Enabled = true,
                    TargetApi = "HealthCheck",
                    TargetType = "all"
                });

            Assert.AreEqual(HttpStatusCode.Created, createResponse.StatusCode);

            List<TaskScheduleInfoType>? schedules = await dashboardClient
                .GetFromJsonAsync<List<TaskScheduleInfoType>>("/api/schedules");

            Assert.IsNotNull(schedules);
            Assert.IsTrue(schedules!.Any(item => item.Name == name && item.Enabled));
        }

        /// <summary>
        /// Dashboard summary endpoint returns aggregated counts.
        /// </summary>
        [TestMethod()]
        public async Task DashboardSummaryReturnsCountsTest()
        {
            using HttpClient agentClient = s_Factory!.CreateClient();
            using HttpClient dashboardClient = s_Factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                HandleCookies = true
            });

            agentClient.DefaultRequestHeaders.Add("X-Zeron-Agent-Key", AgentApiKey);
            await PostHeartbeatAsync(agentClient);
            await LoginAsync(dashboardClient);

            DashboardSummaryType? summary = await dashboardClient
                .GetFromJsonAsync<DashboardSummaryType>("/api/dashboard/summary");

            Assert.IsNotNull(summary);
            Assert.IsTrue(summary!.AgentsTotal >= 1);
            Assert.IsTrue(summary.AgentsOnline >= 1);
            Assert.IsNotNull(summary.RecentAgents);
        }

        /// <summary>
        /// Admin can create users via API.
        /// </summary>
        [TestMethod()]
        public async Task AdminCanCreateUserTest()
        {
            using HttpClient dashboardClient = s_Factory!.CreateClient(new WebApplicationFactoryClientOptions
            {
                HandleCookies = true
            });

            await LoginAsync(dashboardClient);

            string username = "e2e-ops-" + Guid.NewGuid().ToString("N")[..8];
            HttpResponseMessage createResponse = await dashboardClient.PostAsJsonAsync("/api/users", new UserCreateRequestType
            {
                Username = username,
                Password = "secret12",
                Role = "Operator"
            });

            Assert.AreEqual(HttpStatusCode.Created, createResponse.StatusCode);

            List<UserInfoType>? users = await dashboardClient.GetFromJsonAsync<List<UserInfoType>>("/api/users");

            Assert.IsNotNull(users);
            Assert.IsTrue(users!.Any(user => user.Username == username && user.Role == "Operator"));
        }

        /// <summary>
        /// Diagnostics endpoint returns healthy agent after heartbeat.
        /// </summary>
        [TestMethod()]
        public async Task AgentDiagnosticsReturnsHealthyStateTest()
        {
            using HttpClient agentClient = s_Factory!.CreateClient();
            using HttpClient dashboardClient = s_Factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                HandleCookies = true
            });

            agentClient.DefaultRequestHeaders.Add("X-Zeron-Agent-Key", AgentApiKey);
            await PostHeartbeatAsync(agentClient);
            await LoginAsync(dashboardClient);

            List<AgentDiagnosticType>? diagnostics = await dashboardClient
                .GetFromJsonAsync<List<AgentDiagnosticType>>("/api/agents/diagnostics");

            Assert.IsNotNull(diagnostics);

            AgentDiagnosticType? diagnostic = diagnostics!.FirstOrDefault(item => item.AgentKey == TestAgentId);

            Assert.IsNotNull(diagnostic);
            Assert.AreEqual("healthy", diagnostic!.ConnectionState);
        }

        private static async Task<AgentHeartbeatResponseType> PostHeartbeatAsync(HttpClient client)
        {
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/agents/heartbeat", CreateHeartbeatRequest());

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

            AgentHeartbeatResponseType? heartbeat = await response.Content.ReadFromJsonAsync<AgentHeartbeatResponseType>();

            Assert.IsNotNull(heartbeat);

            return heartbeat!;
        }

        private static AgentHeartbeatRequestType CreateHeartbeatRequest()
        {
            return new AgentHeartbeatRequestType
            {
                AgentId = TestAgentId,
                MachineName = "E2E-HOST",
                UptimeSeconds = 60,
                Version = "1.0.0",
                InstallQueueCount = 0,
                InstallRunning = false,
                SchedulerTaskCount = 0
            };
        }

        private static string s_AdminPassword = "admin123";

        private static async Task LoginAsync(HttpClient client)
        {
            string[] candidates = [s_AdminPassword, "admin123", "admin", "admin-e2e-changed"];

            foreach (string password in candidates.Distinct())
            {
                HttpResponseMessage response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestType
                {
                    Username = "admin",
                    Password = password
                });

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    continue;
                }

                LoginResponseType? login = await response.Content.ReadFromJsonAsync<LoginResponseType>();

                Assert.IsNotNull(login);
                Assert.IsTrue(login!.Success);

                if (login.User?.MustChangePassword == true)
                {
                    HttpResponseMessage changeResponse = await client.PostAsJsonAsync(
                        "/api/auth/change-password",
                        new ChangePasswordRequestType
                        {
                            CurrentPassword = password,
                            NewPassword = "admin-e2e-changed"
                        });

                    Assert.AreEqual(HttpStatusCode.OK, changeResponse.StatusCode);
                    s_AdminPassword = "admin-e2e-changed";
                }
                else
                {
                    s_AdminPassword = password;
                }

                return;
            }

            Assert.Fail("Admin login failed with known test passwords.");
        }
    }
}
