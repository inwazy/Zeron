// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Zeron.Server.ZCore;
using Zeron.Server.ZServers;

namespace Zeron.Server.Background
{
    /// <summary>
    /// TaskDispatchWorker
    /// </summary>
    public class TaskDispatchWorker : BackgroundService
    {
        // Service provider.
        private readonly IServiceProvider m_ServiceProvider;

        // Settings.
        private readonly ServerSettings m_Settings;

        /// <summary>
        /// TaskDispatchWorker
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <param name="settings"></param>
        /// <returns>Returns void.</returns>
        public TaskDispatchWorker(IServiceProvider serviceProvider, ServerSettings settings)
        {
            m_ServiceProvider = serviceProvider;
            m_Settings = settings;
        }

        /// <summary>
        /// ExecuteAsync
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns>Returns Task.</returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using IServiceScope scope = m_ServiceProvider.CreateScope();
                TaskDispatcherServer taskDispatcher = scope.ServiceProvider.GetRequiredService<TaskDispatcherServer>();
                await taskDispatcher.DispatchPendingAssignmentsAsync(stoppingToken);

                await Task.Delay(m_Settings.DispatchIntervalMs, stoppingToken);
            }
        }
    }
}
