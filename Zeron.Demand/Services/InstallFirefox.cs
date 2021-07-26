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
    [ServicesRep(ZmqApiName = "InstallFirefox", ZmqApiEnabled = true, ZmqNotifySubscriber = false)]

    /// <summary>
    /// InstallFirefox
    /// </summary>
    internal class InstallFirefox : IServices
    {
        // Firefox x64 url.
        const string m_FirefoxX64 = "https://download-installer.cdn.mozilla.net/pub/firefox/releases/88.0.1/win64/en-US/Firefox%20Setup%2088.0.1.msi";

        // Firefox x86 url.
        const string m_FirefoxX86 = "https://download-installer.cdn.mozilla.net/pub/firefox/releases/88.0.1/win32/en-US/Firefox%20Setup%2088.0.1.msi";

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

            string firefoxX64 = m_FirefoxX64;
            string firefoxX86 = m_FirefoxX86;
            string firefoxUrl = firefoxX86;

            if (DeployServer.Is64BitEnv)
                firefoxUrl = firefoxX64;

            string firefoxFileName = Path.GetFileName(firefoxUrl);
            string firefoxFileSavePath = Path.Combine(Path.GetTempPath(), firefoxFileName);

            try
            {
                using (WebClient webClient = new WebClient())
                {
                    webClient.DownloadFile(new Uri(firefoxUrl), firefoxFileSavePath);
                    webClient.Dispose();

                    response.success = Process.Start(firefoxFileSavePath, "/qn");
                }
            }
            catch (Exception e)
            {
                if (DeployServer.AppDebug)
                {
                    ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "InstallFirefox Error:{0}\n{1}", e.Message, e.StackTrace));
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

            string firefoxX64 = m_FirefoxX64;
            string firefoxX86 = m_FirefoxX86;
            string firefoxUrl = firefoxX86;

            if (DeployServer.Is64BitEnv)
                firefoxUrl = firefoxX64;

            string firefoxFileName = Path.GetFileName(firefoxUrl);
            string firefoxFileSavePath = Path.Combine(Path.GetTempPath(), firefoxFileName);

            try
            {
                using (WebClient webClient = new WebClient())
                {
                    webClient.DownloadFileCompleted += (s, e) =>
                    {
                        string installToken = Md5.GenerateBase64(firefoxFileSavePath);

                        InstallQueuesType queuesType = new InstallQueuesType
                        {
                            FilePath = firefoxFileSavePath,
                            FileName = firefoxFileName,
                            Arguments = "/qn"
                        };

                        InstallServer.AddQueues(installToken, queuesType);
                    };

                    webClient.DownloadFileAsync(new Uri(firefoxUrl), firefoxFileSavePath);

                    response.success = true;
                }
            }
            catch (Exception e)
            {
                if (DeployServer.AppDebug)
                {
                    ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "InstallFirefox Async Error:{0}\n{1}", e.Message, e.StackTrace));
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
