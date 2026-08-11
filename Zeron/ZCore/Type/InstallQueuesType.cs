// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.ZCore.Type
{
    /// <summary>
    /// InstallQueuesType
    /// </summary>
    public sealed class InstallQueuesType
    {
        /// <summary>
        /// RepoUrl
        /// </summary>
        public string? RepoUrl
        {
            get;
            set;
        }

        /// <summary>
        /// FileName
        /// </summary>
        public string? FileName
        {
            get;
            set;
        }

        /// <summary>
        /// FilePath
        /// </summary>
        public string? FilePath
        {
            get;
            set;
        }

        /// <summary>
        /// Arguments
        /// </summary>
        public string? Arguments
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
        /// Operation
        /// </summary>
        public string? Operation
        {
            get;
            set;
        }

        /// <summary>
        /// ScriptBefore
        /// </summary>
        public string? ScriptBefore
        {
            get;
            set;
        }

        /// <summary>
        /// ScriptAfter
        /// </summary>
        public string? ScriptAfter
        {
            get;
            set;
        }

        /// <summary>
        /// ScriptEngine - Script Host engine id (default powershell when empty).
        /// </summary>
        public string? ScriptEngine
        {
            get;
            set;
        }

        /// <summary>
        /// AssignmentId - optional Zeron.Server task assignment for completion tracking.
        /// </summary>
        public string? AssignmentId
        {
            get;
            set;
        }

        /// <summary>
        /// ExpectedSha256 - optional lowercase hex digest for downloaded binary.
        /// </summary>
        public string? ExpectedSha256
        {
            get;
            set;
        }

        /// <summary>
        /// InstallQueuesType
        /// </summary>
        /// <returns>Returns void.</returns>
        public InstallQueuesType()
        {
            RepoUrl = "";
            FileName = "";
            FilePath = "";
            Arguments = "";
            PackageName = "";
            Operation = "";
            ScriptBefore = "";
            ScriptAfter = "";
            ScriptEngine = null;
            AssignmentId = null;
            ExpectedSha256 = null;
        }
    }
}
