// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Timers;
using System.Text.Json;
using Zeron.ZCore;
using Zeron.ZCore.Container;
using Zeron.ZCore.Foundation;
using Zeron.ZCore.Type;
using Zeron.ZCore.Utils;
using Zeron.ZInterfaces;

namespace Zeron.ZServers
{
    /// <summary>
    /// InstallServer
    /// </summary>
    public class InstallServer : ConfigurationTable, IServer
    {
        // Subscriber background threading.
        private static readonly Thread m_QueuesThread = new(QueuesProc);

        // Signal Queues.
        private static readonly Semaphore m_QueuesSignal = new(0, 1000);

        // ConcurrentDictionary Install Queues.
        private static readonly ConcurrentQueue<Tuple<string?, InstallQueuesType?>> m_InstallQueues = new();

        // Timer Install Queues.
        private static readonly System.Timers.Timer m_TimerQueues = new();

        // Timer Install Watcher Queues.
        private static readonly System.Timers.Timer m_TimerWatcher = new();

        // Enable Queues trigger.
        private static bool m_EnableQueuesProc = true;

        // Enable Queue Install.
        private static bool m_EnableInstallQueue = false;

        // Running Proc Id.
        private static int m_RunningProcId = 0;

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
            InstallJobTracker.QueueCountProvider = () => m_InstallQueues.Count;

            try
            {
                m_QueuesThread.IsBackground = true;
                m_QueuesThread.Start();

                m_TimerQueues.Elapsed += TimerProc;
                m_TimerQueues.Interval = TimerQueuesTriggerInterval;
                m_TimerQueues.AutoReset = true;
                m_TimerQueues.Enabled = true;

                m_TimerWatcher.Elapsed += QatcherProc;
                m_TimerWatcher.Interval = TimerQueuesTriggerInterval;
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
            m_TimerQueues.Dispose();
            m_TimerWatcher.Dispose();
            m_EnableQueuesProc = false;
            m_EnableInstallQueue = false;

            try
            {
                m_QueuesSignal.Release();
            }
            catch (SemaphoreFullException)
            {
            }

            m_QueuesSignal.Dispose();

            ZNLogger.Common.Info("InstallServer stopped.");

            ServerIntegrate.FinishSingleStop();
        }

        /// <summary>
        /// GetQueueCount
        /// </summary>
        /// <returns>Returns int.</returns>
        public static int GetQueueCount()
        {
            return m_InstallQueues.Count;
        }

        /// <summary>
        /// QueuesProc
        /// </summary>
        /// <param name="aArg"></param>
        /// <returns>Returns void.</returns>
        private static void QueuesProc(
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
                    ExecuteQueues(operation, queuesType);
                }
            }
        }

        /// <summary>
        /// TimerProc
        /// </summary>
        /// <param name="source"></param>
        /// <param name="args"></param>
        /// <returns>Returns void.</returns>
        private static void TimerProc(
            object? source, 
            ElapsedEventArgs args)
        {
            if (m_InstallQueues.Count == 0)
            {
                return;
            }

            if (m_EnableInstallQueue)
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
        }

        /// <summary>
        /// QatcherProc
        /// </summary>
        /// <param name="source"></param>
        /// <param name="args"></param>
        /// <returns>Returns void.</returns>
        private static void QatcherProc(
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
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "InstallServer QatcherProc InvalidOperationException:{0}\n{1}", e.Message, e.StackTrace));
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
            if (queuesType == null)
            {
                return false;
            }

            string eventTopic = operation.Contains("uninstall", StringComparison.OrdinalIgnoreCase)
                ? "install.uninstall"
                : "install.started";

            PublishInstallEvent(eventTopic, queuesType, operation, null, null);

            InstallJobTracker.MarkRunning(queuesType.PackageName, operation);

            if (!ScriptExecutor.Execute(queuesType.ScriptBefore))
            {
                InstallJobTracker.MarkCompleted(queuesType.PackageName, operation, false, -1);
                PublishInstallEvent("install.failed", queuesType, operation, false, -1);

                return false;
            }

            bool result = false;
            int exitCode = -1;

            if (!GetBinaryFileFromUrl(queuesType))
            {
                InstallJobTracker.MarkCompleted(queuesType.PackageName, operation, false, exitCode);
                PublishInstallEvent("install.failed", queuesType, operation, false, exitCode);

                return false;
            }

            m_EnableInstallQueue = true;
            m_TimerQueues.Stop();
            m_TimerWatcher.Start();

            try
            {
                if (File.Exists(queuesType.FilePath))
                {
                    ProcessStartInfo startInfo = new()
                    {
                        FileName = queuesType.FilePath,
                        Arguments = queuesType.Arguments ?? "",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using Process? procStart = Process.Start(startInfo);

                    if (procStart != null)
                    {
                        m_RunningProcId = procStart.Id;
                        procStart.WaitForExit();
                        exitCode = procStart.ExitCode;
                        m_RunningProcId = 0;
                        result = exitCode == 0;
                    }
                }
            }
            catch (InvalidOperationException e)
            {
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "InstallServer ExecuteQueues InvalidOperationException:{0}\n{1}", e.Message, e.StackTrace));
            }
            catch (Win32Exception e)
            {
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "InstallServer ExecuteQueues Win32Exception:{0}\n{1}", e.Message, e.StackTrace));
            }
            catch (FileNotFoundException e)
            {
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "InstallServer ExecuteQueues FileNotFoundException:{0}\n{1}", e.Message, e.StackTrace));
            }

            bool scriptAfterOk = ScriptExecutor.Execute(queuesType.ScriptAfter);
            result = result && scriptAfterOk;

            m_EnableInstallQueue = false;
            m_TimerQueues.Start();
            m_TimerWatcher.Stop();

            InstallJobTracker.MarkCompleted(queuesType.PackageName, operation, result, exitCode);
            PublishInstallEvent(result ? "install.completed" : "install.failed", queuesType, operation, result, exitCode);

            ZNLogger.Common.Info(string.Format(CultureInfo.InvariantCulture,
                "InstallServer {0} {1}: success={2}, exitCode={3}",
                operation, queuesType.PackageName, result, exitCode));

            return result;
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
                timestamp = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture)
            };

            InstallEventPublisher.Publish(topic, JsonSerializer.Serialize(payload));
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
            bool result = false;

            if (queuesType == null)
            {
                return result;
            }

            if (queuesType.RepoUrl == null || string.IsNullOrEmpty(queuesType.RepoUrl)
                || queuesType.FilePath == null || string.IsNullOrEmpty(queuesType.FilePath))
            {
                return result;
            }

            if (File.Exists(queuesType.FilePath))
            {
                return true;
            }

            using HttpClient httpClient = new();

            try
            {
                using Task<HttpResponseMessage> httpResponse = httpClient.GetAsync(queuesType.RepoUrl);
                httpResponse.Wait();

                if (httpResponse.IsCompletedSuccessfully)
                {
                    try
                    {
                        string? directory = Path.GetDirectoryName(queuesType.FilePath);

                        if (!string.IsNullOrEmpty(directory))
                        {
                            Directory.CreateDirectory(directory);
                        }

                        using FileStream fileStream = File.Create(queuesType.FilePath);
                        httpResponse.Result.Content.CopyToAsync(fileStream).Wait();

                        result = true;
                    }
                    catch (UnauthorizedAccessException e)
                    {
                        ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "InstallServer GetBinaryFileFromUrl UnauthorizedAccessException:{0}\n{1}", e.Message, e.StackTrace));
                    }
                    catch (ArgumentException e)
                    {
                        ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "InstallServer GetBinaryFileFromUrl ArgumentException:{0}\n{1}", e.Message, e.StackTrace));
                    }
                    catch (PathTooLongException e)
                    {
                        ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "InstallServer GetBinaryFileFromUrl PathTooLongException:{0}\n{1}", e.Message, e.StackTrace));
                    }
                    catch (DirectoryNotFoundException e)
                    {
                        ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "InstallServer GetBinaryFileFromUrl DirectoryNotFoundException:{0}\n{1}", e.Message, e.StackTrace));
                    }
                    catch (IOException e)
                    {
                        ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "InstallServer GetBinaryFileFromUrl IOException:{0}\n{1}", e.Message, e.StackTrace));
                    }
                    catch (NotSupportedException e)
                    {
                        ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "InstallServer GetBinaryFileFromUrl NotSupportedException:{0}\n{1}", e.Message, e.StackTrace));
                    }
                }
            }
            catch (InvalidOperationException e)
            {
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "InstallServer GetBinaryFileFromUrl InvalidOperationException:{0}\n{1}", e.Message, e.StackTrace));
            }
            catch (HttpRequestException e)
            {
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "InstallServer GetBinaryFileFromUrl HttpRequestException:{0}\n{1}", e.Message, e.StackTrace));
            }
            catch (TaskCanceledException e)
            {
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "InstallServer GetBinaryFileFromUrl TaskCanceledException:{0}\n{1}", e.Message, e.StackTrace));
            }

            return result;
        }
    }
}
