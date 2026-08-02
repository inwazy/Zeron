// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using System.Collections.Specialized;
using System.Globalization;
using Zeron.ZCore;
using Zeron.ZCore.Container;
using Zeron.ZCore.Foundation;
using Zeron.ZCore.Utils;
using Zeron.ZInterfaces;

namespace Zeron.ZServers
{
    /// <summary>
    /// AgentServer - manages persistent agent identity and uptime.
    /// </summary>
    public class AgentServer : ConfigurationTable, IServer
    {
        private static readonly DateTime s_StartedAtUtc = DateTime.UtcNow;

        /// <summary>
        /// AgentId
        /// </summary>
        public static string? AgentId
        {
            get;
            private set;
        }

        /// <summary>
        /// IdentityFile
        /// </summary>
        public static string? IdentityFile
        {
            get;
            private set;
        }

        /// <summary>
        /// StartedAtUtc
        /// </summary>
        public static DateTime StartedAtUtc => s_StartedAtUtc;

        /// <summary>
        /// UptimeSeconds
        /// </summary>
        public static long UptimeSeconds =>
            (long)(DateTime.UtcNow - s_StartedAtUtc).TotalSeconds;

        /// <summary>
        /// LoadConfig
        /// </summary>
        /// <param name="aConfig"></param>
        /// <returns>Returns void.</returns>
        public override void LoadConfig(
            NameValueCollection aConfig)
        {
            try
            {
                IdentityFile = aConfig["agent_id_file"] ?? "Resource/agent.id";
                AgentId = AgentIdProvider.LoadOrCreate(aConfig["agent_id"], IdentityFile);
            }
            catch (Exception e)
            {
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "AgentServer Config Error:{0}\n{1}", e.Message, e.StackTrace));
            }
        }

        /// <summary>
        /// Initialize
        /// </summary>
        /// <returns>Returns void.</returns>
        public void Initialize()
        {
            ZNLogger.Common.Info(string.Format(CultureInfo.InvariantCulture, "AgentServer initialized. AgentId={0}", AgentId));
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
