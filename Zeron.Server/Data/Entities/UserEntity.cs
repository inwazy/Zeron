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
        /// Role - Admin, Operator, Viewer
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
    }
}
