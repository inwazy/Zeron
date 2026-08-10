// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using System.Collections.Specialized;
using System.Globalization;
using System.Reflection;
using System.Text;
using Zeron.Demand.ZServers.Impls;
using Zeron.ZCore;
using Zeron.ZCore.Container;
using Zeron.ZCore.Foundation;
using Zeron.ZInterfaces;

namespace Zeron.Demand.ZServers
{
    /// <summary>
    /// ZmqServer
    /// </summary>
    public class ZmqServer : ConfigurationTable, IServer
    {
        // ZmqImpl instance.
        private readonly ZmqImpl m_ZmqImpl = new();

        /// <summary>
        /// PubSocketEnabled
        /// </summary>
        public static bool PubSocketEnabled
        {
            get;
            set;
        }

        /// <summary>
        /// PubSocketAddr
        /// </summary>
        public static string? PubSocketAddr
        {
            get;
            set;
        }

        /// <summary>
        /// SubSocketEnabled
        /// </summary>
        public static bool SubSocketEnabled
        {
            get;
            set;
        }

        /// <summary>
        /// SubSocketAddr
        /// </summary>
        public static string? SubSocketAddr
        {
            get;
            set;
        }

        /// <summary>
        /// RepSocketEnabled
        /// </summary>
        public static bool RepSocketEnabled
        {
            get;
            set;
        }

        /// <summary>
        /// RepSocketAddr
        /// </summary>
        public static string? RepSocketAddr
        {
            get;
            set;
        }

        /// <summary>
        /// SubApiKey
        /// </summary>
        public static string? SubApiKey
        {
            get;
            set;
        }

        /// <summary>
        /// RepApiKey
        /// </summary>
        public static string? RepApiKey
        {
            get;
            set;
        }

        /// <summary>
        /// RepApiScopes
        /// </summary>
        public static string? RepApiScopes
        {
            get;
            set;
        }

        /// <summary>
        /// SubApiScopes
        /// </summary>
        public static string? SubApiScopes
        {
            get;
            set;
        }

        /// <summary>
        /// SubCurveEnabled
        /// </summary>
        public static bool SubCurveEnabled
        {
            get;
            set;
        }

        /// <summary>
        /// SubCurveServerPublicKeyFile
        /// </summary>
        public static string? SubCurveServerPublicKeyFile
        {
            get;
            set;
        }

        /// <summary>
        /// SubCurveClientSecretFile
        /// </summary>
        public static string? SubCurveClientSecretFile
        {
            get;
            set;
        }

        /// <summary>
        /// LoadConfig
        /// </summary>
        /// <param name="aConfig"></param>
        /// <returns>Returns void.</returns>
        public override void LoadConfig(
            NameValueCollection aConfig)
        {
            if (aConfig == null)
            {
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "ZmqServer Config Empty"));

                return;
            }
            
            try
            {
                PubSocketEnabled = bool.Parse(aConfig["zmq_pub_enabled"] ?? "false");
                PubSocketAddr = aConfig["zmq_pub_addr"];

                SubSocketEnabled = bool.Parse(aConfig["zmq_sub_enabled"] ?? "false");
                SubSocketAddr = aConfig["zmq_sub_addr"];
                SubApiKey = aConfig["zmq_sub_api_key"];
                SubCurveEnabled = bool.Parse(aConfig["zmq_sub_curve_enabled"] ?? "false");
                SubCurveServerPublicKeyFile = aConfig["zmq_sub_curve_server_public_key_file"];
                SubCurveClientSecretFile = aConfig["zmq_sub_curve_client_secret_file"] ?? "Resource/curve-client.secret";

                RepSocketEnabled = bool.Parse(aConfig["zmq_rep_enabled"] ?? "false");
                RepSocketAddr = aConfig["zmq_rep_addr"];
                RepApiKey = aConfig["zmq_rep_api_key"];
                RepApiScopes = aConfig["zmq_rep_api_scopes"] ?? "*";
                SubApiScopes = aConfig["zmq_sub_api_scopes"] ?? "*";
            }
            catch (Exception e)
            {
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "ZmqServer Config Error:{0}\n{1}", e.Message, e.StackTrace));
            }
        }

        /// <summary>
        /// Initialize
        /// </summary>
        /// <returns>Returns void.</returns>
        public void Initialize()
        {
            try
            {
                m_ZmqImpl.ActivateAsCurrent();
                ZmqImpl.PrepareServices(Assembly.GetExecutingAssembly());

                if (PubSocketEnabled)
                {
                    m_ZmqImpl.PreparePubSocket(PubSocketAddr);
                    InstallEventPublisher.PublishHandler = (topic, message) =>
                    {
                        ZmqImpl.PublishMessage(topic, Encoding.UTF8.GetBytes(message));
                        ReporterServer.ForwardEvent(topic, message);
                    };
                }
                else if (ReporterServer.Enabled)
                {
                    InstallEventPublisher.PublishHandler = ReporterServer.ForwardEvent;
                }

                if (SubSocketEnabled)
                {
                    m_ZmqImpl.PrepareSubAPI(SubApiKey, SubApiScopes);
                    m_ZmqImpl.PrepareSubSocket(
                        SubSocketAddr,
                        SubCurveEnabled,
                        SubCurveServerPublicKeyFile,
                        SubCurveClientSecretFile);
                }

                if (RepSocketEnabled)
                {
                    m_ZmqImpl.PrepareRepAPI(RepApiKey, RepApiScopes);
                    m_ZmqImpl.PrepareRepSocket(RepSocketAddr);
                }
            }
            catch (Exception e)
            {
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "ZmqServer Error:{0}\n{1}", e.Message, e.StackTrace));
            }
        }

        /// <summary>
        /// Stop
        /// </summary>
        /// <returns>Returns void.</returns>
        public void Stop()
        {
            try
            {
                InstallEventPublisher.PublishHandler = null;
                m_ZmqImpl.Dispose();
            }
            catch (Exception e)
            {
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "ZmqServer Error:{0}\n{1}", e.Message, e.StackTrace));
            }

            ServerIntegrate.FinishSingleStop();
        }
    }
}
