﻿using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using Zeron.Core;
using Zeron.Interfaces;

namespace Zeron.Demand.Services
{
    [ServicesRep(ZmqApiName = "ProcessInfo", ZmqApiEnabled = true)]

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

            List<dynamic> processLists = new List<dynamic>();

            Process[] processes = Process.GetProcesses();

            foreach (Process process in processes)
            {
                dynamic proc = new ExpandoObject();

                proc.id = process.Id;
                proc.process_name = process.ProcessName;
                proc.machine_name = process.MachineName;
                proc.main_window_title = process.MainWindowTitle;

                processLists.Add(proc);
            }

            if (processLists.Count > 0)
            {
                response.success = true;
                response.processes = processLists;
            }

            return JsonConvert.SerializeObject(response);
        }
    }
}