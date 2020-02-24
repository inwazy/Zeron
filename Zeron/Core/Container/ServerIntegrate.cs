using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Zeron.Interfaces;

namespace Zeron.Core.Container
{
    /// <summary>
    /// ServerIntegrate
    /// </summary>
    public static class ServerIntegrate
    {
        // Signal collection list.
        private static readonly List<IServer> m_Collection = new List<IServer>();

        // Signal stop wait threading.
        private static readonly System.Threading.Semaphore m_StopSignal = new System.Threading.Semaphore(0, 20000);

        /// <summary>
        /// Fork
        /// </summary>
        /// <param name="T"></param>
        /// <returns>Returns void.</returns>
        public static void Fork<T>() where T : IServer, new()
        {
            if (m_Collection == null)
            {
                return;
            }

            try
            {
                IServer item = new T() as IServer;

                m_Collection.Add(item);

                item.Initialize();
            }
            catch (Exception e)
            {
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "ServerIntegrate Fork Error:{0}\n{1}", e.Message, e.StackTrace));
            }
        }

        /// <summary>
        /// StopAll
        /// </summary>
        /// <returns>Returns void.</returns>
        public static void StopAll()
        {
            List<IServer> items = m_Collection.ToList();

            if (items == null)
            {
                return;
            }

            m_Collection.Clear();

            items.Reverse();

            try
            {
                foreach (IServer i in items)
                {
                    i.Stop();
                }
            }
            catch (Exception e)
            {
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "ServerIntegrate StopAll Error:{0}\n{1}", e.Message, e.StackTrace));
            }

            int finishCount = 0;

            Stopwatch watch = new Stopwatch();

            watch.Start();

            while (finishCount < items.Count)
            {
                long timeout = 10000 - watch.ElapsedMilliseconds;

                if (timeout < 0)
                {
                    timeout = 0;
                }

                bool fine = m_StopSignal.WaitOne(TimeSpan.FromMilliseconds(timeout));

                if (fine)
                {
                    finishCount++;
                }
                else
                {
                    break;
                }
            }

            watch.Stop();
        }

        /// <summary>
        /// FinishSingleStop
        /// </summary>
        /// <returns>Returns void.</returns>
        public static void FinishSingleStop()
        {
            if (m_StopSignal == null)
            {
                return;
            }

            try
            {
                m_StopSignal.Release();
            }
            catch (Exception e)
            {
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "ServerIntegrate Error:{0}\n{1}", e.Message, e.StackTrace));
            }
        }
    }
}
