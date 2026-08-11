// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.ZCore.Type
{
    /// <summary>
    /// PackageDeployResponseType
    /// </summary>
    public sealed class PackageDeployResponseType
    {
        /// <summary>
        /// Success
        /// </summary>
        public bool Success
        {
            get;
            set;
        }

        /// <summary>
        /// Message
        /// </summary>
        public string? Message
        {
            get;
            set;
        }

        /// <summary>
        /// TaskId
        /// </summary>
        public Guid? TaskId
        {
            get;
            set;
        }

        /// <summary>
        /// Command
        /// </summary>
        public string? Command
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
        /// PackageName
        /// </summary>
        public string? PackageName
        {
            get;
            set;
        }
    }
}
