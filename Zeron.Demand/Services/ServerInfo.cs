// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using Zeron.Core;
using Zeron.Interfaces;
using Zeron.Servers;

namespace Zeron.Demand.Services
{
    [ServicesRep(ZmqApiName = "ServerInfo", ZmqApiEnabled = true, ZmqNotifySubscriber = false)]

    /// <summary>
    /// ServerInfo
    /// </summary>
    internal class ServerInfo : IServices
    {
        /// <summary>
        /// OnRequest
        /// </summary>
        /// <param name="aJson"></param>
        /// <returns>Returns string.</returns>
        public string OnRequest(dynamic aJson)
        {
            dynamic response = new ExpandoObject();

            try
            {
                response.success = true;
                response.app_name = DeployServer.AppTitle;
                response.app_debug = DeployServer.AppDebug.ToString();
                response.app_service_name = DeployServer.ServiceName;
                response.app_service_description = DeployServer.ServiceDescription;
                response.app_service_displayname = DeployServer.ServiceDisplayName;
                response.app_service_instancename = DeployServer.ServiceInstanceName;
                response.machine_name = Environment.MachineName;
                response.os_version = Environment.OSVersion.ToString();
                response.os_x64 = Environment.Is64BitOperatingSystem;
                response.user_name = Environment.UserName;
                response.user_domain_name = Environment.UserDomainName;
                response.user_interactive = Environment.UserInteractive;
                response.sys_directory = Environment.SystemDirectory;
            }
            catch (Exception e)
            {
                if (DeployServer.AppDebug)
                {
                    ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "ServerInfo Error:{0}\n{1}", e.Message, e.StackTrace));
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
