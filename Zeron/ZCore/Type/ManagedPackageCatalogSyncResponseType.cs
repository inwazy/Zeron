// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.ZCore.Type
{
    /// <summary>
    /// ManagedPackageCatalogSyncResponseType - payload agents pull from Server.
    /// </summary>
    public sealed class ManagedPackageCatalogSyncResponseType
    {
        /// <summary>
        /// Success
        /// </summary>
        public bool Success
        {
            get;
            set;
        } = true;

        /// <summary>
        /// GeneratedAt
        /// </summary>
        public DateTime GeneratedAt
        {
            get;
            set;
        }

        /// <summary>
        /// Packages
        /// </summary>
        public List<ManagedPackageInfoType> Packages
        {
            get;
            set;
        } = [];
    }
}
