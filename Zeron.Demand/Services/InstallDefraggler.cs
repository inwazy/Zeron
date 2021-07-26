// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Net;
using Zeron.Core;
using Zeron.Core.Type;
using Zeron.Core.Utils;
using Zeron.Interfaces;
using Zeron.Servers;

namespace Zeron.Demand.Services
{
    [ServicesRep(ZmqApiName = "InstallDefraggler", ZmqApiEnabled = true, ZmqNotifySubscriber = false)]

    /// <summary>
    /// InstallDefraggler
    /// </summary>
    internal class InstallDefraggler : IServices
    {
        // Defraggler x64 url.
        const string m_DefragglerX64 = "https://download.ccleaner.com/dfsetup222.exe";

        // Defraggler x86 url.
        const string m_DefragglerX86 = "https://download.ccleaner.com/dfsetup222.exe";

        /// <summary>
        /// OnRequest
        /// </summary>
        /// <param name="aJson"></param>
        /// <returns>Returns string.</returns>
        public string OnRequest(dynamic aJson)
        {
            dynamic response = new ExpandoObject();

            response.success = false;
            response.result = null;

            string defragglerX64 = m_DefragglerX64;
            string defragglerX86 = m_DefragglerX86;
            string defragglerUrl = defragglerX86;

            if (DeployServer.Is64BitEnv)
                defragglerUrl = defragglerX64;

            string defragglerFileName = Path.GetFileName(defragglerUrl);
            string defragglerFileSavePath = Path.Combine(Path.GetTempPath(), defragglerFileName);

            try
            {
                using (WebClient webClient = new WebClient())
                {
                    webClient.DownloadFile(new Uri(defragglerUrl), defragglerFileSavePath);
                    webClient.Dispose();

                    response.success = Process.Start(defragglerFileSavePath, "/S");
                }
            }
            catch (Exception e)
            {
                if (DeployServer.AppDebug)
                {
                    ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "InstallDefraggler Error:{0}\n{1}", e.Message, e.StackTrace));
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
            dynamic response = new ExpandoObject();

            response.success = false;
            response.result = null;

            string defragglerX64 = m_DefragglerX64;
            string defragglerX86 = m_DefragglerX86;
            string defragglerUrl = defragglerX86;

            if (DeployServer.Is64BitEnv)
                defragglerUrl = defragglerX64;

            string defragglerFileName = Path.GetFileName(defragglerUrl);
            string defragglerFileSavePath = Path.Combine(Path.GetTempPath(), defragglerFileName);

            try
            {
                using (WebClient webClient = new WebClient())
                {
                    webClient.DownloadFileCompleted += (s, e) =>
                    {
                        string installToken = Md5.GenerateBase64(defragglerFileSavePath);

                        InstallQueuesType queuesType = new InstallQueuesType
                        {
                            FilePath = defragglerFileSavePath,
                            FileName = defragglerFileName,
                            Arguments = "/S"
                        };

                        InstallServer.AddQueues(installToken, queuesType);
                    };

                    webClient.DownloadFileAsync(new Uri(defragglerUrl), defragglerFileSavePath);

                    response.success = true;
                }
            }
            catch (Exception e)
            {
                if (DeployServer.AppDebug)
                {
                    ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "InstallDefraggler Async Error:{0}\n{1}", e.Message, e.StackTrace));
                }
            }

            return JsonConvert.SerializeObject(response);
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
