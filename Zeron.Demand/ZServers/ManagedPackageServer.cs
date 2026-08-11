// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using System.Collections.Specialized;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Timers;
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
        private readonly ManagedPackageDbImpl m_ManagedPackageImpl = new();

        // Catalog sync timer.
        private readonly System.Timers.Timer m_SyncTimer = new();

        // Runtime sync enabled.
        private bool m_SyncEnabled;

        // Config sync enabled.
        private static bool s_ConfigSyncEnabled = true;

        // Config sync interval.
        private static int s_ConfigSyncIntervalMs = 300000;

        /// <summary>
        /// LastCatalogSyncUtc - last successful sync timestamp.
        /// </summary>
        public static DateTime? LastCatalogSyncUtc
        {
            get;
            private set;
        }

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
        /// CatalogSyncIntervalMs
        /// </summary>
        public static int CatalogSyncIntervalMs
        {
            get => s_ConfigSyncIntervalMs;
            set => s_ConfigSyncIntervalMs = value;
        }

        /// <summary>
        /// LoadConfig
        /// </summary>
        /// <param name="aConfig"></param>
        /// <returns>Returns void.</returns>
        public override void LoadConfig(
            NameValueCollection aConfig)
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
                s_ConfigSyncEnabled = bool.Parse(aConfig["mp_catalog_sync_enabled"] ?? "true");
                s_ConfigSyncIntervalMs = int.Parse(aConfig["mp_catalog_sync_interval_ms"] ?? "300000", CultureInfo.InvariantCulture);
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

            m_SyncEnabled = s_ConfigSyncEnabled
                && !string.IsNullOrWhiteSpace(ReporterImpl.ServerUrl)
                && ReporterServer.Enabled;

            if (!m_SyncEnabled)
            {
                ZNLogger.Common.Info("ManagedPackageServer catalog sync disabled.");

                return;
            }

            m_SyncTimer.Elapsed += OnSyncTimer;
            m_SyncTimer.Interval = s_ConfigSyncIntervalMs > 0 ? s_ConfigSyncIntervalMs : 300000;
            m_SyncTimer.AutoReset = true;
            m_SyncTimer.Enabled = true;

            ZNLogger.Common.Info(string.Format(CultureInfo.InvariantCulture,
                "ManagedPackageServer catalog sync enabled. IntervalMs={0}", m_SyncTimer.Interval));

            _ = SyncCatalogAsync();
        }

        /// <summary>
        /// Stop
        /// </summary>
        /// <returns>Returns void.</returns>
        public void Stop()
        {
            m_SyncEnabled = false;

            try
            {
                m_SyncTimer.Stop();
                m_SyncTimer.Dispose();
            }
            catch (Exception)
            {
            }

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
        /// SyncCatalogAsync
        /// </summary>
        /// <returns>Returns applied package count (-1 on failure).</returns>
        public static async Task<int> SyncCatalogAsync()
        {
            if (string.IsNullOrWhiteSpace(ReporterImpl.ServerUrl))
            {
                return -1;
            }

            try
            {
                ManagedPackageCatalogSyncResponseType? response = await ReporterImpl.GetPackageCatalogAsync();

                if (response == null || !response.Success)
                {
                    ZNLogger.Common.Warn("ManagedPackageServer catalog sync failed.");

                    return -1;
                }

                int applied = ManagedPackageDbImpl.ApplyServerCatalog(response.Packages);
                LastCatalogSyncUtc = DateTime.UtcNow;

                ZNLogger.Common.Info(string.Format(CultureInfo.InvariantCulture,
                    "ManagedPackageServer catalog sync applied {0} package(s) (server total {1}).",
                    applied,
                    response.Packages.Count));

                return applied;
            }
            catch (Exception e)
            {
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture,
                    "ManagedPackageServer SyncCatalogAsync Error:{0}\n{1}", e.Message, e.StackTrace));

                return -1;
            }
        }

        /// <summary>
        /// OnSyncTimer
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        /// <returns>Returns void.</returns>
        private async void OnSyncTimer(
            object? sender,
            ElapsedEventArgs args)
        {
            if (!m_SyncEnabled)
            {
                return;
            }

            await SyncCatalogAsync();
        }

        /// <summary>
        /// GetRepoByName
        /// </summary>
        /// <param name="commands"></param>
        /// <returns>Returns ManagedPackageRepoType.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ManagedPackageRepoType GetRepoByName(
            ServicesSubCommandType? commands)
        {
            ManagedPackageRepoType? result = new();

            if (commands == null)
            {
                return result;
            }

            if (commands.PackageName != null
                && !string.IsNullOrEmpty(commands.PackageName))
            {
                ManagedPackageRepoType? repoResult = ManagedPackageDbImpl.GetSingleByName(commands.PackageName);

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
                    result.ScriptUnInstallBefore = repoResult.ScriptUnInstallBefore;
                    result.ScriptUnInstallAfter = repoResult.ScriptUnInstallAfter;
                    result.ScriptEngine = repoResult.ScriptEngine;
                    result.Sha256x86 = repoResult.Sha256x86;
                    result.Sha256x64 = repoResult.Sha256x64;
                    result.Source = repoResult.Source;
                }
            }

            return result;
        }
    }
}
