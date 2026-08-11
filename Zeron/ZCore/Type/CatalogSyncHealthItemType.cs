// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.ZCore.Type
{
    /// <summary>
    /// CatalogSyncHealthItemType - per-agent ManagedPackage catalog sync health.
    /// </summary>
    public sealed class CatalogSyncHealthItemType
    {
        /// <summary>
        /// AgentKey
        /// </summary>
        public string? AgentKey
        {
            get;
            set;
        }

        /// <summary>
        /// MachineName
        /// </summary>
        public string? MachineName
        {
            get;
            set;
        }

        /// <summary>
        /// Status - online, offline, disabled
        /// </summary>
        public string? Status
        {
            get;
            set;
        }

        /// <summary>
        /// LastCatalogSyncAt
        /// </summary>
        public DateTime? LastCatalogSyncAt
        {
            get;
            set;
        }

        /// <summary>
        /// LastHeartbeatAt
        /// </summary>
        public DateTime? LastHeartbeatAt
        {
            get;
            set;
        }

        /// <summary>
        /// SyncState - healthy, stale, never, offline, failed
        /// </summary>
        public string SyncState
        {
            get;
            set;
        } = "never";

        /// <summary>
        /// DiagnosticMessage
        /// </summary>
        public string? DiagnosticMessage
        {
            get;
            set;
        }

        /// <summary>
        /// LastFailedSyncAt - most recent failed package.catalog.sync audit for this agent.
        /// </summary>
        public DateTime? LastFailedSyncAt
        {
            get;
            set;
        }

        /// <summary>
        /// AgeMinutes - minutes since last successful sync (null when never).
        /// </summary>
        public int? AgeMinutes
        {
            get;
            set;
        }
    }
}
