// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.ZCore.Type
{
    /// <summary>
    /// PackageDeployRequestType
    /// </summary>
    public class PackageDeployRequestType
    {
        /// <summary>
        /// Operation - install or uninstall
        /// </summary>
        public string? Operation
        {
            get;
            set;
        }

        /// <summary>
        /// PackageName - must exist in each agent's managed_packages catalog
        /// </summary>
        public string? PackageName
        {
            get;
            set;
        }

        /// <summary>
        /// ExtraArgs - appended to package install/uninstall command args
        /// </summary>
        public string? ExtraArgs
        {
            get;
            set;
        }

        /// <summary>
        /// Name - optional task name
        /// </summary>
        public string? Name
        {
            get;
            set;
        }

        /// <summary>
        /// Description
        /// </summary>
        public string? Description
        {
            get;
            set;
        }

        /// <summary>
        /// TargetType - all, agent, filter
        /// </summary>
        public string? TargetType
        {
            get;
            set;
        }

        /// <summary>
        /// AgentIds
        /// </summary>
        public List<string>? AgentIds
        {
            get;
            set;
        }

        /// <summary>
        /// HostnamePattern
        /// </summary>
        public string? HostnamePattern
        {
            get;
            set;
        }
    }
}
