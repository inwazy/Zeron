// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.ZCore.Type
{
    /// <summary>
    /// LoginResponseType
    /// </summary>
    public sealed class LoginResponseType
    {
        /// <summary>
        /// Success
        /// </summary>
        public bool Success
        {
            get;
            set;
        }

        /// <summary>
        /// Token
        /// </summary>
        public string? Token
        {
            get;
            set;
        }

        /// <summary>
        /// User
        /// </summary>
        public UserInfoType? User
        {
            get;
            set;
        }

        /// <summary>
        /// Message
        /// </summary>
        public string? Message
        {
            get;
            set;
        }
    }
}
