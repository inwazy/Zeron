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
    [ServicesRep(ZmqApiName = "InstallKLite", ZmqApiEnabled = true, ZmqNotifySubscriber = false)]

    /// <summary>
    /// InstallKLite
    /// </summary>
    internal class InstallKLite : IServices
    {
        // K-Lite x64 url.
        const string m_KliteX64 = "https://files2.codecguide.com/K-Lite_Codec_Pack_1635_Mega.exe";

        // K-Lite x86 url.
        const string m_KliteX86 = "https://files2.codecguide.com/K-Lite_Codec_Pack_1635_Mega.exe";

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

            string kliteX64 = m_KliteX64;
            string kliteX86 = m_KliteX86;
            string kliteUrl = kliteX86;

            if (DeployServer.Is64BitEnv)
            {
                kliteUrl = kliteX64;
            }

            string kliteFileName = Path.GetFileName(kliteUrl);
            string kliteFileSavePath = Path.Combine(Path.GetTempPath(), kliteFileName);

            try
            {
                using (WebClient webClient = new WebClient())
                {
                    webClient.DownloadFile(new Uri(kliteUrl), kliteFileSavePath);
                    webClient.Dispose();

                    response.success = Process.Start(kliteFileSavePath, "/verysilent");
                }
            }
            catch (Exception e)
            {
                if (DeployServer.AppDebug)
                {
                    ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "InstallKLite Error:{0}\n{1}", e.Message, e.StackTrace));
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

            string kliteX64 = m_KliteX64;
            string kliteX86 = m_KliteX86;
            string kliteUrl = kliteX86;

            if (DeployServer.Is64BitEnv)
            {
                kliteUrl = kliteX64;
            }

            string kliteFileName = Path.GetFileName(kliteUrl);
            string kliteFileSavePath = Path.Combine(Path.GetTempPath(), kliteFileName);

            try
            {
                using (WebClient webClient = new WebClient())
                {
                    webClient.DownloadFileCompleted += (s, e) =>
                    {
                        string installToken = Md5.GenerateBase64(kliteFileSavePath);

                        InstallQueuesType queuesType = new InstallQueuesType
                        {
                            FilePath = kliteFileSavePath,
                            FileName = kliteFileName,
                            Arguments = "/verysilent"
                        };

                        InstallServer.AddQueues(installToken, queuesType);
                    };

                    webClient.DownloadFileAsync(new Uri(kliteUrl), kliteFileSavePath);

                    response.success = true;
                }
            }
            catch (Exception e)
            {
                if (DeployServer.AppDebug)
                {
                    ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "InstallKLite Async Error:{0}\n{1}", e.Message, e.StackTrace));
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
