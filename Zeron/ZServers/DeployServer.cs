// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using System.Collections.Specialized;
using System.Globalization;
using Zeron.ZCore;
using Zeron.ZCore.Container;
using Zeron.ZCore.Foundation;
using Zeron.ZInterfaces;

namespace Zeron.ZServers
{
    /// <summary>
    /// DeployServer
    /// </summary>
    public class DeployServer : ConfigurationTable, IServer
    {
        /// <summary>
        /// AppTitle
        /// </summary>
        public static string? AppTitle
        {
            get;
            set;
        }

        /// <summary>
        /// AppDebug
        /// </summary>
        public static bool AppDebug
        {
            get;
            set;
        }

        /// <summary>
        /// ServiceName
        /// </summary>
        public static string? ServiceName
        {
            get;
            set;
        }

        /// <summary>
        /// ServiceDescription
        /// </summary>
        public static string? ServiceDescription
        {
            get;
            set;
        }

        /// <summary>
        /// ServiceDisplayName
        /// </summary>
        public static string? ServiceDisplayName
        {
            get;
            set;
        }

        /// <summary>
        /// ServiceInstanceName
        /// </summary>
        public static string? ServiceInstanceName
        {
            get;
            set;
        }

        /// <summary>
        /// IsConsoleEnv
        /// </summary>
        public static bool IsConsoleEnv
        {
            get;
            set;
        }

        /// <summary>
        /// Is64BitEnv
        /// </summary>
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
        public override void LoadConfig(NameValueCollection aConfig)
        {
            if (aConfig == null)
            {
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "DeployServer Config Empty"));

                return;
            }

            try
            {
                AppTitle = aConfig["deploy_app_title"];
                AppDebug = bool.Parse(aConfig["deploy_app_debug"] ?? "false");

                ServiceName = aConfig["deploy_srvice_name"];
                ServiceDescription = aConfig["deploy_service_description"];
                ServiceDisplayName = aConfig["deploy_service_displayname"];
                ServiceInstanceName = aConfig["deploy_service_instancename"];
            }
            catch (Exception e)
            {
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "DeployServer Config Error:{0}\n{1}", e.Message, e.StackTrace));
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
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "DeployServer Error:{0}\n{1}", e.Message, e.StackTrace));
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
