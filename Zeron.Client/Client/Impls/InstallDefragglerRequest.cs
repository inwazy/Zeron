// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Zeron.Interfaces;

namespace Zeron.Client.Client.Impls
{
    /// <summary>
    /// InstallDefragglerRequest
    /// </summary>
    public class InstallDefragglerRequest : IServicesRequest
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
        /// InstallDefragglerRequest
        /// </summary>
        /// <returns>Returns void.</returns>
        public InstallDefragglerRequest()
        {
            APIName = "InstallDefraggler";
            APIKey = "";
            Async = false;
        }
    }
}
