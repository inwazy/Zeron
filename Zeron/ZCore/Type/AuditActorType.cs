// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.ZCore.Type
{
    /// <summary>
    /// AuditActorType - who performed an audited action.
    /// </summary>
    public sealed class AuditActorType
    {
        /// <summary>
        /// UserId
        /// </summary>
        public Guid? UserId
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
        /// Role
        /// </summary>
        public string? Role
        {
            get;
            set;
        }

        /// <summary>
        /// Source - server or agent
        /// </summary>
        public string Source
        {
            get;
            set;
        } = "server";
    }
}
