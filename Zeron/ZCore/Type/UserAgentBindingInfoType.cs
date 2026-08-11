// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.ZCore.Type
{
    /// <summary>
    /// UserAgentBindingInfoType
    /// </summary>
    public sealed class UserAgentBindingInfoType
    {
        /// <summary>
        /// Id
        /// </summary>
        public string? Id
        {
            get;
            set;
        }

        /// <summary>
        /// UserId
        /// </summary>
        public string? UserId
        {
            get;
            set;
        }

        /// <summary>
        /// Username
        /// </summary>
        public string? Username
        {
            get;
            set;
        }

        /// <summary>
        /// AgentKey
        /// </summary>
        public string? AgentKey
        {
            get;
            set;
        }

        /// <summary>
        /// MachineName
        /// </summary>
        public string? MachineName
        {
            get;
            set;
        }

        /// <summary>
        /// BoundAt
        /// </summary>
        public DateTime? BoundAt
        {
            get;
            set;
        }
    }
}
