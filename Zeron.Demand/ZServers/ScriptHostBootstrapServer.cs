// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using System.Collections.Specialized;
using System.Globalization;
using Zeron.ZCore;
using Zeron.ZCore.Foundation;
using Zeron.ZCore.Utils;
using Zeron.ZCore.Utils.Engines;
using Zeron.ZInterfaces;

namespace Zeron.Demand.ZServers
{
    /// <summary>
    /// ScriptHostBootstrapServer - loads script host config and registers engines.
    /// </summary>
    public class ScriptHostBootstrapServer : ConfigurationTable, IServer
    {
        // Last loaded appSettings (for external engine keys).
        private static NameValueCollection? s_Config;

        /// <summary>
        /// PowerShellEnabled
        /// </summary>
        public static bool PowerShellEnabled
        {
            get;
            set;
        } = true;

        /// <summary>
        /// PowerShellExe
        /// </summary>
        public static string PowerShellExe
        {
            get;
            set;
        } = "powershell.exe";

        /// <summary>
        /// DefaultTimeoutMs
        /// </summary>
        public static int DefaultTimeoutMs
        {
            get;
            set;
        } = ScriptHostServer.DefaultTimeoutMs;

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

            s_Config = aConfig;

            try
            {
                PowerShellEnabled = bool.Parse(aConfig["script_powershell_enabled"] ?? "true");
                PowerShellExe = aConfig["script_powershell_exe"] ?? "powershell.exe";
                DefaultTimeoutMs = int.TryParse(
                    aConfig["script_default_timeout_ms"],
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out int timeout)
                    ? timeout
                    : ScriptHostServer.DefaultTimeoutMs;
            }
            catch (Exception e)
            {
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture,
                    "ScriptHostBootstrapServer Config Error:{0}\n{1}", e.Message, e.StackTrace));
            }
        }

        /// <summary>
        /// Initialize
        /// </summary>
        /// <returns>Returns void.</returns>
        public void Initialize()
        {
            ScriptHostServer.Clear();
            ScriptHostServer.ConfigureDefaultTimeoutMs(DefaultTimeoutMs);
            ScriptHostServer.Register(new PowerShellScriptEngine(PowerShellExe, PowerShellEnabled));

            foreach (ExternalProcessScriptEngine engine in ExternalScriptEngineConfig.CreateEngines(s_Config))
            {
                ScriptHostServer.Register(engine);
            }

            ZNLogger.Common.Info(string.Format(CultureInfo.InvariantCulture,
                "ScriptHostBootstrapServer ready. Engines={0}",
                string.Join(",", ScriptHostServer.ListEngines().Select(engine => engine.Id + (engine.Available ? "+" : "-")))));
        }

        /// <summary>
        /// Stop
        /// </summary>
        /// <returns>Returns void.</returns>
        public void Stop()
        {
            Zeron.ZCore.Container.ServerIntegrate.FinishSingleStop();
        }
    }
}
