// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Zeron.ZInterfaces;

namespace Zeron.ZServers.RequestImpls
{
    /// <summary>
    /// FileSystemImpl
    /// </summary>
    public class FileSystemImpl : IImpl, IServicesRequest
    {
        /// <summary>
        /// APIName
        /// </summary>
        /// <returns>Returns string.</returns>
        public string APIName { get; } = "FileSystem";

        /// <summary>
        /// APIKey
        /// </summary>
        /// <returns>Returns string.</returns>
        public string APIKey { get; set; } = "";

        /// <summary>
        /// Command
        /// </summary>
        /// <returns>Returns string.</returns>
        public string Command { get; set; } = "";

        /// <summary>
        /// Async
        /// </summary>
        /// <returns>Returns bool.</returns>
        public bool Async { get; set; }

        /// <summary>
        /// Dispose
        /// </summary>
        /// <returns>Returns void.</returns>
        public void Dispose() { }
    }
}
