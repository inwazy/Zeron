// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using System.Globalization;
using NLog;
using Topshelf;
using Zeron.Demand.ZCore;
using Zeron.Demand.ZServers;
using Zeron.ZCore;
using Zeron.ZCore.Container;
using Zeron.ZServers;

namespace Zeron.Demand
{
    /// <summary>
    /// Program
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// Main
        /// </summary>
        /// <param name="args"></param>
        /// <returns>Returns void.</returns>
        public static void Main(string[] args)
        {
            LogManager.Setup().LoadConfigurationFromFile("NLog.config");

            bool result = BootLoader();

            if (!result)
            {
                return;
            }

            SCHostLoader Hostloader = new();

            TopshelfExitCode rc = HostFactory.Run(x =>
            {
                x.Service<SCLoader>(sc =>
                {
                    sc.ConstructUsing(name => new SCLoader());

                    sc.WhenStarted(tc => tc.WhenStart());
                    sc.WhenStopped(tc => tc.WhenStop());
                    sc.WhenPaused(tc => tc.WhenPause());
                    sc.WhenContinued(tc => tc.WhenContinue());
                    sc.WhenShutdown(tc => tc.WhenShutdown());
                });

                x.RunAsLocalSystem();

                x.SetServiceName(DeployServer.ServiceName);
                x.SetDisplayName(DeployServer.ServiceDisplayName);
                x.SetInstanceName(DeployServer.ServiceInstanceName);
                x.SetDescription(DeployServer.ServiceDescription);

                x.BeforeInstall(settings => Hostloader.BeforeInstall());
                x.BeforeUninstall(() => Hostloader.BeforeUninstall());

                x.AfterInstall(settings => Hostloader.AfterInstall());
                x.AfterUninstall(() => Hostloader.AfterUninstall());
            });

            int exitCode = (int)Convert.ChangeType(rc, rc.GetTypeCode(), CultureInfo.InvariantCulture);

            Environment.ExitCode = exitCode;
        }

        /// <summary>
        /// BootLoader
        /// </summary>
        /// <returns>Returns bool.</returns>
        private static bool BootLoader()
        {
            Environment.CurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;

            try
            {
                SchedulerServer.OnTaskDue = task => TaskPipelineExecutor.ExecuteTask(task);

                // Shared Servers
                ServerIntegrate.Fork<ConfigServer>();
                ServerIntegrate.Fork<AgentServer>();
                ServerIntegrate.Fork<ApplicationServer>();
                ServerIntegrate.Fork<DeployServer>();
                ServerIntegrate.Fork<ScriptHostBootstrapServer>();
                ServerIntegrate.Fork<ZeronGateBootstrapServer>();
                ServerIntegrate.Fork<ScriptEventBridgeServer>();
                ServerIntegrate.Fork<InstallServer>();
                ServerIntegrate.Fork<MailerServer>();
                ServerIntegrate.Fork<SchedulerServer>();

                // Local Servers
                ServerIntegrate.Fork<ReporterServer>();
                ServerIntegrate.Fork<ZmqServer>();
                ServerIntegrate.Fork<ManagedPackageServer>();
                ServerIntegrate.Fork<AuditServer>();
            }
            catch (Exception e)
            {
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "Boot Error:{0}\n{1}", e.Message, e.StackTrace));
            }

            return true;
        }
    }
}