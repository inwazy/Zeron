using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Net;
using Zeron.Core;
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
        /// <summary>
        /// OnRequest
        /// </summary>
        /// <param name="aJson"></param>
        /// <returns>Returns string.</returns>
        public string OnRequest(dynamic aJson)
        {
            dynamic response = new ExpandoObject();

            response.success = false;

            string gitX64 = "https://github.com/git-for-windows/git/releases/download/v2.23.0.windows.1/Git-2.23.0-64-bit.exe";
            string gitX86 = "https://github.com/git-for-windows/git/releases/download/v2.23.0.windows.1/Git-2.23.0-32-bit.exe";
            string gitUrl = gitX86;

            if (DeployServer.Is64BitEnv)
                gitUrl = gitX64;

            string gitFileName = Path.GetFileName(gitUrl);
            string gitFileSavePath = Path.Combine(Path.GetTempPath(), gitFileName);

            try
            {
                using (WebClient webClient = new WebClient())
                {
                    webClient.DownloadFile(gitUrl, gitFileSavePath);
                    webClient.Dispose();

                    response.success = Process.Start(gitFileSavePath, "/SILENT");
                }
            }
            catch (Exception e)
            {
                ZNLogger.Common.Error(string.Format("InstallGit Error:{0}\n{1}", e.Message, e.StackTrace));
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
