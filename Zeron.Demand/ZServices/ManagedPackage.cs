// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Newtonsoft.Json;
using System.Dynamic;
using System.Globalization;
using Zeron.Demand.ZCore.Type;
using Zeron.Demand.ZServers;
using Zeron.ZAttribute;
using Zeron.ZCore;
using Zeron.ZCore.Type;
using Zeron.ZInterfaces;
using Zeron.ZServers;

namespace Zeron.Demand.ZServices
{
    [ServicesRep(ZmqApiName = "ManagedPackage", ZmqApiEnabled = true, ZmqNotifySubscriber = false)]

    /// <summary>
    /// ManagedPackage
    /// </summary>
    internal class ManagedPackage : IServices
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

            try
            {
                string? command = Convert.ToString(aJson["Command"]);

                if (command == null || string.IsNullOrEmpty(command))
                {
                    return JsonConvert.SerializeObject(response);
                }

                ServicesSubCommandType? commands = Helper.BuildCommands(command);
                ManagedPackageRepoType? repo = ManagedPackageServer.GetRepoByName(commands);

                string? repoTempPath = ManagedPackageServer.RepoTempPath;

                if (repo != null && repo.Name != null)
                {
                    string? repoUrl = repo.Urlx86;
                    string? repoArgs = repo.CmdInstallx86;

                    if (DeployServer.Is64BitEnv)
                    {
                        repoUrl = !string.IsNullOrEmpty(repo.Urlx64) ? repo.Urlx64 : repoUrl;
                        repoArgs = !string.IsNullOrEmpty(repo.CmdInstallx64) ? repo.CmdInstallx64 : repoArgs;
                    }

                    string? repoBinaryFileName = Path.GetFileName(repoUrl);
                    string? repoBinaryTempFilePath = !string.IsNullOrEmpty(repoTempPath) ? repoTempPath : Path.GetTempPath();
                    string? repoBinaryFileLocalPath = Path.Combine(repoBinaryTempFilePath, repoBinaryFileName ?? "");

                    InstallQueuesType installQueuesTypeRepo = new()
                    {
                        RepoUrl = repoUrl,
                        FileName = repoBinaryFileName,
                        FilePath = repoBinaryFileLocalPath,
                        Arguments = repoArgs
                    };

                    if (InstallServer.AddQueues(commands.Option, installQueuesTypeRepo) > 0)
                    {
                        response.success = true;
                    }
                }
            }
            catch (Exception e)
            {
                if (DeployServer.AppDebug)
                {
                    ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "ManagedPackage Error:{0}\n{1}", e.Message, e.StackTrace));
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
