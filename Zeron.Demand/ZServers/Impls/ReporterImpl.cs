// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using System.Globalization;
using System.Net.Http.Json;
using Zeron.ZCore.Type;
using Zeron.ZInterfaces;

namespace Zeron.Demand.ZServers.Impls
{
    /// <summary>
    /// ReporterImpl
    /// </summary>
    internal class ReporterImpl : IImpl
    {
        // Http client.
        private static readonly HttpClient s_HttpClient = new();

        // Server URL.
        private static string? s_ServerUrl;

        /// <summary>
        /// ServerUrl
        /// </summary>
        public static string? ServerUrl
        {
            get => s_ServerUrl;
            set => s_ServerUrl = value?.TrimEnd('/');
        }

        /// <summary>
        /// Dispose
        /// </summary>
        /// <returns>Returns void.</returns>
        public void Dispose()
        {
        }

        /// <summary>
        /// SendHeartbeatAsync
        /// </summary>
        /// <param name="request"></param>
        /// <returns>Returns AgentHeartbeatResponseType.</returns>
        public static async Task<AgentHeartbeatResponseType?> SendHeartbeatAsync(AgentHeartbeatRequestType request)
        {
            if (string.IsNullOrWhiteSpace(s_ServerUrl))
            {
                return null;
            }

            try
            {
                HttpResponseMessage response = await s_HttpClient.PostAsJsonAsync(
                    s_ServerUrl + "/api/agents/heartbeat",
                    request);

                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                return await response.Content.ReadFromJsonAsync<AgentHeartbeatResponseType>();
            }
            catch (Exception e)
            {
                Zeron.ZCore.ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture,
                    "ReporterImpl SendHeartbeatAsync Error:{0}\n{1}", e.Message, e.StackTrace));

                return null;
            }
        }

        /// <summary>
        /// SendEventAsync
        /// </summary>
        /// <param name="report"></param>
        /// <returns>Returns void.</returns>
        public static async Task SendEventAsync(AgentEventReportType report)
        {
            if (string.IsNullOrWhiteSpace(s_ServerUrl))
            {
                return;
            }

            try
            {
                await s_HttpClient.PostAsJsonAsync(s_ServerUrl + "/api/events", report);
            }
            catch (Exception e)
            {
                Zeron.ZCore.ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture,
                    "ReporterImpl SendEventAsync Error:{0}\n{1}", e.Message, e.StackTrace));
            }
        }

        /// <summary>
        /// SendTaskResultAsync
        /// </summary>
        /// <param name="report"></param>
        /// <returns>Returns void.</returns>
        public static async Task SendTaskResultAsync(TaskResultReportType report)
        {
            if (string.IsNullOrWhiteSpace(s_ServerUrl))
            {
                return;
            }

            try
            {
                await s_HttpClient.PostAsJsonAsync(s_ServerUrl + "/api/tasks/results", report);
            }
            catch (Exception e)
            {
                Zeron.ZCore.ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture,
                    "ReporterImpl SendTaskResultAsync Error:{0}\n{1}", e.Message, e.StackTrace));
            }
        }
    }
}
