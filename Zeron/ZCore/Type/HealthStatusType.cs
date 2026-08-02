// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.ZCore.Type
{
    /// <summary>
    /// HealthStatusType
    /// </summary>
    public class HealthStatusType
    {
        /// <summary>
        /// Status - healthy, unhealthy, degraded
        /// </summary>
        public string Status
        {
            get;
            set;
        } = "healthy";

        /// <summary>
        /// Service
        /// </summary>
        public string Service
        {
            get;
            set;
        } = "Zeron.Server";

        /// <summary>
        /// Version
        /// </summary>
        public string? Version
        {
            get;
            set;
        }

        /// <summary>
        /// TimestampUtc
        /// </summary>
        public DateTime TimestampUtc
        {
            get;
            set;
        }

        /// <summary>
        /// Checks
        /// </summary>
        public Dictionary<string, string>? Checks
        {
            get;
            set;
        }
    }
}
