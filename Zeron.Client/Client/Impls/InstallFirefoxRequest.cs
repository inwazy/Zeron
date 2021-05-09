// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Zeron.Interfaces;

namespace Zeron.Client.Client.Impls
{
    /// <summary>
    /// InstallFirefoxRequest
    /// </summary>
    class InstallFirefoxRequest : IServicesRequest
    {
        /// <summary>
        /// APIName
        /// </summary>
        public string APIName { get; }

        /// <summary>
        /// APIKey
        /// </summary>
        public string APIKey { get; set; }

        /// <summary>
        /// Async
        /// </summary>
        public bool Async { get; set; }

        /// <summary>
        /// InstallFirefoxRequest
        /// </summary>
        /// <returns>Returns void.</returns>
        public InstallFirefoxRequest()
        {
            APIName = "InstallFirefox";
            APIKey = "";
            Async = false;
        }
    }
}
