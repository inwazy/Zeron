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

        // Server API key.
        private static string? s_ServerApiKey;

        /// <summary>
        /// ServerUrl
        /// </summary>
        public static string? ServerUrl
        {
            get => s_ServerUrl;
            set => s_ServerUrl = value?.TrimEnd('/');
        }

        /// <summary>
        /// ServerApiKey
        /// </summary>
        public static string? ServerApiKey
        {
            get => s_ServerApiKey;
            set => s_ServerApiKey = value;
        }

        /// <summary>
        /// Dispose
        /// </summary>
        /// <returns>Returns void.</returns>
        public void Dispose()
        {
        }

        /// <summary>
        /// CreateRequestAsync
        /// </summary>
        /// <param name="method"></param>
        /// <param name="path"></param>
        /// <param name="content"></param>
        /// <returns>Returns HttpResponseMessage.</returns>
        private static async Task<HttpResponseMessage> SendRequestAsync(
            HttpMethod method,
            string path,
            HttpContent? content = null)
        {
            HttpRequestMessage request = new(method, s_ServerUrl + path)
            {
                Content = content
            };

            if (!string.IsNullOrWhiteSpace(s_ServerApiKey))
            {
                request.Headers.Add("X-Zeron-Agent-Key", s_ServerApiKey);
            }

            return await s_HttpClient.SendAsync(request);
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
                HttpResponseMessage response = await SendRequestAsync(
                    HttpMethod.Post,
                    "/api/agents/heartbeat",
                    JsonContent.Create(request));

                if (!response.IsSuccessStatusCode)
                {
                    string body = await response.Content.ReadAsStringAsync();
                    Zeron.ZCore.ZNLogger.Common.Warn(string.Format(CultureInfo.InvariantCulture,
                        "ReporterImpl SendHeartbeatAsync failed. Status={0}, Body={1}",
                        (int)response.StatusCode,
                        body));

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
                await SendRequestAsync(
                    HttpMethod.Post,
                    "/api/events",
                    JsonContent.Create(report));
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
                await SendRequestAsync(
                    HttpMethod.Post,
                    "/api/tasks/results",
                    JsonContent.Create(report));
            }
            catch (Exception e)
            {
                Zeron.ZCore.ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture,
                    "ReporterImpl SendTaskResultAsync Error:{0}\n{1}", e.Message, e.StackTrace));
            }
        }
    }
}
