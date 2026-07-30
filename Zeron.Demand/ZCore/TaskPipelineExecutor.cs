// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using System.Globalization;
using System.Text.Json;
using Zeron.ZCore;
using Zeron.ZCore.Type;
using Zeron.ZCore.Utils;
using Zeron.ZInterfaces;
using Zeron.ZServers;

namespace Zeron.Demand.ZCore
{
    /// <summary>
    /// TaskPipelineExecutor - executes JSON-defined task pipelines.
    /// </summary>
    internal static class TaskPipelineExecutor
    {
        /// <summary>
        /// ExecuteTask
        /// </summary>
        /// <param name="task"></param>
        /// <returns>Returns execution result.</returns>
        public static TaskPipelineResult ExecuteTask(SchedulerTaskDefinition task)
        {
            TaskPipelineResult result = new()
            {
                TaskName = task.Name,
                Success = true,
                Steps = []
            };

            if (task.Steps == null || task.Steps.Count == 0)
            {
                result.Success = false;
                result.Message = "Task has no steps.";

                return result;
            }

            InstallEventPublisher.Publish("task.started", JsonSerializer.Serialize(new
            {
                task = task.Name,
                timestamp = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture)
            }));

            foreach (TaskStepDefinition step in task.Steps)
            {
                TaskStepResult stepResult = ExecuteStep(step);
                result.Steps.Add(stepResult);

                if (!stepResult.Success)
                {
                    result.Success = false;
                    result.Message = stepResult.Message;

                    break;
                }
            }

            InstallEventPublisher.Publish(result.Success ? "task.completed" : "task.failed", JsonSerializer.Serialize(new
            {
                task = task.Name,
                success = result.Success,
                message = result.Message,
                timestamp = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture)
            }));

            ZNLogger.Common.Info(string.Format(CultureInfo.InvariantCulture,
                "TaskPipeline '{0}' finished: success={1}", task.Name, result.Success));

            return result;
        }

        /// <summary>
        /// ExecuteStep
        /// </summary>
        /// <param name="step"></param>
        /// <returns>Returns step result.</returns>
        public static TaskStepResult ExecuteStep(TaskStepDefinition step)
        {
            string stepType = step.Type?.Trim().ToLowerInvariant() ?? "";

            return stepType switch
            {
                "powershell" => ExecutePowerShellStep(step),
                "managedpackage" => ExecuteApiStep("ManagedPackage", step.Command),
                "wait" => ExecuteWaitStep(step),
                "api" => ExecuteApiStep(step.ApiName, step.Command),
                _ => new TaskStepResult
                {
                    Type = stepType,
                    Success = false,
                    Message = $"Unknown step type: {step.Type}"
                }
            };
        }

        /// <summary>
        /// ExecutePowerShellStep
        /// </summary>
        /// <param name="step"></param>
        /// <returns>Returns step result.</returns>
        private static TaskStepResult ExecutePowerShellStep(TaskStepDefinition step)
        {
            bool success = ScriptExecutor.Execute(step.Script);

            return new TaskStepResult
            {
                Type = "powershell",
                Success = success,
                Message = success ? null : "PowerShell step failed."
            };
        }

        /// <summary>
        /// ExecuteWaitStep
        /// </summary>
        /// <param name="step"></param>
        /// <returns>Returns step result.</returns>
        private static TaskStepResult ExecuteWaitStep(TaskStepDefinition step)
        {
            int seconds = step.Seconds > 0 ? step.Seconds : 1;
            Thread.Sleep(TimeSpan.FromSeconds(seconds));

            return new TaskStepResult
            {
                Type = "wait",
                Success = true,
                Message = $"Waited {seconds} second(s)."
            };
        }

        /// <summary>
        /// ExecuteApiStep
        /// </summary>
        /// <param name="apiName"></param>
        /// <param name="command"></param>
        /// <returns>Returns step result.</returns>
        private static TaskStepResult ExecuteApiStep(string? apiName, string? command)
        {
            string responseJson = InternalServiceInvoker.Invoke(apiName, command);
            bool success = responseJson.Contains("\"success\":true", StringComparison.OrdinalIgnoreCase)
                || responseJson.Contains("\"success\": true", StringComparison.OrdinalIgnoreCase);

            return new TaskStepResult
            {
                Type = "api",
                Success = success,
                Message = success ? null : responseJson,
                Response = responseJson
            };
        }
    }

    /// <summary>
    /// TaskPipelineResult
    /// </summary>
    internal class TaskPipelineResult
    {
        /// <summary>
        /// TaskName
        /// </summary>
        /// <returns>Returns string.</returns>
        public string? TaskName
        {
            get;
            set;
        }

        /// <summary>
        /// Success
        /// </summary>
        /// <returns>Returns bool.</returns>
        public bool Success
        {
            get;
            set;
        }

        /// <summary>
        /// Message
        /// </summary>
        /// <returns>Returns string.</returns>
        public string? Message
        {
            get;
            set;
        }

        /// <summary>
        /// Steps
        /// </summary>
        /// <returns>Returns list of TaskStepResult.</returns>
        public List<TaskStepResult> Steps
        {
            get;
            set;
        } = [];
    }

    /// <summary>
    /// TaskStepResult
    /// </summary>
    internal class TaskStepResult
    {
        /// <summary>
        /// Type
        /// </summary>
        /// <returns>Returns string.</returns>
        public string? Type
        {
            get;
            set;
        }

        /// <summary>
        /// Success
        /// </summary>
        /// <returns>Returns bool.</returns>
        public bool Success
        {
            get;
            set;
        }

        /// <summary>
        /// Message
        /// </summary>
        /// <returns>Returns string.</returns>
        public string? Message
        {
            get;
            set;
        }

        /// <summary>
        /// Response
        /// </summary>
        /// <returns>Returns string.</returns>
        public string? Response
        {
            get;
            set;
        }
    }
}
