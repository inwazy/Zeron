using NLog.Internal;
using System;
using Zeron.Core;
using Zeron.Core.Base;
using Zeron.Core.Container;
using Zeron.Interfaces;

namespace Zeron.Servers
{
    /// <summary>
    /// DeployServer
    /// </summary>
    public class DeployServer : ConfigurationTable, IServer
    {
        // AppTitle
        public static string AppTitle
        {
            get;
            set;
        }

        // AppTitle
        public static bool AppDebug
        {
            get;
            set;
        }

        // ServiceName
        public static string ServiceName
        {
            get;
            set;
        }

        // ServiceDescription
        public static string ServiceDescription
        {
            get;
            set;
        }

        // ServiceDisplayName
        public static string ServiceDisplayName
        {
            get;
            set;
        }

        // ServiceInstanceName
        public static string ServiceInstanceName
        {
            get;
            set;
        }

        // IsUserConsoleEnv
        public static bool IsConsoleEnv
        {
            get;
            set;
        }

        // Is64BitEnv
        public static bool Is64BitEnv
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
                AppTitle = aConfig.AppSettings["deploy_app_title"];
                AppDebug = bool.Parse(aConfig.AppSettings["deploy_app_debug"]);

                ServiceName = aConfig.AppSettings["deploy_srvice_name"];
                ServiceDescription = aConfig.AppSettings["deploy_service_description"];
                ServiceDisplayName = aConfig.AppSettings["deploy_service_displayname"];
                ServiceInstanceName = aConfig.AppSettings["deploy_service_instancename"];
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
            if (Environment.Is64BitProcess)
            {
                Is64BitEnv = true;
            }

            try
            {

            }
            catch (Exception e)
            {
                ZNLogger.Common.Error(string.Format("DeployServer Error:{0}\n{1}", e.Message, e.StackTrace));
            }
        }

        /// <summary>
        /// Stop
        /// </summary>
        /// <returns>Returns void.</returns>
        public void Stop()
        {
            ServerIntegrate.FinishSingleStop();
        }
    }
}
