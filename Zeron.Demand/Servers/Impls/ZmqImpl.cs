using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using System.Threading;
using Zeron.Core;
using Zeron.Core.Utils;
using Zeron.Interfaces;

namespace Zeron.Demand.Servers.Impls
{
    /// <summary>
    /// ConfigImpl
    /// </summary>
    internal class ZmqImpl : IImpl
    {
        // Publisher background threading.
        private static readonly Thread m_PublisherThread = new Thread(PublisherSocketProc);

        // Subscriber background threading.
        private static readonly Thread m_SubscriberThread = new Thread(SubscriberSocketProc);

        // Response background threading.
        private static readonly Thread m_ResponseThread = new Thread(ResponseSocketProc);

        // NetNQ PublisherSocket instance.
        private static readonly PublisherSocket m_PublisherSocket = new PublisherSocket();

        // NetNQ SubscriberSocket instance.
        private static readonly SubscriberSocket m_SubscriberSocket = new SubscriberSocket();

        // NetNQ ResponseSocket instance.
        private static readonly ResponseSocket m_ResponseSocket = new ResponseSocket();

        // Signal Publisher
        private static readonly Semaphore m_PublisherSignal = new Semaphore(0, 20000);

        // ConcurrentDictionary Response APIs
        private static readonly ConcurrentDictionary<string, ServicesRepAttribute> m_RepAPIResponse = new ConcurrentDictionary<string, ServicesRepAttribute>();

        // ConcurrentDictionary Response APIs Type
        private static readonly ConcurrentDictionary<string, Type> m_RepAPITypeResponse = new ConcurrentDictionary<string, Type>();

        // Enable Publisher trigger.
        private static bool m_EnablePublisherProc = false;

        // Enable Subscriber trigger.
        private static bool m_EnableSubscriberProc = false;

        // Enable Response trigger.
        private static bool m_EnableResponseProc = false;

        // Client Response Api key.
        private static string m_ResponsetApiKey = "";
        
        /// <summary>
        /// Dispose
        /// </summary>
        /// <returns>Returns void.</returns>
        public void Dispose()
        {
            m_EnablePublisherProc = false;
            m_EnableSubscriberProc = false;
            m_EnableResponseProc = false;

            m_PublisherThread.Abort();
            m_SubscriberThread.Abort();
            m_ResponseThread.Abort();

            m_PublisherSocket.Dispose();
            m_SubscriberSocket.Dispose();
            m_ResponseSocket.Dispose();

            m_PublisherSignal.Dispose();

            m_RepAPIResponse.Clear();
        }

        /// <summary>
        /// PrepareRepAPI
        /// </summary>
        /// <param name="apiKey"></param>
        /// <returns>Returns void.</returns>
        public void PrepareRepAPI(string apiKey)
        {
            foreach (Type assemblyType in Assembly.GetExecutingAssembly().GetTypes())
            {
                if (assemblyType.GetCustomAttributes(typeof(ServicesRepAttribute), true).Length > 0)
                {
                    ServicesRepAttribute repAttribute = assemblyType.GetCustomAttribute(typeof(ServicesRepAttribute)) as ServicesRepAttribute;
                    string apiName = repAttribute.ZmqApiName;

                    if (repAttribute.ZmqApiEnabled == false)
                        continue;

                    if (apiName == null || apiName == "")
                        apiName = assemblyType.Name;

                    m_RepAPIResponse.TryAdd(apiName, repAttribute);
                    m_RepAPITypeResponse.TryAdd(apiName, assemblyType);
                }
            }

            m_ResponsetApiKey = apiKey;
        }

        /// <summary>
        /// PreparePubSocket
        /// </summary>
        /// <param name="addr"></param>
        /// <returns>Returns void.</returns>
        public void PreparePubSocket(string addr)
        {
            if (addr.Length == 0)
                return;

            m_PublisherSocket.Options.TcpKeepalive = true;
            m_PublisherSocket.Options.SendHighWatermark = 1000;
            m_PublisherSocket.Bind(addr);

            m_EnablePublisherProc = true;

            m_PublisherThread.IsBackground = true;
            m_PublisherThread.Start();
        }

        /// <summary>
        /// PrepareSubSocket
        /// </summary>
        /// <param name="addr"></param>
        /// <returns>Returns void.</returns>
        public void PrepareSubSocket(string addr)
        {
            if (addr.Length == 0)
                return;

            m_SubscriberSocket.Options.TcpKeepalive = true;
            m_SubscriberSocket.Options.ReceiveHighWatermark = 1000;
            m_SubscriberSocket.Connect(addr);
            m_SubscriberSocket.Subscribe("");

            m_EnableSubscriberProc = true;

            m_SubscriberThread.IsBackground = true;
            m_SubscriberThread.Start();
        }

        /// <summary>
        /// PrepareRepSocket
        /// </summary>
        /// <param name="addr"></param>
        /// <returns>Returns void.</returns>
        public void PrepareRepSocket(string addr)
        {
            if (addr.Length == 0)
                return;

            m_ResponseSocket.Bind(addr);

            m_EnableResponseProc = true;

            m_ResponseThread.IsBackground = true;
            m_ResponseThread.Start();
        }

        /// <summary>
        /// PublisherSocketProc
        /// </summary>
        /// <param name="aArg"></param>
        /// <returns>Returns void.</returns>
        private static void PublisherSocketProc(object aArg)
        {
            try
            {
                while (m_EnablePublisherProc)
                {
                    m_PublisherSignal.WaitOne();

                }
            }
            catch (Exception e)
            {
                ZNLogger.Common.Error(string.Format("ZmqImpl Error:{0}\n{1}", e.Message, e.StackTrace));
            }
        }

        /// <summary>
        /// SubscriberSocketProc
        /// </summary>
        /// <param name="aArg"></param>
        /// <returns>Returns void.</returns>
        private static void SubscriberSocketProc(object aArg)
        {
            try
            {
                while (m_EnableSubscriberProc)
                {
                    string message = m_SubscriberSocket.ReceiveFrameString();

                    if (message == null || message == "")
                        continue;

                    using (JsonReader reader = new JsonTextReader(new StringReader(message)))
                    {
                        JsonSerializer serializer = new JsonSerializer();

                    }
                }
            }
            catch (Exception e)
            {
                ZNLogger.Common.Error(string.Format("ZmqImpl Error:{0}\n{1}", e.Message, e.StackTrace));
            }
        }

        /// <summary>
        /// ResponseSocketProc
        /// </summary>
        /// <param name="aArg"></param>
        /// <returns>Returns void.</returns>
        private static void ResponseSocketProc(object aArg)
        {
            string message;
            bool received;

            try
            {
                while (m_EnableResponseProc)
                {
                    received = m_ResponseSocket.TryReceiveFrameString(TimeSpan.Zero, out message);

                    if (received != true)
                        continue;

                    if (message == null || message == "")
                    {
                        m_ResponseSocket.SendFrameEmpty();

                        continue;
                    }

                    dynamic json = JsonConvert.DeserializeObject<dynamic>(message);
                    string apiName = (string) json["APIName"];
                    string apiKey = (string) json["APIKey"];

                    m_RepAPIResponse.TryGetValue(apiName, out ServicesRepAttribute serviceAttribute);
                    m_RepAPITypeResponse.TryGetValue(apiName, out Type serviceType);

                    if (serviceAttribute == null || serviceType == null)
                        continue;

                    if (!m_ResponsetApiKey.Contains(Encryption.Decrypt(apiKey)))
                        continue;

                    IServices serviceInstance = Activator.CreateInstance(serviceType) as IServices;
                    string requestMessage = serviceInstance.OnRequest(json);

                    m_ResponseSocket.SendFrame(requestMessage);
                }
            }
            catch (Exception e)
            {
                ZNLogger.Common.Error(string.Format("ZmqImpl Error:{0}\n{1}", e.Message, e.StackTrace));
            }
        }
    }
}
