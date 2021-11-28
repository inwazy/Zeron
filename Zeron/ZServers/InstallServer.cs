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

        // Signal Queues
        private static readonly Semaphore m_QueuesSignal = new(0, 1000);

        // ConcurrentDictionary Install Queues
        private static readonly ConcurrentQueue<Tuple<string?, InstallQueuesType?>> m_InstallQueues = new();

        // Timer Install Queues
        private static readonly System.Timers.Timer m_TimerQueues = new();

        // Timer Install Queues Trigger Interval
        private static readonly int m_TimerQueuesTriggerInterval = 60000;

        // Enable Queues trigger
        private static bool m_EnableQueuesProc = true;

        // Enable Queue Install
        private static bool m_EnableInstallQueue = false;

        /// <summary>
        /// LoadConfig
        /// </summary>
        /// <param name="aConfig"></param>
        /// <returns>Returns void.</returns>
        public override void LoadConfig(NameValueCollection aConfig)
        {
            try
            {
                
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
                m_TimerQueues.Interval = m_TimerQueuesTriggerInterval;
                m_TimerQueues.AutoReset = true;
                m_TimerQueues.Enabled = true;
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
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "InstallServer AddQueues SemaphoreFullException:{0}\n{1}", e.Message, e.StackTrace));
            }
            catch (IOException e)
            {
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "InstallServer AddQueues IOException:{0}\n{1}", e.Message, e.StackTrace));
            }
            catch (UnauthorizedAccessException e)
            {
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "InstallServer AddQueues UnauthorizedAccessException:{0}\n{1}", e.Message, e.StackTrace));
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

            try
            {
                if (File.Exists(queuesType.FilePath))
                {
                    Process? procStart = Process.Start(queuesType.FilePath, queuesType.Arguments ?? "");

                    if (procStart != null)
                    {
                        procStart.WaitForExit();

                        result = true;
                    }
                }
            }
            catch (InvalidOperationException e)
            {
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "InstallServer ExecuteQueues QueuesProc InvalidOperationException:{0}\n{1}", e.Message, e.StackTrace));
            }
            catch (Win32Exception e)
            {
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "InstallServer ExecuteQueues Win32Exception:{0}\n{1}", e.Message, e.StackTrace));
            }
            catch (FileNotFoundException e)
            {
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "InstallServer ExecuteQueues FileNotFoundException:{0}\n{1}", e.Message, e.StackTrace));
            }

            m_EnableInstallQueue = false;
            m_TimerQueues.Start();

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
                            using (FileStream? fileStream = File.Create(queuesType.FilePath))
                            {
                                httpResponse.Result.Content.CopyToAsync(fileStream).Wait();

                                result = true;
                            }
                        }
                    }
                }
                catch (InvalidOperationException e)
                {
                    ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "InstallServer GetFileFromUrl InvalidOperationException:{0}\n{1}", e.Message, e.StackTrace));
                }
                catch (HttpRequestException e)
                {
                    ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "InstallServer GetFileFromUrl HttpRequestException:{0}\n{1}", e.Message, e.StackTrace));
                }
                catch (TaskCanceledException e)
                {
                    ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "InstallServer GetFileFromUrl TaskCanceledException:{0}\n{1}", e.Message, e.StackTrace));
                }
            }

            return result;
        }
    }
}
