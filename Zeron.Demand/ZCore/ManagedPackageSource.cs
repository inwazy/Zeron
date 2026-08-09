// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.Demand.ZCore
{
    /// <summary>
    /// ManagedPackageSource - local Demand catalog row provenance.
    /// </summary>
    internal static class ManagedPackageSource
    {
        /// <summary>
        /// Server - managed by Zeron.Server catalog sync.
        /// </summary>
        public const string Server = "server";

        /// <summary>
        /// Local - Demand-only / locally overridden row (sync will not overwrite).
        /// </summary>
        public const string Local = "local";
    }
}
