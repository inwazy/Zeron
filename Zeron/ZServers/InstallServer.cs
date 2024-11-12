// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Timers;
using Zeron.ZCore;
using Zeron.ZCore.Container;
using Zeron.ZCore.Foundation;
using Zeron.ZCore.Type;
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
        public override void LoadConfig(NameValueCollection aConfig)
        {
            try
            {
                TimerQueuesTriggerInterval = int.Parse(aConfig["install_timer_queue_trigger_interval"] ?? "50000");
                // TimerQueuesWatchInterval = int.Parse(aConfig["install_timer_queue_watch_interval"] ?? "300000");
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
            m_EnableQueuesProc = false;
            m_EnableInstallQueue = false;
            m_QueuesSignal.Dispose();

            ServerIntegrate.FinishSingleStop();
        }

        /// <summary>
        /// QueuesProc
        /// </summary>
        /// <param name="aArg"></param>
        /// <returns>Returns void.</returns>
        private static void QueuesProc(object? aArg)
        {
            string? installToken;
            InstallQueuesType? queuesType;

            while (m_EnableQueuesProc)
            {
                m_QueuesSignal.WaitOne();

                if (!m_EnableInstallQueue)
                {
                    m_InstallQueues.TryDequeue(out Tuple<string?, InstallQueuesType?>? item);

                    if (item == null)
                    {
                        continue;
                    }

                    installToken = item.Item1;
                    queuesType = item.Item2;

                    if (installToken == null || installToken.Length == 0)
                    {
                        continue;
                    }

                    if (queuesType == null)
                    {
                        continue;
                    }

                    if (installToken.Contains("install"))
                    {
                        if (ExecuteInstallQueues(queuesType))
                        {
                            // Done
                        }
                    }

                    if (installToken.Contains("uninstall"))
                    {

                    }

                    if (installToken.Contains("status"))
                    {

                    }
                }

                Thread.Sleep(1000);
            }
        }

        /// <summary>
        /// TimerProc
        /// </summary>
        /// <param name="source"></param>
        /// <param name="args"></param>
        /// <returns>Returns void.</returns>
        private static void TimerProc(object? source, ElapsedEventArgs args)
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
        /// TimerProc
        /// </summary>
        /// <param name="source"></param>
        /// <param name="args"></param>
        /// <returns>Returns void.</returns>
        private static void QatcherProc(object? source, ElapsedEventArgs args)
        {
            if (!m_EnableInstallQueue)
            {
                return;
            }

            if (m_RunningProcId <= 0)
            {
                return;
            }

            try
            {
                Process? procRunning = Process.GetProcessById(m_RunningProcId);

                if (procRunning != null)
                {

                }
            }
            catch (ArgumentException e)
            {
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "InstallServer QatcherProc ArgumentException:{0}\n{1}", e.Message, e.StackTrace));
            }
            catch (InvalidOperationException e)
            {
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "InstallServer QatcherProc InvalidOperationException:{0}\n{1}", e.Message, e.StackTrace));
            }
        }

        /// <summary>
        /// ExecuteInstallQueues
        /// </summary>
        /// <param name="queuesType"></param>
        /// <returns>Returns bool.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ExecuteInstallQueues(InstallQueuesType? queuesType)
        {
            bool result = false;

            if (queuesType == null)
            {
                return result;
            }

            if (!GetBinaryFileFromUrl(queuesType))
            {
                return result;
            }

            m_EnableInstallQueue = true;
            m_TimerQueues.Stop();
            m_TimerWatcher.Start();

            try
            {
                if (File.Exists(queuesType.FilePath))
                {
                    Process? procStart = Process.Start(queuesType.FilePath, queuesType.Arguments ?? "");

                    if (procStart != null)
                    {
                        m_RunningProcId = procStart.Id;

                        procStart.EnableRaisingEvents = true;
                        procStart.WaitForExit();
                        procStart.Close();

                        m_RunningProcId = 0;

                        result = true;
                    }
                }
            }
            catch (InvalidOperationException e)
            {
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "InstallServer ExecuteInstallQueues QueuesProc InvalidOperationException:{0}\n{1}", e.Message, e.StackTrace));
            }
            catch (Win32Exception e)
            {
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "InstallServer ExecuteInstallQueues Win32Exception:{0}\n{1}", e.Message, e.StackTrace));
            }
            catch (FileNotFoundException e)
            {
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "InstallServer ExecuteInstallQueues FileNotFoundException:{0}\n{1}", e.Message, e.StackTrace));
            }

            m_EnableInstallQueue = false;
            m_TimerQueues.Start();
            m_TimerWatcher.Stop();

            return result;
        }

        /// <summary>
        /// AddQueues
        /// </summary>
        /// <param name="token"></param>
        /// <param name="queuesType"></param>
        /// <returns>Returns int.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int AddQueues(string? token, InstallQueuesType queuesType)
        {
            int result = m_InstallQueues.Count;

            if (token == null || token.Length == 0)
            {
                return result;
            }

            m_InstallQueues.Enqueue(new Tuple<string?, InstallQueuesType?>(token, queuesType));

            if (queuesType != null)
            {
                result = m_InstallQueues.Count;
            }

            return result;
        }

        /// <summary>
        /// GetBinaryFileFromUrl
        /// </summary>
        /// <param name="queuesType"></param>
        /// <returns>Returns bool.</returns>
        public static bool GetBinaryFileFromUrl(InstallQueuesType? queuesType)
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

            using (HttpClient? httpClient = new())
            {
                try
                {
                    using (Task<HttpResponseMessage>? httpResponse = httpClient.GetAsync(queuesType.RepoUrl))
                    {
                        httpResponse.Wait();

                        if (httpResponse.IsCompletedSuccessfully)
                        {
                            try
                            {
                                using (FileStream? fileStream = File.Create(queuesType.FilePath))
                                {
                                    httpResponse.Result.Content.CopyToAsync(fileStream).Wait();

                                    result = true;
                                }
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
            }

            return result;
        }
    }
}
