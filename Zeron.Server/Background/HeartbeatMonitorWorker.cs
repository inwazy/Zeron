// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Zeron.Server.ZCore;
using Zeron.Server.ZServers;

namespace Zeron.Server.Background
{
    /// <summary>
    /// HeartbeatMonitorWorker
    /// </summary>
    public class HeartbeatMonitorWorker : BackgroundService
    {
        // Service provider.
        private readonly IServiceProvider m_ServiceProvider;

        // Settings.
        private readonly ServerSettings m_Settings;

        /// <summary>
        /// HeartbeatMonitorWorker
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <param name="settings"></param>
        /// <returns>Returns void.</returns>
        public HeartbeatMonitorWorker(
            IServiceProvider serviceProvider, 
            ServerSettings settings)
        {
            m_ServiceProvider = serviceProvider;
            m_Settings = settings;
        }

        /// <summary>
        /// ExecuteAsync
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns>Returns Task.</returns>
        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using IServiceScope scope = m_ServiceProvider.CreateScope();
                AgentManagerServer agentManager = scope.ServiceProvider.GetRequiredService<AgentManagerServer>();
                await agentManager.MarkOfflineAgentsAsync(m_Settings.HeartbeatTimeoutSeconds, stoppingToken);

                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
    }
}
