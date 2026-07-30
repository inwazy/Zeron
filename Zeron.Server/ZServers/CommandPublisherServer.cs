// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using NetMQ;
using NetMQ.Sockets;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Zeron.Server.ZCore;
using Zeron.ZCore;
using Zeron.ZCore.Utils;

namespace Zeron.Server.ZServers
{
    /// <summary>
    /// CommandPublisherServer - NetMQ PUB for RemoteCommand dispatch.
    /// </summary>
    public class CommandPublisherServer : IDisposable
    {
        private readonly ServerSettings m_Settings;
        private PublisherSocket? m_PublisherSocket;
        private bool m_IsBound;

        /// <summary>
        /// CommandPublisherServer
        /// </summary>
        /// <param name="settings"></param>
        /// <returns>Returns void.</returns>
        public CommandPublisherServer(ServerSettings settings)
        {
            m_Settings = settings;
        }

        /// <summary>
        /// Start
        /// </summary>
        /// <returns>Returns void.</returns>
        public void Start()
        {
            if (m_IsBound)
            {
                return;
            }

            m_PublisherSocket = new PublisherSocket();
            m_PublisherSocket.Options.TcpKeepalive = true;
            m_PublisherSocket.Bind(m_Settings.CommandPubAddr);
            m_IsBound = true;

            ZNLogger.Common.Info(string.Format(CultureInfo.InvariantCulture,
                "CommandPublisherServer bound to {0}", m_Settings.CommandPubAddr));
        }

        /// <summary>
        /// PublishRemoteCommand
        /// </summary>
        /// <param name="agentKey"></param>
        /// <param name="assignmentId"></param>
        /// <param name="targetApi"></param>
        /// <param name="command"></param>
        /// <returns>Returns bool.</returns>
        public bool PublishRemoteCommand(string agentKey, Guid assignmentId, string targetApi, string command)
        {
            if (m_PublisherSocket == null)
            {
                return false;
            }

            try
            {
                var payload = new
                {
                    APIName = "RemoteCommand",
                    APIKey = EncryptionProvider.Encrypt(m_Settings.AgentApiKey),
                    TargetApi = targetApi,
                    Command = command,
                    AssignmentId = assignmentId.ToString(),
                    AgentKey = agentKey
                };

                string json = JsonSerializer.Serialize(payload);
                string topic = "remotecommand." + agentKey;
                m_PublisherSocket.SendMoreFrame(topic).SendFrame(Encoding.UTF8.GetBytes(json));

                ZNLogger.Common.Info(string.Format(CultureInfo.InvariantCulture,
                    "CommandPublisherServer dispatched {0} to agent {1}", targetApi, agentKey));

                return true;
            }
            catch (Exception e)
            {
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture,
                    "CommandPublisherServer Error:{0}\n{1}", e.Message, e.StackTrace));

                return false;
            }
        }

        /// <summary>
        /// Dispose
        /// </summary>
        /// <returns>Returns void.</returns>
        public void Dispose()
        {
            m_PublisherSocket?.Dispose();
            m_IsBound = false;
        }
    }
}
