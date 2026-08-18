// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Zeron.Server.ZCore;
using Zeron.Server.ZServers;

namespace Zeron.Server.Background
{
    /// <summary>
    /// DataRetentionWorker - periodic prune of audit, notifications, and catalog versions.
    /// </summary>
    public class DataRetentionWorker : BackgroundService
    {
        // Service provider.
        private readonly IServiceProvider m_ServiceProvider;

        // Settings.
        private readonly ServerSettings m_Settings;

        /// <summary>
        /// DataRetentionWorker
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <param name="settings"></param>
        /// <returns>Returns void.</returns>
        public DataRetentionWorker(
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
                if (m_Settings.RetentionEnabled)
                {
                    using IServiceScope scope = m_ServiceProvider.CreateScope();
                    DataRetentionServer retentionServer = scope.ServiceProvider.GetRequiredService<DataRetentionServer>();
                    await retentionServer.PruneAsync(stoppingToken);
                }

                int minutes = Math.Max(1, m_Settings.RetentionIntervalMinutes);
                await Task.Delay(TimeSpan.FromMinutes(minutes), stoppingToken);
            }
        }
    }
}
