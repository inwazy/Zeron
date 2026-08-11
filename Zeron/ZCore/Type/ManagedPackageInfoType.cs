// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.ZCore.Type
{
    /// <summary>
    /// ManagedPackageInfoType
    /// </summary>
    public sealed class ManagedPackageInfoType
    {
        /// <summary>
        /// Id
        /// </summary>
        public string? Id
        {
            get;
            set;
        }

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
        /// ScriptEngine - Script Host engine id for before/after hooks (default powershell).
        /// </summary>
        public string? ScriptEngine
        {
            get;
            set;
        }

        /// <summary>
        /// Sha256x86
        /// </summary>
        public string? Sha256x86
        {
            get;
            set;
        }

        /// <summary>
        /// Sha256x64
        /// </summary>
        public string? Sha256x64
        {
            get;
            set;
        }

        /// <summary>
        /// IsEnabled
        /// </summary>
        public bool IsEnabled
        {
            get;
            set;
        } = true;

        /// <summary>
        /// UpdatedAt
        /// </summary>
        public DateTime? UpdatedAt
        {
            get;
            set;
        }
    }
}
