// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.ZCore.Type
{
    /// <summary>
    /// UserCreateRequestType
    /// </summary>
    public sealed class UserCreateRequestType
    {
        /// <summary>
        /// Username
        /// </summary>
        public string? Username
        {
            get;
            set;
        }

        /// <summary>
        /// Password
        /// </summary>
        public string? Password
        {
            get;
            set;
        }

        /// <summary>
        /// Role - Admin, Operator, Viewer, DeviceOwner
        /// </summary>
        public string? Role
        {
            get;
            set;
        }

        /// <summary>
        /// Email - optional notification address
        /// </summary>
        public string? Email
        {
            get;
            set;
        }
    }
}
