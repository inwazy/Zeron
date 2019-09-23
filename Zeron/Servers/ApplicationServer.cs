using NLog.Internal;
using System;
using Zeron.Core;
using Zeron.Core.Base;
using Zeron.Core.Container;
using Zeron.Interfaces;

namespace Zeron.Servers
{
    /// <summary>
    /// ApplicationServer
    /// </summary>
    public class ApplicationServer : ConfigurationTable, IServer
    {
        /// <summary>
        /// ApiKey
        /// </summary>
        public static string ApiKey
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
                ApiKey = aConfig.AppSettings["app_api_key"];
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

            }
            catch (Exception e)
            {
                ZNLogger.Common.Error(string.Format("ApplicationServer Error:{0}\n{1}", e.Message, e.StackTrace));
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
