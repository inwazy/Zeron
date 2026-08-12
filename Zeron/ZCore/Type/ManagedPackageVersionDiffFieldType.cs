// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.ZCore.Type
{
    /// <summary>
    /// ManagedPackageVersionDiffFieldType - one compared catalog field.
    /// </summary>
    public sealed class ManagedPackageVersionDiffFieldType
    {
        /// <summary>
        /// Field - property name
        /// </summary>
        public string Field
        {
            get;
            set;
        } = "";

        /// <summary>
        /// Left - left-side value (display string)
        /// </summary>
        public string? Left
        {
            get;
            set;
        }

        /// <summary>
        /// Right - right-side value (display string)
        /// </summary>
        public string? Right
        {
            get;
            set;
        }

        /// <summary>
        /// Changed
        /// </summary>
        public bool Changed
        {
            get;
            set;
        }
    }
}
