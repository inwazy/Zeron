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
    /// ZmqImpl - NetMQ pub/sub/rep runtime (instance state + Current facade).
    /// </summary>
    internal class ZmqImpl : IImpl
    {
        // Active runtime instance.
        public static ZmqImpl? Current
        {
            get;
            private set;
        }

        // Publisher thread.
        private Thread? m_PublisherThread;

        // Subscriber thread.
        private Thread? m_SubscriberThread;

        // Response thread.
        private Thread? m_ResponseThread;

        // Publisher socket.
        private readonly PublisherSocket m_PublisherSocket = new();

        // Subscriber socket.
        private readonly SubscriberSocket m_SubscriberSocket = new();

        // Response socket.
        private readonly ResponseSocket m_ResponseSocket = new();

        // Publisher signal.
        private readonly Semaphore m_PublisherSignal = new(0, 20000);

        // Publisher API queue messages.
        private readonly ConcurrentQueue<Tuple<string, byte[]>> m_PubAPIQueueMessages = new();

        // Enable publisher process.
        private bool m_EnablePublisherProc = true;

        // Enable subscriber process.
        private bool m_EnableSubscriberProc = true;

        // Enable response process.
        private bool m_EnableResponseProc = true;

        // Whether publisher socket was prepared.
        private bool m_PublisherPrepared;

        // Subscriber API key.
        private string m_SubscriberApiKey = "";

        // Response API key.
        private string m_ResponseApiKey = "";

        // Response API scopes.
        private string m_RepApiScopes = "*";

        // Subscriber API scopes.
        private string m_SubApiScopes = "*";

        /// <summary>
        /// ActivateAsCurrent
        /// </summary>
        /// <returns>Returns void.</returns>
        public void ActivateAsCurrent()
        {
            Current = this;
        }

        /// <summary>
        /// Dispose
        /// </summary>
        /// <returns>Returns void.</returns>
        public void Dispose()
        {
            m_EnablePublisherProc = false;
            m_EnableSubscriberProc = false;
            m_EnableResponseProc = false;
            m_PublisherPrepared = false;

            try
            {
                m_PublisherSignal.Release();
            }
            catch (SemaphoreFullException)
            {
            }
            catch (ObjectDisposedException)
            {
            }

            try
            {
                m_PublisherSocket.Dispose();
                m_SubscriberSocket.Dispose();
                m_ResponseSocket.Dispose();
                m_PublisherSignal.Dispose();
            }
            catch (Exception)
            {
            }

            if (ReferenceEquals(Current, this))
            {
                Current = null;
            }
        }

        /// <summary>
        /// PrepareServices
        /// </summary>
        /// <param name="assembly"></param>
        /// <returns>Returns void.</returns>
        public static void PrepareServices(
            Assembly assembly)
        {
            ServiceRegistry.RegisterFromAssembly(assembly);
        }

        /// <summary>
        /// PrepareSubAPI
        /// </summary>
        /// <param name="apiKey"></param>
        /// <param name="apiScopes"></param>
        /// <returns>Returns void.</returns>
        public void PrepareSubAPI(
            string? apiKey, 
            string? apiScopes)
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
        public void PrepareRepAPI(
            string? apiKey, 
            string? apiScopes)
        {
            m_ResponseApiKey = apiKey ?? "";
            m_RepApiScopes = string.IsNullOrWhiteSpace(apiScopes) ? "*" : apiScopes;
        }

        /// <summary>
        /// PreparePubSocket
        /// </summary>
        /// <param name="addr"></param>
        /// <returns>Returns void.</returns>
        public void PreparePubSocket(
            string? addr)
        {
            if (addr == null || addr.Length == 0)
            {
                return;
            }

            m_PublisherSocket.Options.TcpKeepalive = true;
            m_PublisherSocket.Options.SendHighWatermark = 1000;
            m_PublisherSocket.Bind(addr);
            m_PublisherPrepared = true;

            m_PublisherThread = new Thread(PublisherSocketProc)
            {
                IsBackground = true,
                Name = "ZmqImpl.Publisher"
            };
            m_PublisherThread.Start();
        }

        /// <summary>
        /// PrepareSubSocket
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="curveEnabled"></param>
        /// <param name="serverPublicKeyFile"></param>
        /// <param name="clientSecretFile"></param>
        /// <returns>Returns void.</returns>
        public void PrepareSubSocket(
            string? addr,
            bool curveEnabled = false,
            string? serverPublicKeyFile = null,
            string? clientSecretFile = null)
        {
            if (addr == null || addr.Length == 0)
            {
                return;
            }

            m_SubscriberSocket.Options.TcpKeepalive = true;
            m_SubscriberSocket.Options.ReceiveHighWatermark = 1000;

            if (curveEnabled)
            {
                if (string.IsNullOrWhiteSpace(serverPublicKeyFile))
                {
                    throw new InvalidOperationException(
                        "zmq_sub_curve_enabled=true requires zmq_sub_curve_server_public_key_file.");
                }

                string clientSecretPath = string.IsNullOrWhiteSpace(clientSecretFile)
                    ? "Resource/curve-client.secret"
                    : clientSecretFile;
                string clientPublicPath = Path.ChangeExtension(clientSecretPath, ".public");

                NetMQCertificate clientCert = CurveKeyServer.LoadOrCreate(clientSecretPath, clientPublicPath);
                byte[] serverPublicKey = CurveKeyServer.LoadPublicKey(serverPublicKeyFile);
                CurveKeyServer.ApplyCurveClient(m_SubscriberSocket.Options, clientCert, serverPublicKey);

                ZNLogger.Common.Info(string.Format(CultureInfo.InvariantCulture,
                    "ZmqImpl SUB CURVE enabled. Server public key: {0}", Path.GetFullPath(serverPublicKeyFile)));
            }

            m_SubscriberSocket.Connect(addr);
            m_SubscriberSocket.Subscribe("");

            m_SubscriberThread = new Thread(SubscriberSocketProc)
            {
                IsBackground = true,
                Name = "ZmqImpl.Subscriber"
            };
            m_SubscriberThread.Start();
        }

        /// <summary>
        /// PrepareRepSocket
        /// </summary>
        /// <param name="addr"></param>
        /// <returns>Returns void.</returns>
        public void PrepareRepSocket(
            string? addr)
        {
            if (addr == null || addr.Length == 0)
            {
                return;
            }

            m_ResponseSocket.Bind(addr);

            m_ResponseThread = new Thread(ResponseSocketProc)
            {
                IsBackground = true,
                Name = "ZmqImpl.Response"
            };
            m_ResponseThread.Start();
        }

        /// <summary>
        /// PublishMessage
        /// </summary>
        /// <param name="aTopic"></param>
        /// <param name="aMessage"></param>
        /// <returns>Returns void.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void PublishMessage(
            string aTopic, 
            byte[] aMessage)
        {
            Current?.EnqueuePublish(aTopic, aMessage);
        }

        /// <summary>
        /// EnqueuePublish
        /// </summary>
        /// <param name="aTopic"></param>
        /// <param name="aMessage"></param>
        /// <returns>Returns void.</returns>
        private void EnqueuePublish(
            string aTopic,
            byte[] aMessage)
        {
            if (!m_EnablePublisherProc || !m_PublisherPrepared)
            {
                return;
            }

            m_PubAPIQueueMessages.Enqueue(new Tuple<string, byte[]>(aTopic, aMessage));

            try
            {
                m_PublisherSignal.Release();
            }
            catch (SemaphoreFullException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
        }

        /// <summary>
        /// PublisherSocketProc
        /// </summary>
        /// <param name="aArg"></param>
        /// <returns>Returns void.</returns>
        private void PublisherSocketProc(
            object? aArg)
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
        private void SubscriberSocketProc(
            object? aArg)
        {
            while (m_EnableSubscriberProc)
            {
                try
                {
                    // PUB messages are multipart: [topic][json]. Legacy single-frame JSON is also accepted.
                    NetMQMessage mqMessage = m_SubscriberSocket.ReceiveMultipartMessage();
                    string message = ExtractSubscriberPayload(mqMessage);

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
                    bool asyncTask = json["Async"] != null && Convert.ToBoolean(json["Async"]);

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
        /// ExtractSubscriberPayload
        /// </summary>
        /// <param name="mqMessage"></param>
        /// <returns>Returns JSON payload string.</returns>
        private static string ExtractSubscriberPayload(
            NetMQMessage mqMessage)
        {
            if (mqMessage.FrameCount == 0)
            {
                return "";
            }

            if (mqMessage.FrameCount >= 2)
            {
                return mqMessage[1].ConvertToString() ?? "";
            }

            string singleFrame = mqMessage[0].ConvertToString() ?? "";

            // Ignore bare topic frames that are not JSON payloads.
            if (!string.IsNullOrEmpty(singleFrame)
                && singleFrame[0] != '{'
                && singleFrame[0] != '[')
            {
                return "";
            }

            return singleFrame;
        }

        /// <summary>
        /// ResponseSocketProc
        /// </summary>
        /// <param name="aArg"></param>
        /// <returns>Returns void.</returns>
        private void ResponseSocketProc(
            object? aArg)
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

                    if (!ApiKeyValidator.Validate(m_ResponseApiKey, apiKey))
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
                    try
                    {
                        m_ResponseSocket.SendFrameEmpty();
                    }
                    catch (Exception)
                    {
                    }

                    if (DeployServer.AppDebug)
                    {
                        ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "ZmqImpl Response Error:{0}\n{1}", e.Message, e.StackTrace));
                    }
                }
            }
        }
    }
}
