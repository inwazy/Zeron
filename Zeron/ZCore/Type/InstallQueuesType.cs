// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.ZCore.Type
{
    /// <summary>
    /// InstallQueuesType
    /// </summary>
    public class InstallQueuesType
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
        /// InstallQueuesType
        /// </summary>
        /// <returns>Returns void.</returns>
        public InstallQueuesType()
        {
            RepoUrl = "";
            FileName = "";
            FilePath = "";
            Arguments = "";
        }
    }
}
