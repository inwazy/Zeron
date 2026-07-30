// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.ZCore.Type
{
    /// <summary>
    /// TaskStepDefinition
    /// </summary>
    public class TaskStepDefinition
    {
        /// <summary>
        /// Type - powershell, managedPackage, wait, api
        /// </summary>
        public string? Type
        {
            get;
            set;
        }

        /// <summary>
        /// Script - for powershell steps
        /// </summary>
        public string? Script
        {
            get;
            set;
        }

        /// <summary>
        /// Command - for managedPackage or api steps
        /// </summary>
        public string? Command
        {
            get;
            set;
        }

        /// <summary>
        /// ApiName - for api steps
        /// </summary>
        public string? ApiName
        {
            get;
            set;
        }

        /// <summary>
        /// Seconds - for wait steps
        /// </summary>
        public int Seconds
        {
            get;
            set;
        }
    }
}
