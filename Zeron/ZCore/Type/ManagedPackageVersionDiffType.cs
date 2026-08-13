// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.ZCore.Type
{
    /// <summary>
    /// ManagedPackageVersionDiffType - field-level catalog version comparison.
    /// </summary>
    public sealed class ManagedPackageVersionDiffType
    {
        /// <summary>
        /// PackageId
        /// </summary>
        public string? PackageId
        {
            get;
            set;
        }

        /// <summary>
        /// PackageName
        /// </summary>
        public string? PackageName
        {
            get;
            set;
        }

        /// <summary>
        /// LeftLabel - e.g. v2 or current
        /// </summary>
        public string LeftLabel
        {
            get;
            set;
        } = "";

        /// <summary>
        /// RightLabel
        /// </summary>
        public string RightLabel
        {
            get;
            set;
        } = "";

        /// <summary>
        /// LeftVersionNumber - null when left is live current
        /// </summary>
        public int? LeftVersionNumber
        {
            get;
            set;
        }

        /// <summary>
        /// RightVersionNumber - null when right is live current
        /// </summary>
        public int? RightVersionNumber
        {
            get;
            set;
        }

        /// <summary>
        /// LeftIsCurrent
        /// </summary>
        public bool LeftIsCurrent
        {
            get;
            set;
        }

        /// <summary>
        /// RightIsCurrent
        /// </summary>
        public bool RightIsCurrent
        {
            get;
            set;
        }

        /// <summary>
        /// ChangedCount
        /// </summary>
        public int ChangedCount
        {
            get;
            set;
        }

        /// <summary>
        /// Fields - all compared fields (including unchanged)
        /// </summary>
        public List<ManagedPackageVersionDiffFieldType> Fields
        {
            get;
            set;
        } = [];
    }
}
