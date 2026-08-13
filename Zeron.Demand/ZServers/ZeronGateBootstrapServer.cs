// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using System.Collections.Specialized;
using System.Globalization;
using Zeron.ZCore;
using Zeron.ZCore.Foundation;
using Zeron.ZCore.Utils;
using Zeron.ZInterfaces;

namespace Zeron.Demand.ZServers
{
    /// <summary>
    /// ZeronGateBootstrapServer - configures gate timeout and loads agent plugins.
    /// </summary>
    public class ZeronGateBootstrapServer : ConfigurationTable, IServer
    {
        // Loaded plugins.
        private static readonly List<IZeronAgentPlugin> s_Plugins = [];

        /// <summary>
        /// PauseTimeoutMs
        /// </summary>
        public static int PauseTimeoutMs
        {
            get;
            set;
        } = ZeronGateServer.DefaultTimeoutMs;

        /// <summary>
        /// PluginsDirectory
        /// </summary>
        public static string PluginsDirectory
        {
            get;
            set;
        } = "plugins";

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
                return;
            }

            try
            {
                PauseTimeoutMs = int.TryParse(
                    aConfig["gate_pause_timeout_ms"],
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out int timeout)
                    ? timeout
                    : ZeronGateServer.DefaultTimeoutMs;
                PluginsDirectory = string.IsNullOrWhiteSpace(aConfig["script_plugins_dir"])
                    ? "plugins"
                    : aConfig["script_plugins_dir"]!.Trim();
            }
            catch (Exception e)
            {
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture,
                    "ZeronGateBootstrapServer Config Error:{0}\n{1}", e.Message, e.StackTrace));
            }
        }

        /// <summary>
        /// Initialize
        /// </summary>
        /// <returns>Returns void.</returns>
        public void Initialize()
        {
            ZeronGateServer.ConfigureDefaultTimeoutMs(PauseTimeoutMs);

            string directory = Path.IsPathRooted(PluginsDirectory)
                ? PluginsDirectory
                : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, PluginsDirectory);

            StopPlugins();
            s_Plugins.AddRange(ZeronPluginLoader.LoadAgentPlugins(
                directory,
                ZeronEventBus.Current,
                ZeronGateServer.Current));

            ZNLogger.Common.Info(string.Format(CultureInfo.InvariantCulture,
                "ZeronGateBootstrapServer ready. timeoutMs={0} plugins={1} dir={2}",
                PauseTimeoutMs,
                s_Plugins.Count,
                directory));
        }

        /// <summary>
        /// Stop
        /// </summary>
        /// <returns>Returns void.</returns>
        public void Stop()
        {
            StopPlugins();
            Zeron.ZCore.Container.ServerIntegrate.FinishSingleStop();
        }

        /// <summary>
        /// StopPlugins
        /// </summary>
        private static void StopPlugins()
        {
            foreach (IZeronAgentPlugin plugin in s_Plugins)
            {
                try
                {
                    plugin.Stop();
                }
                catch (Exception e)
                {
                    ZNLogger.Common.Warn("ZeronGateBootstrapServer plugin stop: " + e.Message);
                }
            }

            s_Plugins.Clear();
        }
    }
}
