// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using System.Globalization;
using Zeron.Demand.ZCore;
using Zeron.ZAttribute;
using Zeron.ZCore;
using Zeron.ZInterfaces;
using Zeron.ZServers;

namespace Zeron.Demand.ZServices
{
    [ServicesRep(ZmqApiName = "Scheduler", ZmqApiEnabled = true, ZmqNotifySubscriber = false, ApiScope = "admin")]

    /// <summary>
    /// Scheduler - manage scheduled tasks.
    /// </summary>
    internal class Scheduler : IServices
    {
        /// <summary>
        /// OnRequest
        /// </summary>
        /// <param name="aJson"></param>
        /// <returns>Returns string.</returns>
        public string OnRequest(
            dynamic aJson)
        {
            try
            {
                string? command = Convert.ToString(aJson["Command"]);
                (string? verb, string? arguments) = Helper.SplitCommand(command);

                if (string.IsNullOrEmpty(verb))
                {
                    return ServiceResponse.SerializeFailure("Usage: list | reload | run taskName");
                }

                return verb.ToLowerInvariant() switch
                {
                    "list" => ListTasks(),
                    "reload" => ReloadTasks(),
                    "run" => RunTask(arguments),
                    _ => ServiceResponse.SerializeFailure($"Unknown Scheduler command: {verb}")
                };
            }
            catch (Exception e)
            {
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "Scheduler Error:{0}\n{1}", e.Message, e.StackTrace));

                return ServiceResponse.SerializeFailure(e.Message);
            }
        }

        /// <summary>
        /// ListTasks
        /// </summary>
        /// <returns>Returns JSON response.</returns>
        private static string ListTasks()
        {
            var tasks = SchedulerServer.GetTasks()
                .Select(task => new
                {
                    name = task.Name,
                    cron = task.Cron,
                    enabled = task.Enabled,
                    stepCount = task.Steps?.Count ?? 0
                })
                .ToList();

            return ServiceResponse.SerializeSuccess(tasks);
        }

        /// <summary>
        /// ReloadTasks
        /// </summary>
        /// <returns>Returns JSON response.</returns>
        private static string ReloadTasks()
        {
            SchedulerServer.ReloadTasks();

            return ServiceResponse.SerializeSuccess(new
            {
                reloaded = true,
                taskCount = SchedulerServer.GetTasks().Count
            });
        }

        /// <summary>
        /// RunTask
        /// </summary>
        /// <param name="taskName"></param>
        /// <returns>Returns JSON response.</returns>
        private static string RunTask(
            string? taskName)
        {
            return InternalServiceInvoker.Invoke("TaskPipeline", $"run {taskName}");
        }

        /// <summary>
        /// OnRequestAsync
        /// </summary>
        /// <param name="aJson"></param>
        /// <returns>Returns string.</returns>
        public string OnRequestAsync(
            dynamic aJson) => "";

        /// <summary>
        /// OnSubscriber
        /// </summary>
        /// <param name="aJson"></param>
        /// <returns>Returns string.</returns>
        public string OnSubscriber(
            dynamic aJson) => "";

        /// <summary>
        /// OnSubscriberAsync
        /// </summary>
        /// <param name="aJson"></param>
        /// <returns>Returns string.</returns>
        public string OnSubscriberAsync(
            dynamic aJson) => "";

        /// <summary>
        /// OnNotifySubscriber
        /// </summary>
        /// <param name="aJson"></param>
        /// <param name="processedMsg"></param>
        /// <returns>Returns string.</returns>
        public string OnNotifySubscriber(
            dynamic aJson, 
            string processedMsg) => "";
    }
}
