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
using Zeron.ZCore.Utils;
using Zeron.ZInterfaces;
using Zeron.ZServers;

namespace Zeron.Demand.ZServers
{
    /// <summary>
    /// ReporterServer - heartbeat and event forwarder (instance state + Current facade).
    /// </summary>
    internal class ReporterServer : ConfigurationTable, IServer
    {
        // Active runtime instance.
        public static ReporterServer? Current
        {
            get;
            private set;
        }

        // Configured enabled flag (written by LoadConfig before Fork).
        private static bool s_ConfigEnabled;

        // Heartbeat timer.
        private readonly System.Timers.Timer m_HeartbeatTimer = new();

        // Runtime enabled flag for this instance.
        private bool m_Enabled;

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
        public static bool Enabled => Current?.m_Enabled == true || (Current == null && s_ConfigEnabled);

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
                s_ConfigEnabled = bool.Parse(aConfig["server_enabled"] ?? "false");
                ReporterImpl.ServerUrl = aConfig["server_url"];
                ReporterImpl.ServerApiKey = aConfig["server_api_key"] ?? "zeron.testkey";
                ReporterImpl.HmacEnabled = bool.Parse(aConfig["server_hmac_enabled"] ?? "false");
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
            Current = this;
            m_Enabled = s_ConfigEnabled;

            if (!m_Enabled)
            {
                ZNLogger.Common.Info("ReporterServer disabled.");

                return;
            }

            InstallServer.AssignmentCompletedHandler = (assignmentId, success, responseJson, errorMessage) =>
                ReportTaskResult(assignmentId, success, responseJson, errorMessage);

            m_HeartbeatTimer.Elapsed += OnHeartbeatTimer;
            m_HeartbeatTimer.Interval = HeartbeatIntervalMs > 0 ? HeartbeatIntervalMs : 30000;
            m_HeartbeatTimer.AutoReset = true;
            m_HeartbeatTimer.Enabled = true;

            ZNLogger.Common.Info(string.Format(CultureInfo.InvariantCulture,
                "ReporterServer initialized. ServerUrl={0}", ReporterImpl.ServerUrl));

            _ = SendHeartbeatAndProcessTasksAsync();
        }

        /// <summary>
        /// Stop
        /// </summary>
        /// <returns>Returns void.</returns>
        public void Stop()
        {
            InstallServer.AssignmentCompletedHandler = null;
            m_Enabled = false;

            try
            {
                m_HeartbeatTimer.Stop();
                m_HeartbeatTimer.Dispose();
            }
            catch (Exception)
            {
            }

            if (ReferenceEquals(Current, this))
            {
                Current = null;
            }

            ServerIntegrate.FinishSingleStop();
        }

        /// <summary>
        /// ForwardEvent
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="message"></param>
        /// <returns>Returns void.</returns>
        public static void ForwardEvent(
            string topic, 
            string message)
        {
            Current?.ForwardEventCore(topic, message);
        }

        /// <summary>
        /// ReportTaskResult
        /// </summary>
        /// <param name="assignmentId"></param>
        /// <param name="success"></param>
        /// <param name="responseJson"></param>
        /// <param name="errorMessage"></param>
        /// <returns>Returns void.</returns>
        public static void ReportTaskResult(
            string? assignmentId, 
            bool success, 
            string? responseJson, 
            string? errorMessage = null)
        {
            Current?.ReportTaskResultCore(assignmentId, success, responseJson, errorMessage);
        }

        /// <summary>
        /// ForwardEventCore
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="message"></param>
        /// <returns>Returns void.</returns>
        private void ForwardEventCore(
            string topic,
            string message)
        {
            if (!m_Enabled || string.IsNullOrWhiteSpace(ReporterImpl.ServerUrl))
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
        /// ReportTaskResultCore
        /// </summary>
        /// <param name="assignmentId"></param>
        /// <param name="success"></param>
        /// <param name="responseJson"></param>
        /// <param name="errorMessage"></param>
        /// <returns>Returns void.</returns>
        private void ReportTaskResultCore(
            string? assignmentId,
            bool success,
            string? responseJson,
            string? errorMessage)
        {
            if (!m_Enabled || string.IsNullOrWhiteSpace(assignmentId))
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
        private async void OnHeartbeatTimer(
            object? sender, 
            ElapsedEventArgs args)
        {
            try
            {
                await SendHeartbeatAndProcessTasksAsync();
            }
            catch (Exception e)
            {
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture,
                    "ReporterServer OnHeartbeatTimer Error:{0}\n{1}", e.Message, e.StackTrace));
            }
        }

        /// <summary>
        /// SendHeartbeatAndProcessTasksAsync
        /// </summary>
        /// <returns>Returns void.</returns>
        public static async Task SendHeartbeatAndProcessTasksAsync()
        {
            ReporterServer? current = Current;

            if (current == null || !current.m_Enabled)
            {
                return;
            }

            await current.SendHeartbeatAndProcessTasksCoreAsync();
        }

        /// <summary>
        /// SendHeartbeatAndProcessTasksCoreAsync
        /// </summary>
        /// <returns>Returns void.</returns>
        private async Task SendHeartbeatAndProcessTasksCoreAsync()
        {
            AgentHeartbeatRequestType request = new()
            {
                AgentId = AgentServer.AgentId,
                MachineName = Environment.MachineName,
                UptimeSeconds = AgentServer.UptimeSeconds,
                Version = typeof(ReporterServer).Assembly.GetName().Version?.ToString(),
                InstallQueueCount = InstallJobTracker.GetStatus().QueueCount,
                InstallRunning = InstallJobTracker.GetStatus().IsRunning,
                SchedulerTaskCount = SchedulerServer.GetTasks().Count,
                SupportedEngines = ScriptHostServer.ListEngines(),
                LastCatalogSyncAt = ManagedPackageServer.LastCatalogSyncUtc
            };

            AgentHeartbeatResponseType? response = await ReporterImpl.SendHeartbeatAsync(request);

            if (response == null)
            {
                ZNLogger.Common.Warn(string.Format(CultureInfo.InvariantCulture,
                    "ReporterServer heartbeat failed for AgentId={0}", AgentServer.AgentId));

                return;
            }

            if (!response.Success)
            {
                ZNLogger.Common.Warn(string.Format(CultureInfo.InvariantCulture,
                    "ReporterServer heartbeat rejected for AgentId={0}", AgentServer.AgentId));

                return;
            }

            ZNLogger.Common.Info(string.Format(CultureInfo.InvariantCulture,
                "ReporterServer heartbeat ok. AgentId={0}, PendingTasks={1}",
                AgentServer.AgentId,
                response.PendingTasks?.Count ?? 0));

            if (response.PendingTasks == null)
            {
                return;
            }

            foreach (PendingTaskType pendingTask in response.PendingTasks)
            {
                string resultJson = InternalServiceInvoker.Invoke(pendingTask.TargetApi, pendingTask.Command);
                bool success = resultJson.Contains("\"success\":true", StringComparison.OrdinalIgnoreCase)
                    || resultJson.Contains("\"success\": true", StringComparison.OrdinalIgnoreCase);

                ReportTaskResultCore(pendingTask.AssignmentId, success, resultJson, success ? null : resultJson);
            }
        }
    }
}
