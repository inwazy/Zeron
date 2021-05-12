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
    [ServicesRep(ZmqApiName = "InstallSevenZip", ZmqApiEnabled = true, ZmqNotifySubscriber = false)]

    /// <summary>
    /// InstallGit
    /// </summary>
    internal class InstallSevenZip : IServices
    {
        // 7-Zip x64 url.
        const string m_ZipX64 = "https://www.7-zip.org/a/7z1900-x64.exe";

        // 7-Zip x86 url.
        const string m_ZipX86 = "https://www.7-zip.org/a/7z1900.exe";

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

            string zipX64 = m_ZipX64;
            string zipX86 = m_ZipX86;
            string zipUrl = zipX86;

            if (DeployServer.Is64BitEnv)
                zipUrl = zipX64;

            string zipFileName = Path.GetFileName(zipUrl);
            string zipFileSavePath = Path.Combine(Path.GetTempPath(), zipFileName);

            try
            {
                using (WebClient webClient = new WebClient())
                {
                    webClient.DownloadFile(new Uri(zipUrl), zipFileSavePath);
                    webClient.Dispose();

                    response.success = Process.Start(zipFileSavePath, "/S");
                }
            }
            catch (Exception e)
            {
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "InstallSevenZip Error:{0}\n{1}", e.Message, e.StackTrace));
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

            string zipX64 = m_ZipX64;
            string zipX86 = m_ZipX86;
            string zipUrl = zipX86;

            if (DeployServer.Is64BitEnv)
                zipUrl = zipX64;

            string zipFileName = Path.GetFileName(zipUrl);
            string zipFileSavePath = Path.Combine(Path.GetTempPath(), zipFileName);

            try
            {
                using (WebClient webClient = new WebClient())
                {
                    webClient.DownloadFileCompleted += (s, e) =>
                    {
                        string installToken = Md5.GenerateBase64(zipFileSavePath);

                        InstallQueuesType queuesType = new InstallQueuesType
                        {
                            FilePath = zipFileSavePath,
                            FileName = zipFileName,
                            Arguments = "/S"
                        };

                        InstallServer.AddQueues(installToken, queuesType);
                    };

                    webClient.DownloadFileAsync(new Uri(zipUrl), zipFileSavePath);

                    response.success = true;
                }
            }
            catch (Exception e)
            {
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "InstallSevenZip Async Error:{0}\n{1}", e.Message, e.StackTrace));
            }

            return JsonConvert.SerializeObject(response);
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
