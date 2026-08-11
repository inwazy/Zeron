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
    /// ScriptEventBridgeServer - hosts ScriptEventBridge from App.config.
    /// </summary>
    public class ScriptEventBridgeServer : ConfigurationTable, IServer
    {
        // Bridge instance.
        private static ScriptEventBridge? s_Bridge;

        /// <summary>
        /// Bridge - test helper.
        /// </summary>
        public static ScriptEventBridge? Bridge => s_Bridge;

        /// <summary>
        /// Enabled
        /// </summary>
        public static bool Enabled
        {
            get;
            set;
        }

        /// <summary>
        /// ExecutablePath
        /// </summary>
        public static string ExecutablePath
        {
            get;
            set;
        } = "";

        /// <summary>
        /// Arguments
        /// </summary>
        public static string Arguments
        {
            get;
            set;
        } = "";

        /// <summary>
        /// RestartDelayMs
        /// </summary>
        public static int RestartDelayMs
        {
            get;
            set;
        } = 3000;

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
                Enabled = bool.Parse(aConfig["script_event_listener_enabled"] ?? "false");
                ExecutablePath = aConfig["script_event_listener_exe"] ?? "";
                Arguments = aConfig["script_event_listener_args"] ?? "";
                RestartDelayMs = int.TryParse(
                    aConfig["script_event_listener_restart_ms"],
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out int delay)
                    ? delay
                    : 3000;
            }
            catch (Exception e)
            {
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture,
                    "ScriptEventBridgeServer Config Error:{0}\n{1}", e.Message, e.StackTrace));
            }
        }

        /// <summary>
        /// Initialize
        /// </summary>
        /// <returns>Returns void.</returns>
        public void Initialize()
        {
            s_Bridge?.Dispose();
            s_Bridge = new ScriptEventBridge();
            s_Bridge.Configure(Enabled, ExecutablePath, Arguments, RestartDelayMs);
            s_Bridge.Start();

            ZNLogger.Common.Info(string.Format(CultureInfo.InvariantCulture,
                "ScriptEventBridgeServer ready. enabled={0} exe={1}",
                Enabled,
                ExecutablePath));
        }

        /// <summary>
        /// Stop
        /// </summary>
        /// <returns>Returns void.</returns>
        public void Stop()
        {
            s_Bridge?.Dispose();
            s_Bridge = null;
            Zeron.ZCore.Container.ServerIntegrate.FinishSingleStop();
        }
    }
}
