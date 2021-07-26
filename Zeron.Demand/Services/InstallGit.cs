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
    [ServicesRep(ZmqApiName = "InstallGit", ZmqApiEnabled = true, ZmqNotifySubscriber = false)]

    /// <summary>
    /// InstallGit
    /// </summary>
    internal class InstallGit : IServices
    {
        // Git x64 url.
        const string m_Gitx64 = "https://github.com/git-for-windows/git/releases/download/v2.31.1.windows.1/Git-2.31.1-64-bit.exe";

        // Git x86 url.
        const string m_Gitx86 = "https://github.com/git-for-windows/git/releases/download/v2.31.1.windows.1/Git-2.31.1-32-bit.exe";

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

            string gitX64 = m_Gitx64;
            string gitX86 = m_Gitx86;
            string gitUrl = gitX86;

            if (DeployServer.Is64BitEnv)
            {
                gitUrl = gitX64;
            }

            string gitFileName = Path.GetFileName(gitUrl);
            string gitFileSavePath = Path.Combine(Path.GetTempPath(), gitFileName);

            try
            {
                using (WebClient webClient = new WebClient())
                {
                    webClient.DownloadFile(new Uri(gitUrl), gitFileSavePath);
                    webClient.Dispose();

                    response.success = Process.Start(gitFileSavePath, "/SILENT");
                }
            }
            catch (Exception e)
            {
                if (DeployServer.AppDebug)
                {
                    ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "InstallGit Error:{0}\n{1}", e.Message, e.StackTrace));
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

            string gitX64 = m_Gitx64;
            string gitX86 = m_Gitx86;
            string gitUrl = gitX86;

            if (DeployServer.Is64BitEnv)
            {
                gitUrl = gitX64;
            }

            string gitFileName = Path.GetFileName(gitUrl);
            string gitFileSavePath = Path.Combine(Path.GetTempPath(), gitFileName);

            try
            {
                using (WebClient webClient = new WebClient())
                {
                    webClient.DownloadFileCompleted += (s, e) =>
                    {
                        string installToken = Md5.GenerateBase64(gitFileSavePath);

                        InstallQueuesType queuesType = new InstallQueuesType
                        {
                            FilePath = gitFileSavePath,
                            FileName = gitFileName,
                            Arguments = "/SILENT"
                        };

                        InstallServer.AddQueues(installToken, queuesType);
                    };

                    webClient.DownloadFileAsync(new Uri(gitUrl), gitFileSavePath);

                    response.success = true;
                }
            }
            catch (Exception e)
            {
                if (DeployServer.AppDebug)
                {
                    ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "InstallGit Async Error:{0}\n{1}", e.Message, e.StackTrace));
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
