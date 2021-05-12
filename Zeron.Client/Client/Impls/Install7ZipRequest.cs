// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Zeron.Interfaces;

namespace Zeron.Client.Client.Impls
{
    /// <summary>
    /// Install7ZipRequest
    /// </summary>
    public class Install7ZipRequest : IServicesRequest
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
        /// Install7ZipRequest
        /// </summary>
        /// <returns>Returns void.</returns>
        public Install7ZipRequest()
        {
            APIName = "InstallSevenZip";
            APIKey = "";
            Async = false;
        }
    }
}
