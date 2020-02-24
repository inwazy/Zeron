using NLog.Internal;
using System;
using System.Globalization;
using Zeron.Core;
using Zeron.Core.Base;
using Zeron.Core.Container;
using Zeron.Interfaces;

namespace Zeron.Servers
{
    /// <summary>
    /// MailerServer
    /// </summary>
    public class MailerServer : ConfigurationTable, IServer
    {
        /// <summary>
        /// LoadConfig
        /// </summary>
        /// <param name="aConfig"></param>
        /// <returns>Returns void.</returns>
        public override void LoadConfig(ConfigurationManager aConfig)
        {
            try
            {

            }
            catch (Exception e)
            {
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "Config Error:{0}\n{1}", e.Message, e.StackTrace));
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
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "MailerServer Error:{0}\n{1}", e.Message, e.StackTrace));
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
