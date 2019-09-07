using NLog.Internal;
using System;
using Zeron.Core;
using Zeron.Core.Base;
using Zeron.Core.Container;
using Zeron.Interfaces;
using Zeron.Demand.Servers.Impls;

namespace Zeron.Demand.Servers
{
    /// <summary>
    /// ZmqServer
    /// </summary>
    public class ZmqServer : ConfigurationTable, IServer
    {
        // ZmqImpl instance.
        private readonly ZmqImpl m_ZmqImpl = new ZmqImpl();

        // PubSocketEnabled
        public static bool PubSocketEnabled
        {
            get;
            set;
        }

        // PubSocketAddr
        public static string PubSocketAddr
        {
            get;
            set;
        }

        // SubSocketEnabled
        public static bool SubSocketEnabled
        {
            get;
            set;
        }

        // SubSocketAddr
        public static string SubSocketAddr
        {
            get;
            set;
        }

        // RepSocketEnabled
        public static bool RepSocketEnabled
        {
            get;
            set;
        }

        // RepSocketAddr
        public static string RepSocketAddr
        {
            get;
            set;
        }

        // SubApiKey
        public static string SubApiKey
        {
            get;
            set;
        }

        // RepApiKey
        public static string RepApiKey
        {
            get;
            set;
        }

        /// <summary>
        /// LoadConfig
        /// </summary>
        /// <param name="aConfig"></param>
        /// <returns>Returns void.</returns>
        public override void LoadConfig(ConfigurationManager aConfig)
        {
            try
            {
                PubSocketEnabled = bool.Parse(aConfig.AppSettings["zmq_pub_enabled"]);
                PubSocketAddr = aConfig.AppSettings["zmq_pub_addr"];

                SubSocketEnabled = bool.Parse(aConfig.AppSettings["zmq_sub_enabled"]);
                SubSocketAddr = aConfig.AppSettings["zmq_sub_addr"];
                SubApiKey = aConfig.AppSettings["zmq_sub_api_key"];

                RepSocketEnabled = bool.Parse(aConfig.AppSettings["zmq_rep_enabled"]);
                RepSocketAddr = aConfig.AppSettings["zmq_rep_addr"];
                RepApiKey = aConfig.AppSettings["zmq_rep_api_key"];
            }
            catch (Exception e)
            {
                ZNLogger.Common.Error(string.Format("Config Error:{0}\n{1}", e.Message, e.StackTrace));
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
                if (PubSocketEnabled)
                    m_ZmqImpl.PreparePubSocket(PubSocketAddr);

                if (SubSocketEnabled)
                {
                    m_ZmqImpl.PrepareSubAPI(SubApiKey);
                    m_ZmqImpl.PrepareSubSocket(SubSocketAddr);
                }

                if (RepSocketEnabled)
                {
                    m_ZmqImpl.PrepareRepAPI(RepApiKey);
                    m_ZmqImpl.PrepareRepSocket(RepSocketAddr);
                }
            }
            catch (Exception e)
            {
                ZNLogger.Common.Error(string.Format("ZmqServer Error:{0}\n{1}", e.Message, e.StackTrace));
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
                m_ZmqImpl.Dispose();
            }
            catch (Exception e)
            {
                ZNLogger.Common.Error(string.Format("ZmqServer Error:{0}\n{1}", e.Message, e.StackTrace));
            }

            ServerIntegrate.FinishSingleStop();
        }
    }
}
