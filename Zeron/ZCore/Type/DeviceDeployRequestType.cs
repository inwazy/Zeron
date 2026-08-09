// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.ZCore.Type
{
    /// <summary>
    /// DeviceDeployRequestType - self-service install/uninstall for a bound agent.
    /// </summary>
    public class DeviceDeployRequestType
    {
        /// <summary>
        /// Operation - install or uninstall.
        /// </summary>
        public string? Operation
        {
            get;
            set;
        }

        /// <summary>
        /// PackageName - must exist and be enabled in Server catalog.
        /// </summary>
        public string? PackageName
        {
            get;
            set;
        }

        /// <summary>
        /// ExtraArgs
        /// </summary>
        public string? ExtraArgs
        {
            get;
            set;
        }
    }
}
