// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Newtonsoft.Json;
using System.Dynamic;
using System.Globalization;
using Zeron.Demand.ZCore;
using Zeron.Demand.ZCore.Type;
using Zeron.Demand.ZServers;
using Zeron.Demand.ZServers.Impls;
using Zeron.ZAttribute;
using Zeron.ZCore;
using Zeron.ZCore.Type;
using Zeron.ZInterfaces;
using Zeron.ZServers;

namespace Zeron.Demand.ZServices
{
    [ServicesRep(ZmqApiName = "ManagedPackage", ZmqApiEnabled = true, ZmqNotifySubscriber = false, ApiScope = "install")]

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
        public string OnRequest(
            dynamic aJson)
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

                if (commands?.Option == null || string.IsNullOrEmpty(commands.Option))
                {
                    return JsonConvert.SerializeObject(response);
                }

                if (commands.Option.Equals("status", StringComparison.OrdinalIgnoreCase))
                {
                    response.success = true;
                    response.result = InstallJobTracker.GetStatus();

                    return JsonConvert.SerializeObject(response);
                }

                if (commands.Option.Equals("list", StringComparison.OrdinalIgnoreCase))
                {
                    response.success = true;
                    response.result = ManagedPackageDbImpl.ListPackages();

                    return JsonConvert.SerializeObject(response);
                }

                if (commands.Option.Equals("sync", StringComparison.OrdinalIgnoreCase))
                {
                    int applied = ManagedPackageServer.SyncCatalogAsync().GetAwaiter().GetResult();
                    bool synced = applied >= 0;
                    response.success = synced;
                    response.result = new
                    {
                        synced,
                        applied,
                        lastCatalogSyncAt = ManagedPackageServer.LastCatalogSyncUtc
                    };

                    InstallEventPublisher.PublishObject("package.catalog.sync", new
                    {
                        success = synced,
                        synced,
                        applied,
                        lastCatalogSyncAt = ManagedPackageServer.LastCatalogSyncUtc
                    });

                    return JsonConvert.SerializeObject(response);
                }

                if (commands.Option.Equals("override", StringComparison.OrdinalIgnoreCase))
                {
                    bool updated = ManagedPackageDbImpl.MarkLocalOverride(commands.PackageName);
                    response.success = updated;
                    response.result = new
                    {
                        package = commands.PackageName,
                        source = ManagedPackageSource.Local,
                        overridden = updated
                    };

                    InstallEventPublisher.PublishObject("package.override", new
                    {
                        success = updated,
                        package = commands.PackageName,
                        source = ManagedPackageSource.Local,
                        overridden = updated
                    });

                    return JsonConvert.SerializeObject(response);
                }

                if (commands.Option.Equals("clear-override", StringComparison.OrdinalIgnoreCase))
                {
                    bool cleared = ManagedPackageDbImpl.ClearLocalOverride(commands.PackageName);
                    response.success = cleared;
                    response.result = new
                    {
                        package = commands.PackageName,
                        cleared
                    };

                    InstallEventPublisher.PublishObject("package.clear-override", new
                    {
                        success = cleared,
                        package = commands.PackageName,
                        cleared
                    });

                    return JsonConvert.SerializeObject(response);
                }

                ManagedPackageRepoType? repo = ManagedPackageServer.GetRepoByName(commands);
                string? repoTempPath = ManagedPackageServer.RepoTempPath;

                if (repo == null || repo.Name == null || string.IsNullOrEmpty(repo.Name))
                {
                    return JsonConvert.SerializeObject(response);
                }

                bool isUninstall = commands.Option.Equals("uninstall", StringComparison.OrdinalIgnoreCase);
                bool isInstall = commands.Option.Equals("install", StringComparison.OrdinalIgnoreCase);

                if (!isInstall && !isUninstall)
                {
                    return JsonConvert.SerializeObject(response);
                }

                string? repoUrl = repo.Urlx86;
                string? repoArgs = isUninstall ? repo.CmdUnInstallx86 : repo.CmdInstallx86;
                string? scriptBefore = isUninstall ? repo.ScriptUnInstallBefore : repo.ScriptInstallBefore;
                string? scriptAfter = isUninstall ? repo.ScriptUnInstallAfter : repo.ScriptInstallAfter;
                string? expectedSha = repo.Sha256x86;

                if (DeployServer.Is64BitEnv)
                {
                    repoUrl = !string.IsNullOrEmpty(repo.Urlx64) ? repo.Urlx64 : repoUrl;
                    expectedSha = !string.IsNullOrEmpty(repo.Sha256x64) ? repo.Sha256x64 : expectedSha;

                    if (isUninstall)
                    {
                        repoArgs = !string.IsNullOrEmpty(repo.CmdUnInstallx64) ? repo.CmdUnInstallx64 : repoArgs;
                    }
                    else
                    {
                        repoArgs = !string.IsNullOrEmpty(repo.CmdInstallx64) ? repo.CmdInstallx64 : repoArgs;
                    }
                }

                if (!string.IsNullOrEmpty(commands.Args))
                {
                    repoArgs = string.IsNullOrEmpty(repoArgs)
                        ? commands.Args
                        : repoArgs + " " + commands.Args;
                }

                string? repoBinaryFileName = Path.GetFileName(repoUrl);
                string? repoBinaryTempFilePath = !string.IsNullOrEmpty(repoTempPath) ? repoTempPath : Path.GetTempPath();
                string? repoBinaryFileLocalPath = Path.Combine(repoBinaryTempFilePath, repoBinaryFileName ?? "");

                InstallQueuesType installQueuesTypeRepo = new()
                {
                    RepoUrl = repoUrl,
                    FileName = repoBinaryFileName,
                    FilePath = repoBinaryFileLocalPath,
                    Arguments = repoArgs,
                    PackageName = repo.Name,
                    Operation = commands.Option,
                    ScriptBefore = scriptBefore,
                    ScriptAfter = scriptAfter,
                    ScriptEngine = string.IsNullOrWhiteSpace(repo.ScriptEngine) ? "powershell" : repo.ScriptEngine.Trim(),
                    AssignmentId = RemoteCommandContext.AssignmentId,
                    ExpectedSha256 = string.IsNullOrWhiteSpace(expectedSha) ? null : expectedSha.Trim().ToLowerInvariant()
                };

                if (InstallServer.AddQueues(commands.Option, installQueuesTypeRepo) > 0)
                {
                    response.success = true;
                    response.result = new
                    {
                        queued = true,
                        package = repo.Name,
                        operation = commands.Option,
                        assignmentId = installQueuesTypeRepo.AssignmentId,
                        queueCount = InstallServer.GetQueueCount()
                    };
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
        public string OnRequestAsync(
            dynamic aJson)
        {
            return "";
        }

        /// <summary>
        /// OnSubscriber
        /// </summary>
        /// <param name="aJson"></param>
        /// <returns>Returns string.</returns>
        public string OnSubscriber(
            dynamic aJson)
        {
            return "";
        }

        /// <summary>
        /// OnSubscriberAsync
        /// </summary>
        /// <param name="aJson"></param>
        /// <returns>Returns string.</returns>
        public string OnSubscriberAsync(
            dynamic aJson)
        {
            return "";
        }

        /// <summary>
        /// OnNotifySubscriber
        /// </summary>
        /// <param name="aJson"></param>
        /// <param name="processedMsg"></param>
        /// <returns>Returns string.</returns>
        public string OnNotifySubscriber(
            dynamic aJson, 
            string processedMsg)
        {
            return "";
        }
    }
}
