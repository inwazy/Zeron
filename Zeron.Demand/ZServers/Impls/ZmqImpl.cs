// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using Zeron.ZAttribute;
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
        // Publisher background threading.
        private static readonly Thread m_PublisherThread = new(PublisherSocketProc);

        // Subscriber background threading.
        private static readonly Thread m_SubscriberThread = new(SubscriberSocketProc);

        // Response background threading.
        private static readonly Thread m_ResponseThread = new(ResponseSocketProc);

        // NetNQ PublisherSocket instance.
        private static readonly PublisherSocket m_PublisherSocket = new();

        // NetNQ SubscriberSocket instance.
        private static readonly SubscriberSocket m_SubscriberSocket = new();

        // NetNQ ResponseSocket instance.
        private static readonly ResponseSocket m_ResponseSocket = new();

        // Signal Publisher
        private static readonly Semaphore m_PublisherSignal = new(0, 20000);

        // Queue Publisher Message
        private static readonly ConcurrentQueue<Tuple<string, byte[]>> m_PubAPIQueueMessages = new();

        // ConcurrentDictionary Subscriber APIs
        private static readonly ConcurrentDictionary<string, ServicesSubAttribute> m_SubAPIResponse = new();

        // ConcurrentDictionary Subscriber APIs Type
        private static readonly ConcurrentDictionary<string, Type> m_SubAPITypeResponse = new();

        // ConcurrentDictionary Response APIs
        private static readonly ConcurrentDictionary<string, ServicesRepAttribute> m_RepAPIResponse = new();

        // ConcurrentDictionary Response APIs Type
        private static readonly ConcurrentDictionary<string, Type> m_RepAPITypeResponse = new();

        // Enable Publisher trigger.
        private static bool m_EnablePublisherProc = true;

        // Enable Subscriber trigger.
        private static bool m_EnableSubscriberProc = true;

        // Enable Response trigger.
        private static bool m_EnableResponseProc = true;

        // Client Subscriber Api key.
        private static string m_SubscriberApiKey = "";

        // Client Response Api key.
        private static string m_ResponsetApiKey = "";

        /// <summary>
        /// Dispose
        /// </summary>
        /// <returns>Returns void.</returns>
        public void Dispose()
        {
            m_PublisherSocket.Dispose();
            m_SubscriberSocket.Dispose();
            m_ResponseSocket.Dispose();

            m_PublisherSignal.Dispose();

            m_SubAPIResponse.Clear();
            m_SubAPITypeResponse.Clear();
            m_RepAPIResponse.Clear();
            m_RepAPITypeResponse.Clear();
        }

        /// <summary>
        /// PrepareSubAPI
        /// </summary>
        /// <param name="apiKey"></param>
        /// <returns>Returns void.</returns>
        public void PrepareSubAPI(string? apiKey)
        {
            foreach (Type assemblyType in Assembly.GetExecutingAssembly().GetTypes())
            {
                if (assemblyType.GetCustomAttributes(typeof(ServicesSubAttribute), true).Length > 0)
                {
                    ServicesSubAttribute? repAttribute = assemblyType.GetCustomAttribute(typeof(ServicesSubAttribute)) as ServicesSubAttribute;

                    if (repAttribute == null)
                    {
                        continue;
                    }

                    string? apiName = repAttribute.ZmqApiName;

                    if (repAttribute.ZmqApiEnabled == false)
                    {
                        continue;
                    }

                    if (apiName == null || string.IsNullOrEmpty(apiName))
                    {
                        apiName = assemblyType.Name;
                    }

                    m_SubAPIResponse.TryAdd(apiName, repAttribute);
                    m_SubAPITypeResponse.TryAdd(apiName, assemblyType);
                }
            }

            m_SubscriberApiKey = apiKey ?? "";
        }

        /// <summary>
        /// PrepareRepAPI
        /// </summary>
        /// <param name="apiKey"></param>
        /// <returns>Returns void.</returns>
        public void PrepareRepAPI(string? apiKey)
        {
            foreach (Type assemblyType in Assembly.GetExecutingAssembly().GetTypes())
            {
                if (assemblyType.GetCustomAttributes(typeof(ServicesRepAttribute), true).Length > 0)
                {
                    ServicesRepAttribute? repAttribute = assemblyType.GetCustomAttribute(typeof(ServicesRepAttribute)) as ServicesRepAttribute;
                    
                    if (repAttribute == null)
                    {
                        continue;
                    }

                    string? apiName = repAttribute.ZmqApiName;

                    if (repAttribute.ZmqApiEnabled == false)
                    {
                        continue;
                    }

                    if (apiName == null || string.IsNullOrEmpty(apiName))
                    {
                        apiName = assemblyType.Name;
                    }

                    m_RepAPIResponse.TryAdd(apiName, repAttribute);
                    m_RepAPITypeResponse.TryAdd(apiName, assemblyType);
                }
            }

            m_ResponsetApiKey = apiKey ?? "";
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
            if (m_EnablePublisherProc)
            {
                if (m_PublisherSocket != null)
                {
                    m_PubAPIQueueMessages.Enqueue(new Tuple<string, byte[]>(aTopic, aMessage));
                    m_PublisherSignal.Release();
                }
            }
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

                    string topic = item.Item1;
                    byte[] message = item.Item2;

                    if (m_PublisherSocket != null)
                    {
                        m_PublisherSocket.SendMoreFrame("").SendFrame(message);
                    }
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
            string message;

            while (m_EnableSubscriberProc)
            {
                try
                {
                    message = m_SubscriberSocket.ReceiveFrameString();

                    if (message == null || string.IsNullOrEmpty(message))
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

                    m_SubAPIResponse.TryGetValue(apiName, out ServicesSubAttribute? serviceAttribute);
                    m_SubAPITypeResponse.TryGetValue(apiName, out Type? serviceType);

                    if (serviceAttribute == null || serviceType == null)
                    {
                        continue;
                    }

                    if (!m_SubscriberApiKey.Contains(EncryptionProvider.Decrypt(apiKey)))
                    {
                        continue;
                    }

                    IServices? serviceInstance = Activator.CreateInstance(serviceType) as IServices;

                    string responseMessage = "";

                    if (serviceInstance != null)
                    {
                        if (asyncTask)
                        {
                            responseMessage = serviceInstance.OnSubscriberAsync(json);
                        }
                        else
                        {
                            responseMessage = serviceInstance.OnSubscriber(json);
                        }
                    }

                    Thread.Sleep(300);
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
            string message;

            while (m_EnableResponseProc)
            {
                try
                {
                    message = m_ResponseSocket.ReceiveFrameString();

                    if (message == null || string.IsNullOrEmpty(message))
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
                    bool asyncTask = Convert.ToBoolean(json["Async"]);

                    m_RepAPIResponse.TryGetValue(apiName, out ServicesRepAttribute? serviceAttribute);
                    m_RepAPITypeResponse.TryGetValue(apiName, out Type? serviceType);

                    if (serviceAttribute == null || serviceType == null)
                    {
                        m_ResponseSocket.SendFrameEmpty();

                        continue;
                    }

                    if (!m_ResponsetApiKey.Contains(EncryptionProvider.Decrypt(apiKey)))
                    {
                        m_ResponseSocket.SendFrameEmpty();

                        continue;
                    }

                    IServices? serviceInstance = Activator.CreateInstance(serviceType) as IServices;

                    string responseMessage = "";

                    if (serviceInstance != null)
                    {
                        if (asyncTask)
                        {
                            responseMessage = serviceInstance.OnRequestAsync(json);
                        }
                        else
                        {
                            responseMessage = serviceInstance.OnRequest(json);
                        }
                    }

                    m_ResponseSocket.SendFrame(responseMessage);

                    if (serviceAttribute.ZmqNotifySubscriber)
                    {
                        if (serviceInstance != null)
                        {
                            serviceInstance.OnNotifySubscriber(json, responseMessage);
                        }
                    }
                }
                catch (Exception e)
                {
                    if (DeployServer.AppDebug)
                    {
                        ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "ZmqImpl Response Error:{0}\n{1}", e.Message, e.StackTrace));
                    }
                }
            }
        }
    }
}
