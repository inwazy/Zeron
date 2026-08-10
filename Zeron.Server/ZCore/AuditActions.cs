// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.Server.ZCore
{
    /// <summary>
    /// AuditActions - stable action names for AuditLog.
    /// </summary>
    public static class AuditActions
    {
        /// <summary>
        /// CatalogCreate
        /// </summary>
        public const string CatalogCreate = "catalog.create";

        /// <summary>
        /// CatalogUpdate
        /// </summary>
        public const string CatalogUpdate = "catalog.update";

        /// <summary>
        /// CatalogDelete
        /// </summary>
        public const string CatalogDelete = "catalog.delete";

        /// <summary>
        /// PackageDeploy
        /// </summary>
        public const string PackageDeploy = "package.deploy";

        /// <summary>
        /// PackageSelfDeploy
        /// </summary>
        public const string PackageSelfDeploy = "package.self_deploy";

        /// <summary>
        /// BindingCreate
        /// </summary>
        public const string BindingCreate = "binding.create";

        /// <summary>
        /// BindingDelete
        /// </summary>
        public const string BindingDelete = "binding.delete";

        /// <summary>
        /// PackageOverride
        /// </summary>
        public const string PackageOverride = "package.override";

        /// <summary>
        /// PackageClearOverride
        /// </summary>
        public const string PackageClearOverride = "package.clear-override";

        /// <summary>
        /// PackageCatalogSync
        /// </summary>
        public const string PackageCatalogSync = "package.catalog.sync";

        /// <summary>
        /// CatalogSyncPush
        /// </summary>
        public const string CatalogSyncPush = "catalog.sync.push";
    }
}
