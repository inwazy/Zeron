// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.ZCore.Type
{
    /// <summary>
    /// CatalogSyncPushRequestType - request agents to pull ManagedPackage catalog.
    /// </summary>
    public class CatalogSyncPushRequestType
    {
        /// <summary>
        /// AgentKeys - when empty, targets are chosen by OnlyUnhealthy / online filter.
        /// </summary>
        public List<string>? AgentKeys
        {
            get;
            set;
        }

        /// <summary>
        /// OnlyUnhealthy - when AgentKeys empty, push only never/stale/failed online agents (default true).
        /// When false, push all online agents.
        /// </summary>
        public bool OnlyUnhealthy
        {
            get;
            set;
        } = true;
    }
}
