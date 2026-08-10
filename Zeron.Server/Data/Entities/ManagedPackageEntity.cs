// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.Server.Data.Entities
{
    /// <summary>
    /// ManagedPackageEntity - central package catalog entry.
    /// </summary>
    public class ManagedPackageEntity
    {
        /// <summary>
        /// Id
        /// </summary>
        public Guid Id
        {
            get;
            set;
        }

        /// <summary>
        /// Name - unique package key (lowercase).
        /// </summary>
        public string Name
        {
            get;
            set;
        } = "";

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
        /// Sha256x86 - optional SHA-256 hex for x86 installer.
        /// </summary>
        public string? Sha256x86
        {
            get;
            set;
        }

        /// <summary>
        /// Sha256x64 - optional SHA-256 hex for x64 installer.
        /// </summary>
        public string? Sha256x64
        {
            get;
            set;
        }

        /// <summary>
        /// IsEnabled - maps to Demand status=1 when synced.
        /// </summary>
        public bool IsEnabled
        {
            get;
            set;
        } = true;

        /// <summary>
        /// CreatedAt
        /// </summary>
        public DateTime CreatedAt
        {
            get;
            set;
        }

        /// <summary>
        /// UpdatedAt
        /// </summary>
        public DateTime UpdatedAt
        {
            get;
            set;
        }
    }
}
