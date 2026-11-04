// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using System.Globalization;
using Topshelf;
using Zeron.Demand.ZCore;
using Zeron.ZServers;

namespace Zeron.Demand
{
    /// <summary>
    /// Topshelf host runner for Demand Windows Service editions.
    /// </summary>
    public static class DemandHost
    {
        /// <summary>
        /// Runs the Demand agent as a Windows Service (Topshelf).
        /// </summary>
        public static void RunService()
        {
            SCHostLoader hostLoader = new();

            TopshelfExitCode rc = HostFactory.Run(x =>
            {
                x.Service<SCLoader>(sc =>
                {
                    sc.ConstructUsing(_ => new SCLoader());

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

                x.BeforeInstall(_ => hostLoader.BeforeInstall());
                x.BeforeUninstall(() => hostLoader.BeforeUninstall());

                x.AfterInstall(_ => hostLoader.AfterInstall());
                x.AfterUninstall(() => hostLoader.AfterUninstall());
            });

            int exitCode = (int)Convert.ChangeType(rc, rc.GetTypeCode(), CultureInfo.InvariantCulture);
            Environment.ExitCode = exitCode;
        }
    }
}
