// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using System.Globalization;
using Zeron.Demand.ZCore;
using Zeron.ZAttribute;
using Zeron.ZCore;
using Zeron.ZCore.Type;
using Zeron.ZInterfaces;
using Zeron.ZServers;

namespace Zeron.Demand.ZServices
{
    [ServicesRep(ZmqApiName = "HealthCheck", ZmqApiEnabled = true, ZmqNotifySubscriber = false, ApiScope = "read")]

    /// <summary>
    /// HealthCheck
    /// </summary>
    internal class HealthCheck : IServices
    {
        /// <summary>
        /// OnRequest
        /// </summary>
        /// <param name="aJson"></param>
        /// <returns>Returns string.</returns>
        public string OnRequest(dynamic aJson)
        {
            try
            {
                InstallJobStatus installStatus = InstallJobTracker.GetStatus();

                var result = new
                {
                    agentId = AgentServer.AgentId,
                    machineName = Environment.MachineName,
                    uptimeSeconds = AgentServer.UptimeSeconds,
                    startedAt = AgentServer.StartedAtUtc.ToString("o", CultureInfo.InvariantCulture),
                    version = typeof(HealthCheck).Assembly.GetName().Version?.ToString(),
                    installQueueCount = installStatus.QueueCount,
                    installRunning = installStatus.IsRunning,
                    schedulerTaskCount = SchedulerServer.GetTasks().Count
                };

                return ServiceResponse.SerializeSuccess(result);
            }
            catch (Exception e)
            {
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "HealthCheck Error:{0}\n{1}", e.Message, e.StackTrace));

                return ServiceResponse.SerializeFailure(e.Message);
            }
        }

        /// <summary>
        /// OnRequestAsync
        /// </summary>
        /// <param name="aJson"></param>
        /// <returns>Returns string.</returns>
        public string OnRequestAsync(dynamic aJson) => "";

        /// <summary>
        /// OnSubscriber
        /// </summary>
        /// <param name="aJson"></param>
        /// <returns>Returns string.</returns>
        public string OnSubscriber(dynamic aJson) => "";

        /// <summary>
        /// OnSubscriberAsync
        /// </summary>
        /// <param name="aJson"></param>
        /// <returns>Returns string.</returns>
        public string OnSubscriberAsync(dynamic aJson) => "";

        /// <summary>
        /// OnNotifySubscriber
        /// </summary>
        /// <param name="aJson"></param>
        /// <param name="processedMsg"></param>
        /// <returns>Returns string.</returns>
        public string OnNotifySubscriber(dynamic aJson, string processedMsg) => "";
    }
}
