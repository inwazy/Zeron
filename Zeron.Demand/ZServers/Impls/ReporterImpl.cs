// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Zeron.ZCore.Type;
using Zeron.ZCore.Utils;
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

        // JSON options (camelCase, matching prior JsonContent.Create behavior).
        private static readonly JsonSerializerOptions s_JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        // Server URL.
        private static string? s_ServerUrl;

        // Server API key.
        private static string? s_ServerApiKey;

        // HMAC enabled.
        private static bool s_HmacEnabled;

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
        /// HmacEnabled
        /// </summary>
        public static bool HmacEnabled
        {
            get => s_HmacEnabled;
            set => s_HmacEnabled = value;
        }

        /// <summary>
        /// Dispose
        /// </summary>
        /// <returns>Returns void.</returns>
        public void Dispose()
        {
        }

        /// <summary>
        /// SendRequestAsync
        /// </summary>
        /// <param name="method"></param>
        /// <param name="path"></param>
        /// <param name="body"></param>
        /// <returns>Returns HttpResponseMessage.</returns>
        private static async Task<HttpResponseMessage> SendRequestAsync(
            HttpMethod method,
            string path,
            object? body = null)
        {
            byte[] bodyBytes = body == null
                ? Array.Empty<byte>()
                : JsonSerializer.SerializeToUtf8Bytes(body, s_JsonOptions);

            ByteArrayContent? content = null;

            if (body != null)
            {
                content = new ByteArrayContent(bodyBytes);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            }

            HttpRequestMessage request = new(method, s_ServerUrl + path)
            {
                Content = content
            };

            if (!string.IsNullOrWhiteSpace(s_ServerApiKey))
            {
                string primaryKey = AgentApiKeyServer.GetPrimaryKey(s_ServerApiKey);

                if (string.IsNullOrEmpty(primaryKey))
                {
                    primaryKey = s_ServerApiKey;
                }

                request.Headers.Add(AgentHmacServer.AgentKeyHeader, primaryKey);

                if (s_HmacEnabled)
                {
                    long timestampUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    string bodyHash = AgentHmacServer.ComputeBodySha256Hex(bodyBytes);
                    string signature = AgentHmacServer.CreateSignature(
                        primaryKey,
                        method.Method,
                        path,
                        timestampUnix,
                        bodyHash);

                    request.Headers.Add(
                        AgentHmacServer.TimestampHeader,
                        timestampUnix.ToString(CultureInfo.InvariantCulture));
                    request.Headers.Add(AgentHmacServer.SignatureHeader, signature);
                }
            }

            return await s_HttpClient.SendAsync(request);
        }

        /// <summary>
        /// SendHeartbeatAsync
        /// </summary>
        /// <param name="request"></param>
        /// <returns>Returns AgentHeartbeatResponseType.</returns>
        public static async Task<AgentHeartbeatResponseType?> SendHeartbeatAsync(
            AgentHeartbeatRequestType request)
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
                    request);

                if (!response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    Zeron.ZCore.ZNLogger.Common.Warn(string.Format(CultureInfo.InvariantCulture,
                        "ReporterImpl SendHeartbeatAsync failed. Status={0}, Body={1}",
                        (int)response.StatusCode,
                        responseBody));

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
        public static async Task SendEventAsync(
            AgentEventReportType report)
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
                    report);
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
        public static async Task SendTaskResultAsync(
            TaskResultReportType report)
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
                    report);
            }
            catch (Exception e)
            {
                Zeron.ZCore.ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture,
                    "ReporterImpl SendTaskResultAsync Error:{0}\n{1}", e.Message, e.StackTrace));
            }
        }
    }
}
