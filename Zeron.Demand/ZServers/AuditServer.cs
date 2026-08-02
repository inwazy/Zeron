// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using System.Collections.Specialized;
using System.Globalization;
using Zeron.Demand.ZServers.Impls;
using Zeron.ZCore;
using Zeron.ZCore.Container;
using Zeron.ZCore.Foundation;
using Zeron.ZInterfaces;
using Zeron.ZServers;

namespace Zeron.Demand.ZServers
{
    /// <summary>
    /// AuditServer
    /// </summary>
    internal class AuditServer : ConfigurationTable, IServer
    {
        // Audit database implementation.
        private readonly AuditDbImpl m_AuditDbImpl = new();

        /// <summary>
        /// DbSourceFile
        /// </summary>
        public static string? DbSourceFile
        {
            get;
            private set;
        }

        /// <summary>
        /// Enabled
        /// </summary>
        public static bool Enabled
        {
            get;
            private set;
        } = true;

        /// <summary>
        /// Log
        /// </summary>
        /// <param name="apiName"></param>
        /// <param name="command"></param>
        /// <param name="success"></param>
        /// <param name="message"></param>
        /// <param name="source"></param>
        /// <returns>Returns void.</returns>
        public static void Log(
            string? apiName, 
            string? command, 
            bool success, 
            string? message, 
            string source = "rep")
        {
            if (!Enabled)
            {
                return;
            }

            AuditDbImpl.Insert(AgentServer.AgentId, apiName, command, success, message, source);
        }

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
                Enabled = bool.Parse(aConfig["audit_enabled"] ?? "true");
                DbSourceFile = aConfig["audit_db_source_file"] ?? "Resource/audit.db";
            }
            catch (Exception e)
            {
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "AuditServer Config Error:{0}\n{1}", e.Message, e.StackTrace));
            }
        }

        /// <summary>
        /// Initialize
        /// </summary>
        /// <returns>Returns void.</returns>
        public void Initialize()
        {
            if (!Enabled)
            {
                ZNLogger.Common.Info("AuditServer disabled.");

                return;
            }

            m_AuditDbImpl.PrepareDatabase(DbSourceFile);
            ZNLogger.Common.Info("AuditServer initialized.");
        }

        /// <summary>
        /// Stop
        /// </summary>
        /// <returns>Returns void.</returns>
        public void Stop()
        {
            m_AuditDbImpl.Dispose();
            ServerIntegrate.FinishSingleStop();
        }
    }
}
