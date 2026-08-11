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
        // Settings
        private readonly ServerSettings m_Settings;

        // PublisherSocket
        private PublisherSocket? m_PublisherSocket;

        // IsBound
        private bool m_IsBound;

        /// <summary>
        /// CommandPublisherServer
        /// </summary>
        /// <param name="settings"></param>
        /// <returns>Returns void.</returns>
        public CommandPublisherServer(
            ServerSettings settings)
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

            if (m_Settings.CurveEnabled)
            {
                NetMQCertificate certificate = CurveKeyServer.LoadOrCreate(
                    m_Settings.CurveSecretKeyPath,
                    m_Settings.CurvePublicKeyPath);
                CurveKeyServer.ApplyCurveServer(m_PublisherSocket.Options, certificate);

                ZNLogger.Common.Info(string.Format(CultureInfo.InvariantCulture,
                    "CommandPublisherServer CURVE enabled. Public key: {0}",
                    Path.GetFullPath(m_Settings.CurvePublicKeyPath)));
            }

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
        public bool PublishRemoteCommand(
            string agentKey, 
            Guid assignmentId, 
            string targetApi, 
            string command)
        {
            if (m_PublisherSocket == null)
            {
                return false;
            }

            try
            {
                string primaryApiKey = AgentApiKeyServer.GetPrimaryKey(m_Settings.AgentApiKey);

                var payload = new
                {
                    APIName = "RemoteCommand",
                    APIKey = EncryptionProvider.Encrypt(primaryApiKey),
                    TargetApi = targetApi,
                    Command = command,
                    AssignmentId = assignmentId.ToString(),
                    AgentKey = agentKey
                };

                string json = JsonSerializer.Serialize(payload);
                string topic = "remotecommand." + agentKey;
                m_PublisherSocket.SendMoreFrame(topic).SendFrame(Encoding.UTF8.GetBytes(json));

                ZeronEventBus.PublishObject(
                    ZeronEventTopics.TaskDispatched,
                    new
                    {
                        agentKey,
                        assignmentId,
                        targetApi,
                        command
                    },
                    source: "server",
                    correlationId: assignmentId == Guid.Empty ? null : assignmentId.ToString());

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
