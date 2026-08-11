// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.Server.Data.Entities
{
    /// <summary>
    /// UserEntity
    /// </summary>
    public class UserEntity
    {
        /// <summary>
        /// Id
        /// </summary>
        public Guid Id
        {
            get;
            set;
        }

        /// <summary>
        /// Username
        /// </summary>
        public string Username
        {
            get;
            set;
        } = "";

        /// <summary>
        /// PasswordHash
        /// </summary>
        public string PasswordHash
        {
            get;
            set;
        } = "";

        /// <summary>
        /// Role - Admin, Operator, Viewer, DeviceOwner
        /// </summary>
        public string Role
        {
            get;
            set;
        } = "Viewer";

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
        public DateTime CreatedAt
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

        /// <summary>
        /// Email - optional address for install-result and similar notifications.
        /// </summary>
        public string? Email
        {
            get;
            set;
        }
    }
}
