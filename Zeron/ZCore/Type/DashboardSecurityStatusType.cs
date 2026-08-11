// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.ZCore.Type
{
    /// <summary>
    /// DashboardSecurityStatusType - server transport security posture for Dashboard.
    /// </summary>
    public sealed class DashboardSecurityStatusType
    {
        /// <summary>
        /// CurveEnabled
        /// </summary>
        public bool CurveEnabled
        {
            get;
            set;
        }

        /// <summary>
        /// CurvePublicKeyPresent
        /// </summary>
        public bool CurvePublicKeyPresent
        {
            get;
            set;
        }

        /// <summary>
        /// AgentHmacRequired
        /// </summary>
        public bool AgentHmacRequired
        {
            get;
            set;
        }

        /// <summary>
        /// RequireHttpsAgents
        /// </summary>
        public bool RequireHttpsAgents
        {
            get;
            set;
        }

        /// <summary>
        /// OverallStatus - hardened | partial | insecure
        /// </summary>
        public string OverallStatus
        {
            get;
            set;
        } = "insecure";

        /// <summary>
        /// Recommendations
        /// </summary>
        public List<string> Recommendations
        {
            get;
            set;
        } = [];
    }
}
