// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.Server.Data.Entities
{
    /// <summary>
    /// UserNotificationEntity - per-user dashboard tip (e.g. self-service install result).
    /// </summary>
    public class UserNotificationEntity
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
        /// UserId
        /// </summary>
        public Guid UserId
        {
            get;
            set;
        }

        /// <summary>
        /// Kind - e.g. install.result
        /// </summary>
        public string Kind
        {
            get;
            set;
        } = "";

        /// <summary>
        /// Title
        /// </summary>
        public string Title
        {
            get;
            set;
        } = "";

        /// <summary>
        /// Message
        /// </summary>
        public string Message
        {
            get;
            set;
        } = "";

        /// <summary>
        /// AgentKey
        /// </summary>
        public string? AgentKey
        {
            get;
            set;
        }

        /// <summary>
        /// PackageName
        /// </summary>
        public string? PackageName
        {
            get;
            set;
        }

        /// <summary>
        /// Success - null when not applicable
        /// </summary>
        public bool? Success
        {
            get;
            set;
        }

        /// <summary>
        /// CreatedAt
        /// </summary>
        public DateTime CreatedAt
        {
            get;
            set;
        }

        /// <summary>
        /// ReadAt
        /// </summary>
        public DateTime? ReadAt
        {
            get;
            set;
        }

        /// <summary>
        /// User
        /// </summary>
        public UserEntity? User
        {
            get;
            set;
        }
    }
}
