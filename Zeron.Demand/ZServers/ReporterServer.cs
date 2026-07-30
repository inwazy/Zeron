// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using System.Collections.Specialized;
using System.Globalization;
using System.Timers;
using Zeron.Demand.ZCore;
using Zeron.Demand.ZServers.Impls;
using Zeron.ZCore;
using Zeron.ZCore.Container;
using Zeron.ZCore.Foundation;
using Zeron.ZCore.Type;
using Zeron.ZInterfaces;
using Zeron.ZServers;

namespace Zeron.Demand.ZServers
{
    /// <summary>
    /// ReporterServer - sends heartbeat and forwards events to Zeron.Server.
    /// </summary>
    internal class ReporterServer : ConfigurationTable, IServer
    {
        // Heartbeat timer.
        private static readonly System.Timers.Timer s_HeartbeatTimer = new();

        // Enabled.
        private static bool s_Enabled;

        /// <summary>
        /// HeartbeatIntervalMs
        /// </summary>
        public static int HeartbeatIntervalMs
        {
            get;
            set;
        } = 30000;

        /// <summary>
        /// Enabled
        /// </summary>
        public static bool Enabled => s_Enabled;

        /// <summary>
        /// LoadConfig
        /// </summary>
        /// <param name="aConfig"></param>
        /// <returns>Returns void.</returns>
        public override void LoadConfig(NameValueCollection aConfig)
        {
            try
            {
                s_Enabled = bool.Parse(aConfig["server_enabled"] ?? "false");
                ReporterImpl.ServerUrl = aConfig["server_url"];
                HeartbeatIntervalMs = int.Parse(aConfig["server_heartbeat_interval_ms"] ?? "30000", CultureInfo.InvariantCulture);
            }
            catch (Exception e)
            {
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "ReporterServer Config Error:{0}\n{1}", e.Message, e.StackTrace));
            }
        }

        /// <summary>
        /// Initialize
        /// </summary>
        /// <returns>Returns void.</returns>
        public void Initialize()
        {
            if (!s_Enabled)
            {
                ZNLogger.Common.Info("ReporterServer disabled.");

                return;
            }

            s_HeartbeatTimer.Elapsed += OnHeartbeatTimer;
            s_HeartbeatTimer.Interval = HeartbeatIntervalMs;
            s_HeartbeatTimer.AutoReset = true;
            s_HeartbeatTimer.Enabled = true;

            ZNLogger.Common.Info(string.Format(CultureInfo.InvariantCulture,
                "ReporterServer initialized. ServerUrl={0}", ReporterImpl.ServerUrl));
        }

        /// <summary>
        /// Stop
        /// </summary>
        /// <returns>Returns void.</returns>
        public void Stop()
        {
            s_HeartbeatTimer.Stop();
            s_HeartbeatTimer.Dispose();

            ServerIntegrate.FinishSingleStop();
        }

        /// <summary>
        /// ForwardEvent
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="message"></param>
        /// <returns>Returns void.</returns>
        public static void ForwardEvent(string topic, string message)
        {
            if (!s_Enabled || string.IsNullOrWhiteSpace(ReporterImpl.ServerUrl))
            {
                return;
            }

            AgentEventReportType report = new()
            {
                AgentId = AgentServer.AgentId,
                Topic = topic,
                Payload = message
            };

            _ = ReporterImpl.SendEventAsync(report);
        }

        /// <summary>
        /// ReportTaskResult
        /// </summary>
        /// <param name="assignmentId"></param>
        /// <param name="success"></param>
        /// <param name="responseJson"></param>
        /// <param name="errorMessage"></param>
        /// <returns>Returns void.</returns>
        public static void ReportTaskResult(string? assignmentId, bool success, string? responseJson, string? errorMessage = null)
        {
            if (!s_Enabled || string.IsNullOrWhiteSpace(assignmentId))
            {
                return;
            }

            TaskResultReportType report = new()
            {
                AssignmentId = assignmentId,
                AgentId = AgentServer.AgentId,
                Success = success,
                ResponseJson = responseJson,
                ErrorMessage = errorMessage
            };

            _ = ReporterImpl.SendTaskResultAsync(report);
        }

        /// <summary>
        /// OnHeartbeatTimer
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        /// <returns>Returns void.</returns>
        private static async void OnHeartbeatTimer(object? sender, ElapsedEventArgs args)
        {
            await SendHeartbeatAndProcessTasksAsync();
        }

        /// <summary>
        /// SendHeartbeatAndProcessTasksAsync
        /// </summary>
        /// <returns>Returns void.</returns>
        public static async Task SendHeartbeatAndProcessTasksAsync()
        {
            if (!s_Enabled)
            {
                return;
            }

            AgentHeartbeatRequestType request = new()
            {
                AgentId = AgentServer.AgentId,
                MachineName = Environment.MachineName,
                UptimeSeconds = AgentServer.UptimeSeconds,
                Version = typeof(ReporterServer).Assembly.GetName().Version?.ToString(),
                InstallQueueCount = InstallJobTracker.GetStatus().QueueCount,
                InstallRunning = InstallJobTracker.GetStatus().IsRunning,
                SchedulerTaskCount = SchedulerServer.GetTasks().Count
            };

            AgentHeartbeatResponseType? response = await ReporterImpl.SendHeartbeatAsync(request);

            if (response?.PendingTasks == null)
            {
                return;
            }

            foreach (PendingTaskType pendingTask in response.PendingTasks)
            {
                string resultJson = InternalServiceInvoker.Invoke(pendingTask.TargetApi, pendingTask.Command);
                bool success = resultJson.Contains("\"success\":true", StringComparison.OrdinalIgnoreCase)
                    || resultJson.Contains("\"success\": true", StringComparison.OrdinalIgnoreCase);

                ReportTaskResult(pendingTask.AssignmentId, success, resultJson, success ? null : resultJson);
            }
        }
    }
}
