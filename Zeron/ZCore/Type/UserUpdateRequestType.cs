// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.ZCore.Type
{
    /// <summary>
    /// UserUpdateRequestType
    /// </summary>
    public class UserUpdateRequestType
    {
        /// <summary>
        /// Role - Admin, Operator, Viewer
        /// </summary>
        public string? Role
        {
            get;
            set;
        }

        /// <summary>
        /// IsActive
        /// </summary>
        public bool? IsActive
        {
            get;
            set;
        }

        /// <summary>
        /// Password - optional reset
        /// </summary>
        public string? Password
        {
            get;
            set;
        }
    }
}
