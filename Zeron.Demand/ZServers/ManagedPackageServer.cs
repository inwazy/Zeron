// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using System.Collections.Specialized;
using System.Globalization;
using System.Runtime.CompilerServices;
using Zeron.Demand.ZCore.Type;
using Zeron.Demand.ZServers.Impls;
using Zeron.ZCore;
using Zeron.ZCore.Container;
using Zeron.ZCore.Foundation;
using Zeron.ZCore.Type;
using Zeron.ZInterfaces;

namespace Zeron.Demand.ZServers
{
    /// <summary>
    /// ManagedPackageServer
    /// </summary>
    internal class ManagedPackageServer : ConfigurationTable, IServer
    {
        // ManagedPackageImpl instance.
        private readonly ManagedPackageImpl m_ManagedPackageImpl = new();

        /// <summary>
        /// DbSourceFile
        /// </summary>
        public static string? DbSourceFile
        {
            get;
            set;
        }

        /// <summary>
        /// RepoTempPath
        /// </summary>

        public static string? RepoTempPath
        {
            get;
            set;
        }

        /// <summary>
        /// LoadConfig
        /// </summary>
        /// <param name="aConfig"></param>
        /// <returns>Returns void.</returns>
        public override void LoadConfig(NameValueCollection aConfig)
        {
            if (aConfig == null)
            {
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "ManagedPackageServer Config Empty"));

                return;
            }

            try
            {
                DbSourceFile = aConfig["mp_db_source_file"];
                RepoTempPath = aConfig["mp_repo_temp_path"];
            }
            catch (Exception e)
            {
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "ManagedPackageServer Config Error:{0}\n{1}", e.Message, e.StackTrace));
            }
        }

        /// <summary>
        /// Initialize
        /// </summary>
        /// <returns>Returns void.</returns>
        public void Initialize()
        {
            try
            {
                m_ManagedPackageImpl.PrepareDatabase(DbSourceFile);
            }
            catch (Exception e)
            {
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "ManagedPackageServer Error:{0}\n{1}", e.Message, e.StackTrace));
            }
        }

        /// <summary>
        /// Stop
        /// </summary>
        /// <returns>Returns void.</returns>
        public void Stop()
        {
            try
            {
                m_ManagedPackageImpl.Dispose();
            }
            catch (Exception e)
            {
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "ManagedPackageServer Error:{0}\n{1}", e.Message, e.StackTrace));
            }

            ServerIntegrate.FinishSingleStop();
        }

        /// <summary>
        /// GetRepoByName
        /// </summary>
        /// <param name="commands"></param>
        /// <returns>Returns ManagedPackageRepoType.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ManagedPackageRepoType GetRepoByName(ServicesSubCommandType? commands)
        {
            ManagedPackageRepoType? result = new();

            if (commands == null)
            {
                return result;
            }

            if (commands.PackageName != null 
                && !string.IsNullOrEmpty(commands.PackageName))
            {
                ManagedPackageRepoType? repoResult = ManagedPackageImpl.GetSingleByName(commands.PackageName);

                if (repoResult != null)
                {
                    result.Name = repoResult.Name;
                    result.Urlx86 = repoResult.Urlx86;
                    result.Urlx64 = repoResult.Urlx64;
                    result.CmdInstallx86 = repoResult.CmdInstallx86;
                    result.CmdInstallx64 = repoResult.CmdInstallx64;
                    result.CmdUnInstallx86 = repoResult.CmdUnInstallx86;
                    result.CmdUnInstallx64 = repoResult.CmdUnInstallx64;
                    result.ScriptInstallBefore = repoResult.ScriptInstallBefore;
                    result.ScriptInstallAfter = repoResult.ScriptInstallAfter;
                    result.ScriptUnInstallBefore = repoResult.ScriptInstallBefore;
                    result.ScriptUnInstallAfter = repoResult.ScriptUnInstallAfter;
                }
            }

            return result;
        }
    }
}
