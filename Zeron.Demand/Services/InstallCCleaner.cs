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
    [ServicesRep(ZmqApiName = "InstallCCleaner", ZmqApiEnabled = true, ZmqNotifySubscriber = false)]

    /// <summary>
    /// InstallCCleaner
    /// </summary>
    internal class InstallCCleaner : IServices
    {
        // CCleaner x64 url.
        const string m_CcleanerX64 = "https://download.ccleaner.com/ccsetup577.exe";

        // CCleaner x86 url.
        const string m_CcleanerX86 = "https://download.ccleaner.com/ccsetup577.exe";

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

            string ccleanerX64 = m_CcleanerX64;
            string ccleanerX86 = m_CcleanerX86;
            string ccleanerUrl = ccleanerX86;

            if (DeployServer.Is64BitEnv)
                ccleanerUrl = ccleanerX64;

            string ccleanerFileName = Path.GetFileName(ccleanerUrl);
            string ccleanerFileSavePath = Path.Combine(Path.GetTempPath(), ccleanerFileName);

            try
            {
                using (WebClient webClient = new WebClient())
                {
                    webClient.DownloadFile(new Uri(ccleanerUrl), ccleanerFileSavePath);
                    webClient.Dispose();

                    response.success = Process.Start(ccleanerFileSavePath, "/S");
                }
            }
            catch (Exception e)
            {
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "InstallCCleaner Error:{0}\n{1}", e.Message, e.StackTrace));
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

            string ccleanerX64 = m_CcleanerX64;
            string ccleanerX86 = m_CcleanerX86;
            string ccleanerUrl = ccleanerX86;

            if (DeployServer.Is64BitEnv)
                ccleanerUrl = ccleanerX64;

            string ccleanerFileName = Path.GetFileName(ccleanerUrl);
            string ccleanerFileSavePath = Path.Combine(Path.GetTempPath(), ccleanerFileName);

            try
            {
                using (WebClient webClient = new WebClient())
                {
                    webClient.DownloadFileCompleted += (s, e) =>
                    {
                        string installToken = Md5.GenerateBase64(ccleanerFileSavePath);

                        InstallQueuesType queuesType = new InstallQueuesType
                        {
                            FilePath = ccleanerFileSavePath,
                            FileName = ccleanerFileName,
                            Arguments = "/S"
                        };

                        InstallServer.AddQueues(installToken, queuesType);
                    };

                    webClient.DownloadFileAsync(new Uri(ccleanerUrl), ccleanerFileSavePath);

                    response.success = true;
                }
            }
            catch (Exception e)
            {
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "InstallCCleaner Async Error:{0}\n{1}", e.Message, e.StackTrace));
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
