// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.ZCore.Type
{
    /// <summary>
    /// ManagedPackageVersionInfoType - catalog package version snapshot.
    /// </summary>
    public class ManagedPackageVersionInfoType
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
        /// PackageId
        /// </summary>
        public string? PackageId
        {
            get;
            set;
        }

        /// <summary>
        /// VersionNumber
        /// </summary>
        public int VersionNumber
        {
            get;
            set;
        }

        /// <summary>
        /// CreatedAt
        /// </summary>
        public DateTime CreatedAt
        {
            get;
            set;
        }

        /// <summary>
        /// ChangeKind - create, update, rollback
        /// </summary>
        public string? ChangeKind
        {
            get;
            set;
        }

        /// <summary>
        /// ActorUsername
        /// </summary>
        public string? ActorUsername
        {
            get;
            set;
        }

        /// <summary>
        /// RestoredFromVersion
        /// </summary>
        public int? RestoredFromVersion
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
        /// ScriptEngine
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
        }
    }
}
