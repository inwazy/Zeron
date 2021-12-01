// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Zeron.ZInterfaces;

namespace Zeron.ZServers.RequestImpls
{
    /// <summary>
    /// ManagedPackageImpl
    /// </summary>
    public class ManagedPackageImpl : IImpl, IServicesRequest
    {
        /// <summary>
        /// APIName
        /// </summary>
        public string APIName { get; } = "ManagedPackage";

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
        /// NotifySubscriber
        /// </summary>
        public bool NotifySubscriber { get; set; }

        /// <summary>
        /// Dispose
        /// </summary>
        /// <returns>Returns void.</returns>
        public void Dispose()
        {
        }
    }
}
