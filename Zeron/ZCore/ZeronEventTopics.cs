// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.ZCore
{
    /// <summary>
    /// ZeronEventTopics - stable event topic constants.
    /// </summary>
    public static class ZeronEventTopics
    {
        /// <summary>
        /// AgentConnected
        /// </summary>
        public const string AgentConnected = "agent.connected";

        /// <summary>
        /// AgentHeartbeat
        /// </summary>
        public const string AgentHeartbeat = "agent.heartbeat";

        /// <summary>
        /// AgentOffline
        /// </summary>
        public const string AgentOffline = "agent.offline";

        /// <summary>
        /// TaskDispatched
        /// </summary>
        public const string TaskDispatched = "task.dispatched";

        /// <summary>
        /// EventIngested
        /// </summary>
        public const string EventIngested = "event.ingested";

        /// <summary>
        /// CatalogRolledBack
        /// </summary>
        public const string CatalogRolledBack = "catalog.rolled_back";

        /// <summary>
        /// CatalogSyncRequested
        /// </summary>
        public const string CatalogSyncRequested = "catalog.sync_requested";

        /// <summary>
        /// CommandReceived
        /// </summary>
        public const string CommandReceived = "command.received";

        /// <summary>
        /// CommandCompleted
        /// </summary>
        public const string CommandCompleted = "command.completed";

        /// <summary>
        /// PackageCatalogSync
        /// </summary>
        public const string PackageCatalogSync = "package.catalog.sync";

        /// <summary>
        /// ScriptStarted
        /// </summary>
        public const string ScriptStarted = "script.started";

        /// <summary>
        /// ScriptCompleted
        /// </summary>
        public const string ScriptCompleted = "script.completed";

        /// <summary>
        /// ScriptFailed
        /// </summary>
        public const string ScriptFailed = "script.failed";
    }
}
