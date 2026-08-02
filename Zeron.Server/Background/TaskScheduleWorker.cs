// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Zeron.Server.ZCore;
using Zeron.Server.ZServers;

namespace Zeron.Server.Background
{
    /// <summary>
    /// TaskScheduleWorker
    /// </summary>
    public class TaskScheduleWorker : BackgroundService
    {
        // Service provider
        private readonly IServiceProvider m_ServiceProvider;

        // Server settings
        private readonly ServerSettings m_Settings;

        /// <summary>
        /// TaskScheduleWorker
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <param name="settings"></param>
        /// <returns>Returns void.</returns>
        public TaskScheduleWorker(
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
                TaskScheduleServer scheduleServer = scope.ServiceProvider.GetRequiredService<TaskScheduleServer>();
                await scheduleServer.ProcessDueSchedulesAsync(stoppingToken);

                int delayMs = Math.Max(1000, m_Settings.ScheduleIntervalMs);
                await Task.Delay(delayMs, stoppingToken);
            }
        }
    }
}
