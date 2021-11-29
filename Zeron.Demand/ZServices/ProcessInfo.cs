// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Newtonsoft.Json;
using System.Diagnostics;
using System.Dynamic;
using System.Globalization;
using Zeron.ZAttribute;
using Zeron.ZCore;
using Zeron.ZInterfaces;
using Zeron.ZServers;

namespace Zeron.Demand.ZServices
{
    [ServicesRep(ZmqApiName = "ProcessInfo", ZmqApiEnabled = true, ZmqNotifySubscriber = false)]

    /// <summary>
    /// ProcessInfo
    /// </summary>
    internal class ProcessInfo : IServices
    {
        /// <summary>
        /// OnRequest
        /// </summary>
        /// <param name="aJson"></param>
        /// <returns>Returns string.</returns>
        public string OnRequest(dynamic aJson)
        {
            dynamic response = new ExpandoObject();

            response.success = false;
            response.processes = null;

            try
            {
                List<dynamic> processLists = new();
                Process[] processes = Process.GetProcesses();

                foreach (Process process in processes)
                {
                    dynamic proc = new ExpandoObject();

                    try
                    {
                        proc.id = process.Id;
                        proc.process_name = process.ProcessName;
                        proc.machine_name = process.MachineName;
                        proc.responding = process.Responding;
                        proc.main_window_title = process.MainWindowTitle;
                        proc.base_priority = process.BasePriority;
                    }
                    catch (InvalidOperationException e)
                    {
                        if (DeployServer.AppDebug)
                        {
                            ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "ProcessInfo Error:{0}\n{1}", e.Message, e.StackTrace));
                        }
                    }
                    catch (NotSupportedException e)
                    {
                        if (DeployServer.AppDebug)
                        {
                            ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "ProcessInfo Error:{0}\n{1}", e.Message, e.StackTrace));
                        }
                    }

                    processLists.Add(proc);
                }

                if (processLists.Count > 0)
                {
                    response.success = true;
                    response.processes = processLists;
                }
            }
            catch (Exception e)
            {
                if (DeployServer.AppDebug)
                {
                    ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "ProcessInfo Error:{0}\n{1}", e.Message, e.StackTrace));
                }
            }

            return JsonConvert.SerializeObject(response);
        }

        /// <summary>
        /// OnRequestAsync
        /// </summary>
        /// <param name="aJson"></param>
        /// <returns>Returns string.</returns>
        public string OnRequestAsync(dynamic aJson)
        {
            return "";
        }

        /// <summary>
        /// OnSubscriber
        /// </summary>
        /// <param name="aJson"></param>
        /// <returns>Returns string.</returns>
        public string OnSubscriber(dynamic aJson)
        {
            return "";
        }

        /// <summary>
        /// OnSubscriberAsync
        /// </summary>
        /// <param name="aJson"></param>
        /// <returns>Returns string.</returns>
        public string OnSubscriberAsync(dynamic aJson)
        {
            return "";
        }

        /// <summary>
        /// OnNotifySubscriber
        /// </summary>
        /// <param name="aJson"></param>
        /// <param name="processedMsg"></param>
        /// <returns>Returns string.</returns>
        public string OnNotifySubscriber(dynamic aJson, string processedMsg)
        {
            return "";
        }
    }
}
