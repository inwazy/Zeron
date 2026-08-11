// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.ZCore.Type
{
    /// <summary>
    /// ManagedPackageRollbackRequestType
    /// </summary>
    public class ManagedPackageRollbackRequestType
    {
        /// <summary>
        /// VersionNumber - historical version to restore onto the live package.
        /// </summary>
        public int VersionNumber
        {
            get;
            set;
        }
    }
}
