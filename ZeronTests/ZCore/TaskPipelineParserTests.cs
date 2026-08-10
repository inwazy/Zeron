// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Zeron.ZCore.Type;

namespace Zeron.ZCore.Tests
{
    [TestClass()]
    public class TaskPipelineParserTests
    {
        [TestMethod()]
        public void ParseJsonReturnsTasksTest()
        {
            const string json = """
            {
              "tasks": [
                {
                  "name": "sample-task",
                  "cron": "0 2 * * *",
                  "enabled": true,
                  "steps": [
                    { "type": "wait", "seconds": 1 },
                    { "type": "powershell", "script": "Write-Output ok" }
                  ]
                }
              ]
            }
            """;

            List<SchedulerTaskDefinition> tasks = TaskPipelineParser.ParseJson(json);

            Assert.AreEqual(1, tasks.Count);
            Assert.AreEqual("sample-task", tasks[0].Name);
            Assert.AreEqual(2, tasks[0].Steps?.Count);
        }

        [TestMethod()]
        public void ParseJsonScriptStepWithEngineTest()
        {
            const string json = """
            {
              "tasks": [
                {
                  "name": "script-host-task",
                  "cron": "0 2 * * *",
                  "enabled": true,
                  "steps": [
                    { "type": "script", "engine": "powershell", "script": "Write-Output ok" }
                  ]
                }
              ]
            }
            """;

            List<SchedulerTaskDefinition> tasks = TaskPipelineParser.ParseJson(json);

            Assert.AreEqual(1, tasks.Count);
            Assert.IsNotNull(tasks[0].Steps);
            Assert.AreEqual(1, tasks[0].Steps!.Count);
            TaskStepDefinition step = tasks[0].Steps[0];
            Assert.AreEqual("script", step.Type);
            Assert.AreEqual("powershell", step.Engine);
            Assert.AreEqual("Write-Output ok", step.Script);
        }

        [TestMethod()]
        public void FindTaskByNameTest()
        {
            List<SchedulerTaskDefinition> tasks =
            [
                new SchedulerTaskDefinition { Name = "alpha", Cron = "0 * * * *" },
                new SchedulerTaskDefinition { Name = "beta", Cron = "0 0 * * *" }
            ];

            SchedulerTaskDefinition? found = TaskPipelineParser.FindTask(tasks, "beta");

            Assert.IsNotNull(found);
            Assert.AreEqual("beta", found!.Name);
        }
    }
}
