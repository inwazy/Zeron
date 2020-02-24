using System;
using System.Globalization;
using System.IO;
using Zeron.Core;
using Zeron.Core.Container;
using Zeron.Interfaces;
using Zeron.Servers.Impls;

namespace Zeron.Servers
{
    /// <summary>
    /// ConfigServer
    /// </summary>
    public class ConfigServer : IServer
    {
        // Config File Watcher instance.
        private static readonly FileSystemWatcher m_FileWather = new FileSystemWatcher();

        // Config Impls instance.
        private static readonly ConfigImpl m_ConfigImpl = new ConfigImpl();

        /// <summary>
        /// Initialize
        /// </summary>
        /// <returns>Returns void.</returns>
        public void Initialize()
        {
            string configPath = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;

            if (!File.Exists(configPath))
            {
                throw new Exception("ConfigServer: APP configuration file does not exist!");
            }

            try
            {
                m_ConfigImpl.PrepareObjects();

                m_FileWather.Path = Path.GetDirectoryName(configPath);
                m_FileWather.Filter = Path.GetFileName(configPath);
                m_FileWather.EnableRaisingEvents = true;
                m_FileWather.NotifyFilter = NotifyFilters.LastWrite;
                m_FileWather.Changed += m_ConfigImpl.OnChanged;
            }
            catch (Exception e)
            {
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "ConfigServer Error:{0}\n{1}", e.Message, e.StackTrace));
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
                m_FileWather.Changed -= m_ConfigImpl.OnChanged;

                m_ConfigImpl.Dispose();
                m_FileWather.Dispose();
            }
            catch (Exception e)
            {
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "ConfigServer Error:{0}\n{1}", e.Message, e.StackTrace));
            }

            ServerIntegrate.FinishSingleStop();
        }
    }
}
