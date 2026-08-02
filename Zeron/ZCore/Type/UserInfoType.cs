// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.ZCore.Type
{
    /// <summary>
    /// UserInfoType
    /// </summary>
    public class UserInfoType
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
        /// IsActive
        /// </summary>
        public bool IsActive
        {
            get;
            set;
        } = true;

        /// <summary>
        /// CreatedAt
        /// </summary>
        public DateTime? CreatedAt
        {
            get;
            set;
        }

        /// <summary>
        /// MustChangePassword
        /// </summary>
        public bool MustChangePassword
        {
            get;
            set;
        }
    }
}
