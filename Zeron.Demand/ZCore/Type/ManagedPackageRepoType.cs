// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.Demand.ZCore.Type
{
    /// <summary>
    /// ManagedPackageRepoType
    /// </summary>
    internal class ManagedPackageRepoType
    {
        /// <summary>
        /// Name
        /// </summary>
        public string? Name
        {
            get;
            set;
        }

        /// <summary>
        /// Urlx86
        /// </summary>
        public string? Urlx86
        {
            get;
            set;
        }

        /// <summary>
        /// Urlx64
        /// </summary>
        public string? Urlx64
        {
            get;
            set;
        }

        /// <summary>
        /// CmdInstallx86
        /// </summary>
        public string? CmdInstallx86
        {
            get;
            set;
        }

        /// <summary>
        /// CmdInstallx64
        /// </summary>
        public string? CmdInstallx64
        {
            get;
            set;
        }

        /// <summary>
        /// CmdUnInstallx86
        /// </summary>
        public string? CmdUnInstallx86
        {
            get;
            set;
        }

        /// <summary>
        /// CmdUnInstallx64
        /// </summary>
        public string? CmdUnInstallx64
        {
            get;
            set;
        }

        /// <summary>
        /// ScriptInstallBefore
        /// </summary>
        public string? ScriptInstallBefore
        {
            get;
            set;
        }

        /// <summary>
        /// ScriptInstallAfter
        /// </summary>
        public string? ScriptInstallAfter
        {
            get;
            set;
        }

        /// <summary>
        /// ScriptUnInstallBefore
        /// </summary>
        public string? ScriptUnInstallBefore
        {
            get;
            set;
        }

        /// <summary>
        /// ScriptUnInstallAfter
        /// </summary>
        public string? ScriptUnInstallAfter
        {
            get;
            set;
        }

        /// <summary>
        /// ManagedPackageRepoType
        /// </summary>
        /// <returns>Returns void.</returns>
        public ManagedPackageRepoType()
        {
            Name = "";
            Urlx86 = "";
            Urlx64 = "";
            CmdInstallx86 = "";
            CmdInstallx64 = "";
            CmdUnInstallx86 = "";
            CmdUnInstallx64 = "";
            CmdUnInstallx86 = "";
            CmdUnInstallx64 = "";
            ScriptInstallBefore = "";
            ScriptInstallAfter = "";
            ScriptUnInstallBefore = "";
            ScriptUnInstallAfter = "";
        }
    }
}
