// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using System.Configuration;
using System.Globalization;
using Zeron.ZCore;
using Zeron.ZCore.Container;
using Zeron.ZInterfaces;
using Zeron.ZServers.Impls;

namespace Zeron.ZServers
{
    /// <summary>
    /// ConfigServer
    /// </summary>
    public class ConfigServer : IServer
    {
        // Config File Watcher instance.
        private static readonly FileSystemWatcher m_FileWather = new();

        // Config Impls instance.
        private static readonly ConfigImpl m_ConfigImpl = new();

        /// <summary>
        /// Initialize
        /// </summary>
        /// <returns>Returns void.</returns>
        public void Initialize()
        {
            Configuration configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            string configPath = configFile.FilePath;

            if (configPath == null || configPath == "")
            {
                throw new Exception("ConfigServer: APP configuration file path is empty!");
            }

            if (!File.Exists(configPath))
            {
                throw new Exception("ConfigServer: APP configuration file does not exist!");
            }

            try
            {
                m_ConfigImpl.PrepareObjects();

                m_FileWather.Path = Path.GetDirectoryName(configPath) ?? "";
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
