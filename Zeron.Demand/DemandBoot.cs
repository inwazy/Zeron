// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using System.Globalization;
using Zeron.Demand.ZCore;
using Zeron.Demand.ZServers;
using Zeron.ZCore;
using Zeron.ZCore.Container;
using Zeron.ZServers;

namespace Zeron.Demand
{
    /// <summary>
    /// Shared Demand agent server registration (used by Zeron.Demand and edition hosts).
    /// </summary>
    public static class DemandBoot
    {
        /// <summary>
        /// Optional hook invoked immediately before <see cref="ZeronGateBootstrapServer"/> is forked
        /// (e.g. register plugin load policy before agent plugins initialize).
        /// </summary>
        public static Action? BeforeGateBootstrap
        {
            get;
            set;
        }

        /// <summary>
        /// Registers standard Zeron.Demand servers via <see cref="ServerIntegrate"/>.
        /// </summary>
        /// <returns>True when registration succeeded.</returns>
        public static bool RegisterStandardServers()
        {
            Environment.CurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;

            try
            {
                SchedulerServer.OnTaskDue = task => TaskPipelineExecutor.ExecuteTask(task);

                ServerIntegrate.Fork<ConfigServer>();
                ServerIntegrate.Fork<AgentServer>();
                ServerIntegrate.Fork<ApplicationServer>();
                ServerIntegrate.Fork<DeployServer>();
                ServerIntegrate.Fork<ScriptHostBootstrapServer>();
                BeforeGateBootstrap?.Invoke();
                ServerIntegrate.Fork<ZeronGateBootstrapServer>();
                ServerIntegrate.Fork<ScriptEventBridgeServer>();
                ServerIntegrate.Fork<InstallServer>();
                ServerIntegrate.Fork<MailerServer>();
                ServerIntegrate.Fork<SchedulerServer>();
                ServerIntegrate.Fork<ReporterServer>();
                ServerIntegrate.Fork<ZmqServer>();
                ServerIntegrate.Fork<ManagedPackageServer>();
                ServerIntegrate.Fork<AuditServer>();
            }
            catch (Exception e)
            {
                ZNLogger.Common.Error(string.Format(
                    CultureInfo.InvariantCulture,
                    "DemandBoot Error:{0}\n{1}",
                    e.Message,
                    e.StackTrace));
                return false;
            }

            return true;
        }
    }
}
