// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Timers;
using Zeron.ZCore;
using Zeron.ZCore.Container;
using Zeron.ZCore.Foundation;
using Zeron.ZCore.Type;
using Zeron.ZCore.Utils;
using Zeron.ZInterfaces;

namespace Zeron.ZServers
{
    /// <summary>
    /// InstallServer - install queue runtime (instance state + Current facade).
    /// </summary>
    public class InstallServer : ConfigurationTable, IServer
    {
        // Active runtime instance.
        public static InstallServer? Current
        {
            get;
            private set;
        }

        // Pending assignment handler if set before Initialize.
        private static Action<string, bool, string?, string?>? s_PendingAssignmentHandler;

        // Subscriber background threading.
        private Thread? m_QueuesThread;

        // Signal Queues.
        private readonly Semaphore m_QueuesSignal = new(0, 1000);

        // ConcurrentDictionary Install Queues.
        private readonly ConcurrentQueue<Tuple<string?, InstallQueuesType?>> m_InstallQueues = new();

        // Timer Install Queues.
        private readonly System.Timers.Timer m_TimerQueues = new();

        // Timer Install Watcher Queues.
        private readonly System.Timers.Timer m_TimerWatcher = new();

        // Enable Queues trigger.
        private bool m_EnableQueuesProc = true;

        // Enable Queue Install.
        private bool m_EnableInstallQueue;

        // Running Proc Id.
        private int m_RunningProcId;

        // Assignment completed handler for this instance.
        private Action<string, bool, string?, string?>? m_AssignmentCompletedHandler;

        /// <summary>
        /// AssignmentCompletedHandler - notified when an assignment-linked install finishes.
        /// Args: assignmentId, success, responseJson, errorMessage.
        /// </summary>
        public static Action<string, bool, string?, string?>? AssignmentCompletedHandler
        {
            get => Current?.m_AssignmentCompletedHandler ?? s_PendingAssignmentHandler;
            set
            {
                if (Current != null)
                {
                    Current.m_AssignmentCompletedHandler = value;
                }
                else
                {
                    s_PendingAssignmentHandler = value;
                }
            }
        }

        /// <summary>
        /// TimerQueuesTriggerInterval
        /// </summary>
        public static int TimerQueuesTriggerInterval
        {
            get;
            set;
        }

        /// <summary>
        /// TimerQueuesWatchInterval
        /// </summary>
        public static int TimerQueuesWatchInterval
        {
            get;
            set;
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
                TimerQueuesTriggerInterval = int.Parse(aConfig["install_timer_queue_trigger_interval"] ?? "50000", CultureInfo.InvariantCulture);
                TimerQueuesWatchInterval = 50000;
            }
            catch (Exception e)
            {
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "InstallServer Config Error:{0}\n{1}", e.Message, e.StackTrace));
            }
        }

        /// <summary>
        /// Initialize
        /// </summary>
        /// <returns>Returns void.</returns>
        public void Initialize()
        {
            Current = this;
            m_AssignmentCompletedHandler = s_PendingAssignmentHandler;
            s_PendingAssignmentHandler = null;

            InstallJobTracker.QueueCountProvider = () => Current?.m_InstallQueues.Count ?? 0;

            try
            {
                m_QueuesThread = new Thread(QueuesProc)
                {
                    IsBackground = true,
                    Name = "InstallServer.Queues"
                };
                m_QueuesThread.Start();

                m_TimerQueues.Elapsed += TimerProc;
                m_TimerQueues.Interval = TimerQueuesTriggerInterval > 0 ? TimerQueuesTriggerInterval : 50000;
                m_TimerQueues.AutoReset = true;
                m_TimerQueues.Enabled = true;

                m_TimerWatcher.Elapsed += WatcherProc;
                m_TimerWatcher.Interval = TimerQueuesTriggerInterval > 0 ? TimerQueuesTriggerInterval : 50000;
                m_TimerWatcher.AutoReset = true;
                m_TimerWatcher.Enabled = true;

                ZNLogger.Common.Info("InstallServer initialized.");
            }
            catch (ThreadStateException e)
            {
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "InstallServer Error:{0}\n{1}", e.Message, e.StackTrace));
            }
            catch (OutOfMemoryException e)
            {
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "InstallServer Error:{0}\n{1}", e.Message, e.StackTrace));
            }
        }

        /// <summary>
        /// Stop
        /// </summary>
        /// <returns>Returns void.</returns>
        public void Stop()
        {
            m_EnableQueuesProc = false;
            m_EnableInstallQueue = false;
            m_AssignmentCompletedHandler = null;

            try
            {
                m_TimerQueues.Dispose();
                m_TimerWatcher.Dispose();
            }
            catch (Exception)
            {
            }

            try
            {
                m_QueuesSignal.Release();
            }
            catch (SemaphoreFullException)
            {
            }
            catch (ObjectDisposedException)
            {
            }

            try
            {
                m_QueuesSignal.Dispose();
            }
            catch (Exception)
            {
            }

            if (ReferenceEquals(Current, this))
            {
                Current = null;
            }

            ZNLogger.Common.Info("InstallServer stopped.");

            ServerIntegrate.FinishSingleStop();
        }

        /// <summary>
        /// GetQueueCount
        /// </summary>
        /// <returns>Returns int.</returns>
        public static int GetQueueCount()
        {
            return Current?.m_InstallQueues.Count ?? 0;
        }

        /// <summary>
        /// QueuesProc
        /// </summary>
        /// <param name="aArg"></param>
        /// <returns>Returns void.</returns>
        private void QueuesProc(
            object? aArg)
        {
            while (m_EnableQueuesProc)
            {
                m_QueuesSignal.WaitOne();

                if (m_EnableInstallQueue)
                {
                    continue;
                }

                if (!m_InstallQueues.TryDequeue(out Tuple<string?, InstallQueuesType?>? item) || item == null)
                {
                    continue;
                }

                string? operation = item.Item1;
                InstallQueuesType? queuesType = item.Item2;

                if (operation == null || operation.Length == 0 || queuesType == null)
                {
                    continue;
                }

                if (operation.Contains("install", StringComparison.OrdinalIgnoreCase)
                    || operation.Contains("uninstall", StringComparison.OrdinalIgnoreCase))
                {
                    ExecuteQueuesCore(operation, queuesType);
                }
            }
        }

        /// <summary>
        /// TimerProc
        /// </summary>
        /// <param name="source"></param>
        /// <param name="args"></param>
        /// <returns>Returns void.</returns>
        private void TimerProc(
            object? source, 
            ElapsedEventArgs args)
        {
            if (m_InstallQueues.Count == 0 || m_EnableInstallQueue)
            {
                return;
            }

            try
            {
                m_QueuesSignal.Release();
            }
            catch (SemaphoreFullException e)
            {
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "InstallServer TimerProc SemaphoreFullException:{0}\n{1}", e.Message, e.StackTrace));
            }
            catch (IOException e)
            {
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "InstallServer TimerProc IOException:{0}\n{1}", e.Message, e.StackTrace));
            }
            catch (UnauthorizedAccessException e)
            {
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "InstallServer TimerProc UnauthorizedAccessException:{0}\n{1}", e.Message, e.StackTrace));
            }
            catch (ObjectDisposedException)
            {
            }
        }

        /// <summary>
        /// WatcherProc
        /// </summary>
        /// <param name="source"></param>
        /// <param name="args"></param>
        /// <returns>Returns void.</returns>
        private void WatcherProc(
            object? source, 
            ElapsedEventArgs args)
        {
            if (!m_EnableInstallQueue || m_RunningProcId <= 0)
            {
                return;
            }

            try
            {
                Process.GetProcessById(m_RunningProcId);
            }
            catch (ArgumentException)
            {
                m_RunningProcId = 0;
            }
            catch (InvalidOperationException e)
            {
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "InstallServer WatcherProc InvalidOperationException:{0}\n{1}", e.Message, e.StackTrace));
            }
        }

        /// <summary>
        /// ExecuteQueues
        /// </summary>
        /// <param name="operation"></param>
        /// <param name="queuesType"></param>
        /// <returns>Returns bool.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ExecuteQueues(
            string operation, 
            InstallQueuesType? queuesType)
        {
            return Current?.ExecuteQueuesCore(operation, queuesType) ?? false;
        }

        /// <summary>
        /// ExecuteInstallQueues
        /// </summary>
        /// <param name="queuesType"></param>
        /// <returns>Returns bool.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ExecuteInstallQueues(
            InstallQueuesType? queuesType)
        {
            return ExecuteQueues("install", queuesType);
        }

        /// <summary>
        /// ExecuteQueuesCore
        /// </summary>
        /// <param name="operation"></param>
        /// <param name="queuesType"></param>
        /// <returns>Returns bool.</returns>
        private bool ExecuteQueuesCore(
            string operation,
            InstallQueuesType? queuesType)
        {
            if (queuesType == null)
            {
                return false;
            }

            BeginInstall(operation, queuesType);

            if (!RunBeforeScript(queuesType))
            {
                return FinishFailed(queuesType, operation, -1);
            }

            if (!EnsureBinary(queuesType))
            {
                return FinishFailed(queuesType, operation, -1);
            }

            EnterInstallLock();

            try
            {
                (bool processOk, int exitCode) = RunInstallerProcess(queuesType);
                bool scriptAfterOk = RunAfterScript(queuesType);
                bool result = processOk && scriptAfterOk;

                return FinishInstall(queuesType, operation, result, exitCode);
            }
            finally
            {
                LeaveInstallLock();
            }
        }

        /// <summary>
        /// BeginInstall
        /// </summary>
        /// <param name="operation"></param>
        /// <param name="queuesType"></param>
        /// <returns>Returns void.</returns>
        private static void BeginInstall(
            string operation,
            InstallQueuesType queuesType)
        {
            string eventTopic = operation.Contains("uninstall", StringComparison.OrdinalIgnoreCase)
                ? "install.uninstall"
                : "install.started";

            PublishInstallEvent(eventTopic, queuesType, operation, null, null);
            InstallJobTracker.MarkRunning(queuesType.PackageName, operation);
        }

        /// <summary>
        /// RunBeforeScript
        /// </summary>
        /// <param name="queuesType"></param>
        /// <returns>Returns bool.</returns>
        private static bool RunBeforeScript(
            InstallQueuesType queuesType)
        {
            return ScriptExecutor.Execute(queuesType.ScriptBefore);
        }

        /// <summary>
        /// EnsureBinary
        /// </summary>
        /// <param name="queuesType"></param>
        /// <returns>Returns bool.</returns>
        private static bool EnsureBinary(
            InstallQueuesType queuesType)
        {
            return InstallBinaryServer.TryDownload(queuesType);
        }

        /// <summary>
        /// RunAfterScript
        /// </summary>
        /// <param name="queuesType"></param>
        /// <returns>Returns bool.</returns>
        private static bool RunAfterScript(
            InstallQueuesType queuesType)
        {
            return ScriptExecutor.Execute(queuesType.ScriptAfter);
        }

        /// <summary>
        /// EnterInstallLock
        /// </summary>
        /// <returns>Returns void.</returns>
        private void EnterInstallLock()
        {
            m_EnableInstallQueue = true;
            m_TimerQueues.Stop();
            m_TimerWatcher.Start();
        }

        /// <summary>
        /// LeaveInstallLock
        /// </summary>
        /// <returns>Returns void.</returns>
        private void LeaveInstallLock()
        {
            m_EnableInstallQueue = false;
            m_TimerQueues.Start();
            m_TimerWatcher.Stop();
        }

        /// <summary>
        /// RunInstallerProcess
        /// </summary>
        /// <param name="queuesType"></param>
        /// <returns>Returns (success, exitCode).</returns>
        private (bool Success, int ExitCode) RunInstallerProcess(
            InstallQueuesType queuesType)
        {
            bool result = false;
            int exitCode = -1;

            try
            {
                if (string.IsNullOrEmpty(queuesType.FilePath) || !File.Exists(queuesType.FilePath))
                {
                    return (false, exitCode);
                }

                ProcessStartInfo startInfo = new()
                {
                    FileName = queuesType.FilePath,
                    Arguments = queuesType.Arguments ?? "",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using Process? procStart = Process.Start(startInfo);

                if (procStart == null)
                {
                    return (false, exitCode);
                }

                m_RunningProcId = procStart.Id;
                procStart.WaitForExit();
                exitCode = procStart.ExitCode;
                m_RunningProcId = 0;
                result = exitCode == 0;
            }
            catch (InvalidOperationException e)
            {
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "InstallServer RunInstallerProcess InvalidOperationException:{0}\n{1}", e.Message, e.StackTrace));
            }
            catch (Win32Exception e)
            {
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "InstallServer RunInstallerProcess Win32Exception:{0}\n{1}", e.Message, e.StackTrace));
            }
            catch (FileNotFoundException e)
            {
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "InstallServer RunInstallerProcess FileNotFoundException:{0}\n{1}", e.Message, e.StackTrace));
            }

            return (result, exitCode);
        }

        /// <summary>
        /// FinishFailed
        /// </summary>
        /// <param name="queuesType"></param>
        /// <param name="operation"></param>
        /// <param name="exitCode"></param>
        /// <returns>Returns false.</returns>
        private bool FinishFailed(
            InstallQueuesType queuesType,
            string operation,
            int exitCode)
        {
            return FinishInstall(queuesType, operation, success: false, exitCode);
        }

        /// <summary>
        /// FinishInstall
        /// </summary>
        /// <param name="queuesType"></param>
        /// <param name="operation"></param>
        /// <param name="success"></param>
        /// <param name="exitCode"></param>
        /// <returns>Returns bool.</returns>
        private bool FinishInstall(
            InstallQueuesType queuesType,
            string operation,
            bool success,
            int exitCode)
        {
            InstallJobTracker.MarkCompleted(queuesType.PackageName, operation, success, exitCode);
            PublishInstallEvent(success ? "install.completed" : "install.failed", queuesType, operation, success, exitCode);
            NotifyAssignmentCompleted(queuesType, operation, success, exitCode);

            ZNLogger.Common.Info(string.Format(CultureInfo.InvariantCulture,
                "InstallServer {0} {1}: success={2}, exitCode={3}",
                operation, queuesType.PackageName, success, exitCode));

            return success;
        }

        /// <summary>
        /// PublishInstallEvent
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="queuesType"></param>
        /// <param name="operation"></param>
        /// <param name="success"></param>
        /// <param name="exitCode"></param>
        /// <returns>Returns void.</returns>
        private static void PublishInstallEvent(
            string topic, 
            InstallQueuesType queuesType, 
            string operation, 
            bool? success, 
            int? exitCode)
        {
            var payload = new
            {
                topic,
                package = queuesType.PackageName,
                operation,
                success,
                exitCode,
                assignmentId = queuesType.AssignmentId,
                timestamp = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture)
            };

            InstallEventPublisher.Publish(topic, JsonSerializer.Serialize(payload));
        }

        /// <summary>
        /// NotifyAssignmentCompleted
        /// </summary>
        /// <param name="queuesType"></param>
        /// <param name="operation"></param>
        /// <param name="success"></param>
        /// <param name="exitCode"></param>
        /// <returns>Returns void.</returns>
        private void NotifyAssignmentCompleted(
            InstallQueuesType queuesType,
            string operation,
            bool success,
            int exitCode)
        {
            if (string.IsNullOrWhiteSpace(queuesType.AssignmentId) || m_AssignmentCompletedHandler == null)
            {
                return;
            }

            string responseJson = JsonSerializer.Serialize(new
            {
                success,
                completed = true,
                package = queuesType.PackageName,
                operation,
                exitCode
            });

            string? errorMessage = success
                ? null
                : string.Format(CultureInfo.InvariantCulture,
                    "Install {0} failed for '{1}' (exitCode={2}).",
                    operation,
                    queuesType.PackageName,
                    exitCode);

            try
            {
                m_AssignmentCompletedHandler(queuesType.AssignmentId, success, responseJson, errorMessage);
            }
            catch (Exception e)
            {
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture,
                    "InstallServer AssignmentCompletedHandler Error:{0}\n{1}", e.Message, e.StackTrace));
            }
        }

        /// <summary>
        /// AddQueues
        /// </summary>
        /// <param name="token"></param>
        /// <param name="queuesType"></param>
        /// <returns>Returns int.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int AddQueues(
            string? token, 
            InstallQueuesType queuesType)
        {
            return Current?.AddQueuesCore(token, queuesType) ?? 0;
        }

        /// <summary>
        /// AddQueuesCore
        /// </summary>
        /// <param name="token"></param>
        /// <param name="queuesType"></param>
        /// <returns>Returns int.</returns>
        private int AddQueuesCore(
            string? token,
            InstallQueuesType queuesType)
        {
            int result = m_InstallQueues.Count;

            if (token == null || token.Length == 0)
            {
                return result;
            }

            m_InstallQueues.Enqueue(new Tuple<string?, InstallQueuesType?>(token, queuesType));

            result = m_InstallQueues.Count;

            ZNLogger.Common.Info(string.Format(CultureInfo.InvariantCulture,
                "InstallServer queued {0} for {1} (queue size: {2})",
                token, queuesType.PackageName, result));

            return result;
        }

        /// <summary>
        /// GetBinaryFileFromUrl
        /// </summary>
        /// <param name="queuesType"></param>
        /// <returns>Returns bool.</returns>
        public static bool GetBinaryFileFromUrl(
            InstallQueuesType? queuesType)
        {
            return InstallBinaryServer.TryDownload(queuesType);
        }
    }
}
