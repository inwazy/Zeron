// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using Zeron.Demand.ZCore;
using Zeron.ZCore;
using Zeron.ZCore.Utils;
using Zeron.ZInterfaces;
using Zeron.ZServers;

namespace Zeron.Demand.ZServers.Impls
{
    /// <summary>
    /// ZmqImpl
    /// </summary>
    internal class ZmqImpl : IImpl
    {
        // Publisher thread.
        private static readonly Thread m_PublisherThread = new(PublisherSocketProc);

        // Subscriber thread.
        private static readonly Thread m_SubscriberThread = new(SubscriberSocketProc);

        // Response thread.
        private static readonly Thread m_ResponseThread = new(ResponseSocketProc);

        // Publisher socket.
        private static readonly PublisherSocket m_PublisherSocket = new();

        // Subscriber socket.
        private static readonly SubscriberSocket m_SubscriberSocket = new();

        // Response socket.
        private static readonly ResponseSocket m_ResponseSocket = new();

        // Publisher signal.
        private static readonly Semaphore m_PublisherSignal = new(0, 20000);

        // Publisher API queue messages.
        private static readonly ConcurrentQueue<Tuple<string, byte[]>> m_PubAPIQueueMessages = new();

        // Enable publisher process.
        private static bool m_EnablePublisherProc = true;

        // Enable subscriber process.
        private static bool m_EnableSubscriberProc = true;

        // Enable response process.
        private static bool m_EnableResponseProc = true;

        // Subscriber API key.
        private static string m_SubscriberApiKey = "";

        // Response API key.
        private static string m_ResponsetApiKey = "";

        // Response API scopes.
        private static string m_RepApiScopes = "*";

        // Subscriber API scopes.
        private static string m_SubApiScopes = "*";

        /// <summary>
        /// Dispose
        /// </summary>
        /// <returns>Returns void.</returns>
        public void Dispose()
        {
            m_EnablePublisherProc = false;
            m_EnableSubscriberProc = false;
            m_EnableResponseProc = false;

            try
            {
                m_PublisherSignal.Release();
            }
            catch (SemaphoreFullException)
            {
            }

            m_PublisherSocket.Dispose();
            m_SubscriberSocket.Dispose();
            m_ResponseSocket.Dispose();
            m_PublisherSignal.Dispose();
        }

        /// <summary>
        /// PrepareServices
        /// </summary>
        /// <param name="assembly"></param>
        /// <returns>Returns void.</returns>
        public static void PrepareServices(Assembly assembly)
        {
            ServiceRegistry.RegisterFromAssembly(assembly);
        }

        /// <summary>
        /// PrepareSubAPI
        /// </summary>
        /// <param name="apiKey"></param>
        /// <param name="apiScopes"></param>
        /// <returns>Returns void.</returns>
        public void PrepareSubAPI(string? apiKey, string? apiScopes)
        {
            m_SubscriberApiKey = apiKey ?? "";
            m_SubApiScopes = string.IsNullOrWhiteSpace(apiScopes) ? "*" : apiScopes;
        }

        /// <summary>
        /// PrepareRepAPI
        /// </summary>
        /// <param name="apiKey"></param>
        /// <param name="apiScopes"></param>
        /// <returns>Returns void.</returns>
        public void PrepareRepAPI(string? apiKey, string? apiScopes)
        {
            m_ResponsetApiKey = apiKey ?? "";
            m_RepApiScopes = string.IsNullOrWhiteSpace(apiScopes) ? "*" : apiScopes;
        }

        /// <summary>
        /// PreparePubSocket
        /// </summary>
        /// <param name="addr"></param>
        /// <returns>Returns void.</returns>
        public void PreparePubSocket(string? addr)
        {
            if (addr == null || addr.Length == 0)
            {
                return;
            }

            m_PublisherSocket.Options.TcpKeepalive = true;
            m_PublisherSocket.Options.SendHighWatermark = 1000;
            m_PublisherSocket.Bind(addr);

            m_PublisherThread.IsBackground = true;
            m_PublisherThread.Start();
        }

        /// <summary>
        /// PrepareSubSocket
        /// </summary>
        /// <param name="addr"></param>
        /// <returns>Returns void.</returns>
        public void PrepareSubSocket(string? addr)
        {
            if (addr == null || addr.Length == 0)
            {
                return;
            }

            m_SubscriberSocket.Options.TcpKeepalive = true;
            m_SubscriberSocket.Options.ReceiveHighWatermark = 1000;
            m_SubscriberSocket.Connect(addr);
            m_SubscriberSocket.Subscribe("");

            m_SubscriberThread.IsBackground = true;
            m_SubscriberThread.Start();
        }

        /// <summary>
        /// PrepareRepSocket
        /// </summary>
        /// <param name="addr"></param>
        /// <returns>Returns void.</returns>
        public void PrepareRepSocket(string? addr)
        {
            if (addr == null || addr.Length == 0)
            {
                return;
            }

            m_ResponseSocket.Bind(addr);

            m_ResponseThread.IsBackground = true;
            m_ResponseThread.Start();
        }

        /// <summary>
        /// PublishMessage
        /// </summary>
        /// <param name="aTopic"></param>
        /// <param name="aMessage"></param>
        /// <returns>Returns void.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void PublishMessage(string aTopic, byte[] aMessage)
        {
            if (!m_EnablePublisherProc || m_PublisherSocket == null)
            {
                return;
            }

            m_PubAPIQueueMessages.Enqueue(new Tuple<string, byte[]>(aTopic, aMessage));
            m_PublisherSignal.Release();
        }

        /// <summary>
        /// PublisherSocketProc
        /// </summary>
        /// <param name="aArg"></param>
        /// <returns>Returns void.</returns>
        private static void PublisherSocketProc(object? aArg)
        {
            while (m_EnablePublisherProc)
            {
                try
                {
                    m_PublisherSignal.WaitOne();
                    m_PubAPIQueueMessages.TryDequeue(out Tuple<string, byte[]>? item);

                    if (item == null)
                    {
                        continue;
                    }

                    m_PublisherSocket.SendMoreFrame(item.Item1).SendFrame(item.Item2);
                }
                catch (Exception e)
                {
                    if (DeployServer.AppDebug)
                    {
                        ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "ZmqImpl Publisher Error:{0}\n{1}", e.Message, e.StackTrace));
                    }
                }
            }
        }

        /// <summary>
        /// SubscriberSocketProc
        /// </summary>
        /// <param name="aArg"></param>
        /// <returns>Returns void.</returns>
        private static void SubscriberSocketProc(object? aArg)
        {
            while (m_EnableSubscriberProc)
            {
                try
                {
                    string message = m_SubscriberSocket.ReceiveFrameString();

                    if (string.IsNullOrEmpty(message))
                    {
                        continue;
                    }

                    dynamic? json = JsonConvert.DeserializeObject<dynamic>(message);

                    if (json == null)
                    {
                        continue;
                    }

                    string apiName = Convert.ToString(json["APIName"]);
                    string apiKey = Convert.ToString(json["APIKey"]);
                    bool asyncTask = Convert.ToBoolean(json["Async"]);

                    if (!ServiceRegistry.TryGetSubEntry(apiName, out ServiceRegistry.SubEntry? entry) || entry == null)
                    {
                        continue;
                    }

                    if (!ApiKeyValidator.Validate(m_SubscriberApiKey, apiKey))
                    {
                        AuditServer.Log(apiName, Convert.ToString(json["Command"]), false, "Invalid API key", "sub");
                        continue;
                    }

                    if (!ApiScopeValidator.IsAllowed(m_SubApiScopes, apiName, "*"))
                    {
                        AuditServer.Log(apiName, Convert.ToString(json["Command"]), false, "Scope denied", "sub");
                        continue;
                    }

                    string responseMessage = ServiceRegistry.InvokeSub(apiName, json, asyncTask);

                    AuditServer.Log(apiName, Convert.ToString(json["Command"]), true, "SUB handled", "sub");

                    if (DeployServer.AppDebug)
                    {
                        ZNLogger.Common.Info(string.Format(CultureInfo.InvariantCulture, "ZmqImpl SUB handled: {0} -> {1}", apiName, responseMessage));
                    }
                }
                catch (Exception e)
                {
                    if (DeployServer.AppDebug)
                    {
                        ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "ZmqImpl Subscriber Error:{0}\n{1}", e.Message, e.StackTrace));
                    }
                }
            }
        }

        /// <summary>
        /// ResponseSocketProc
        /// </summary>
        /// <param name="aArg"></param>
        /// <returns>Returns void.</returns>
        private static void ResponseSocketProc(object? aArg)
        {
            while (m_EnableResponseProc)
            {
                try
                {
                    string message = m_ResponseSocket.ReceiveFrameString();

                    if (string.IsNullOrEmpty(message))
                    {
                        m_ResponseSocket.SendFrameEmpty();
                        continue;
                    }

                    dynamic? json = JsonConvert.DeserializeObject<dynamic>(message);

                    if (json == null)
                    {
                        m_ResponseSocket.SendFrameEmpty();
                        continue;
                    }

                    string apiName = Convert.ToString(json["APIName"]);
                    string apiKey = Convert.ToString(json["APIKey"]);
                    string command = Convert.ToString(json["Command"]);
                    bool asyncTask = Convert.ToBoolean(json["Async"]);

                    if (!ServiceRegistry.TryGetRepEntry(apiName, out ServiceRegistry.RepEntry? entry) || entry == null)
                    {
                        m_ResponseSocket.SendFrameEmpty();
                        continue;
                    }

                    if (!ApiKeyValidator.Validate(m_ResponsetApiKey, apiKey))
                    {
                        ZNLogger.Common.Warn(string.Format(CultureInfo.InvariantCulture, "ZmqImpl rejected API request (key): {0}", apiName));
                        AuditServer.Log(apiName, command, false, "Invalid API key", "rep");
                        m_ResponseSocket.SendFrameEmpty();
                        continue;
                    }

                    if (!ApiScopeValidator.IsAllowed(m_RepApiScopes, apiName, entry.Attribute.ApiScope))
                    {
                        ZNLogger.Common.Warn(string.Format(CultureInfo.InvariantCulture, "ZmqImpl rejected API request (scope): {0}", apiName));
                        AuditServer.Log(apiName, command, false, "Scope denied", "rep");
                        m_ResponseSocket.SendFrameEmpty();
                        continue;
                    }

                    ZNLogger.Common.Info(string.Format(CultureInfo.InvariantCulture, "ZmqImpl API request: {0}", apiName));

                    string responseMessage = ServiceRegistry.InvokeRep(apiName, json, asyncTask);
                    bool success = responseMessage.Contains("\"success\":true", StringComparison.OrdinalIgnoreCase)
                        || responseMessage.Contains("\"success\": true", StringComparison.OrdinalIgnoreCase);

                    AuditServer.Log(apiName, command, success, success ? "OK" : responseMessage, "rep");

                    m_ResponseSocket.SendFrame(responseMessage);

                    if (entry.Attribute.ZmqNotifySubscriber || Convert.ToBoolean(json["NotifySubscriber"]))
                    {
                        IServices? serviceInstance = Activator.CreateInstance(entry.ServiceType) as IServices;
                        serviceInstance?.OnNotifySubscriber(json, responseMessage);
                    }
                }
                catch (Exception e)
                {
                    m_ResponseSocket.SendFrameEmpty();

                    if (DeployServer.AppDebug)
                    {
                        ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "ZmqImpl Response Error:{0}\n{1}", e.Message, e.StackTrace));
                    }
                }
            }
        }
    }
}
