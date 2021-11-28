// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Zeron.ZInterfaces;

namespace Zeron.ZServers.RequestImpls
{
    /// <summary>
    /// ServerInfoImpl
    /// </summary>
    public class ServerInfoImpl : IImpl, IServicesRequest
    {
        /// <summary>
        /// APIName
        /// </summary>
        public string APIName { get; } = "ServerInfo";

        /// <summary>
        /// APIKey
        /// </summary>
        public string APIKey { get; set; } = "";

        /// <summary>
        /// Command
        /// </summary>
        public string Command { get; set; } = "";

        /// <summary>
        /// Async
        /// </summary>
        public bool Async { get; set; }

        /// <summary>
        /// Dispose
        /// </summary>
        /// <returns>Returns void.</returns>
        public void Dispose()
        {
        }
    }
}
