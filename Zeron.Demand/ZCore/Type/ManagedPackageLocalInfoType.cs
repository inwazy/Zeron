// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.Demand.ZCore.Type
{
    /// <summary>
    /// ManagedPackageLocalInfoType - local catalog row summary.
    /// </summary>
    internal class ManagedPackageLocalInfoType
    {
        /// <summary>
        /// Name
        /// </summary>
        public string Name
        {
            get;
            set;
        } = "";

        /// <summary>
        /// Source - server or local.
        /// </summary>
        public string Source
        {
            get;
            set;
        } = "";

        /// <summary>
        /// Enabled
        /// </summary>
        public bool Enabled
        {
            get;
            set;
        }
    }
}
