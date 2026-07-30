// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.ZCore.Type
{
    /// <summary>
    /// InstallJobStatus
    /// </summary>
    public class InstallJobStatus
    {
        /// <summary>
        /// QueueCount
        /// </summary>
        public int QueueCount
        {
            get;
            set;
        }

        /// <summary>
        /// IsRunning
        /// </summary>
        public bool IsRunning
        {
            get;
            set;
        }

        /// <summary>
        /// CurrentPackage
        /// </summary>
        public string? CurrentPackage
        {
            get;
            set;
        }

        /// <summary>
        /// CurrentOperation
        /// </summary>
        public string? CurrentOperation
        {
            get;
            set;
        }

        /// <summary>
        /// LastPackage
        /// </summary>
        public string? LastPackage
        {
            get;
            set;
        }

        /// <summary>
        /// LastOperation
        /// </summary>
        public string? LastOperation
        {
            get;
            set;
        }

        /// <summary>
        /// LastSuccess
        /// </summary>
        public bool? LastSuccess
        {
            get;
            set;
        }

        /// <summary>
        /// LastExitCode
        /// </summary>
        public int? LastExitCode
        {
            get;
            set;
        }

        /// <summary>
        /// LastCompletedAt
        /// </summary>
        public string? LastCompletedAt
        {
            get;
            set;
        }
    }
}
