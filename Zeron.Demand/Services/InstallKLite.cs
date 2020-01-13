using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Dynamic;
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

            string gitX64 = "https://files3.codecguide.com/K-Lite_Codec_Pack_1532_Mega.exe";
            string gitX86 = "https://files3.codecguide.com/K-Lite_Codec_Pack_1532_Mega.exe";
            string gitUrl = gitX86;

            if (DeployServer.Is64BitEnv)
                gitUrl = gitX64;

            string gitFileName = Path.GetFileName(gitUrl);
            string gitFileSavePath = Path.Combine(Path.GetTempPath(), gitFileName);

            try
            {
                using (WebClient webClient = new WebClient())
                {
                    webClient.DownloadFile(new Uri(gitUrl), gitFileSavePath);
                    webClient.Dispose();

                    response.success = Process.Start(gitFileSavePath, "/verysilent");
                }
            }
            catch (Exception e)
            {
                ZNLogger.Common.Error(string.Format("InstallKLite Error:{0}\n{1}", e.Message, e.StackTrace));
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

            string gitX64 = "https://files3.codecguide.com/K-Lite_Codec_Pack_1532_Mega.exe";
            string gitX86 = "https://files3.codecguide.com/K-Lite_Codec_Pack_1532_Mega.exe";
            string gitUrl = gitX86;

            if (DeployServer.Is64BitEnv)
                gitUrl = gitX64;

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
                            Arguments = "/verysilent"
                        };

                        InstallServer.AddQueues(installToken, queuesType);
                    };

                    webClient.DownloadFileAsync(new Uri(gitUrl), gitFileSavePath);

                    response.success = true;
                }
            }
            catch (Exception e)
            {
                ZNLogger.Common.Error(string.Format("InstallKLite Async Error:{0}\n{1}", e.Message, e.StackTrace));
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
