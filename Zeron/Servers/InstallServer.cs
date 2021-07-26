// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using NLog.Internal;
using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using Zeron.Core;
using Zeron.Core.Base;
using Zeron.Core.Container;
using Zeron.Core.Type;
using Zeron.Interfaces;

namespace Zeron.Servers
{
    /// <summary>
    /// InstallServer
    /// </summary>
    public class InstallServer : ConfigurationTable, IServer
    {
        // Subscriber background threading.
        private static readonly Thread m_QueuesThread = new Thread(QueuesProc);

        // Signal Queues
        private static readonly Semaphore m_QueuesSignal = new Semaphore(0, 1000);

        // ConcurrentDictionary Install Queues
        private static readonly ConcurrentQueue<Tuple<string, InstallQueuesType>> m_InstallQueues = new ConcurrentQueue<Tuple<string, InstallQueuesType>>();

        // Enable Queues trigger.
        private static bool m_EnableQueuesProc = true;

        /// <summary>
        /// LoadConfig
        /// </summary>
        /// <param name="aConfig"></param>
        /// <returns>Returns void.</returns>
        public override void LoadConfig(ConfigurationManager aConfig)
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
            m_QueuesSignal.Dispose();

            ServerIntegrate.FinishSingleStop();
        }

        /// <summary>
        /// QueuesProc
        /// </summary>
        /// <param name="aArg"></param>
        /// <returns>Returns void.</returns>
        private static void QueuesProc(object aArg)
        {
            string installToken;
            InstallQueuesType queuesType;

            while (m_EnableQueuesProc)
            {
                m_QueuesSignal.WaitOne();
                m_InstallQueues.TryDequeue(out Tuple<string, InstallQueuesType> item);

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

                if (!File.Exists(queuesType.FilePath))
                    continue;

                try
                {
                    Process.Start(queuesType.FilePath, queuesType.Arguments);
                }
                catch (InvalidOperationException e)
                {
                    ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "InstallServer QueuesProc Error:{0}\n{1}", e.Message, e.StackTrace));
                }
                catch (Win32Exception e)
                {
                    ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "InstallServer QueuesProc Error:{0}\n{1}", e.Message, e.StackTrace));
                }
                catch (FileNotFoundException e)
                {
                    ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "InstallServer QueuesProc Error:{0}\n{1}", e.Message, e.StackTrace));
                }

                Thread.Sleep(100);
            }
        }

        /// <summary>
        /// AddQueues
        /// </summary>
        /// <param name="token"></param>
        /// <param name="queuesType"></param>
        /// <returns>Returns void.</returns>
        public static void AddQueues(string token, InstallQueuesType queuesType)
        {
            if (token == null || token.Length == 0)
            {
                return;
            }

            m_InstallQueues.Enqueue(new Tuple<string, InstallQueuesType>(token, queuesType));
            m_QueuesSignal.Release();
        }
    }
}
