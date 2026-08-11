// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.ZCore.Type
{
    /// <summary>
    /// SmtpMailOptionsType - shared SMTP connection and From identity.
    /// </summary>
    public sealed class SmtpMailOptionsType
    {
        /// <summary>
        /// Host
        /// </summary>
        public string? Host
        {
            get;
            set;
        }

        /// <summary>
        /// Port
        /// </summary>
        public int Port
        {
            get;
            set;
        } = 587;

        /// <summary>
        /// EnableSsl
        /// </summary>
        public bool EnableSsl
        {
            get;
            set;
        } = true;

        /// <summary>
        /// UserName
        /// </summary>
        public string? UserName
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
        /// FromAddress
        /// </summary>
        public string? FromAddress
        {
            get;
            set;
        }

        /// <summary>
        /// FromDisplayName
        /// </summary>
        public string? FromDisplayName
        {
            get;
            set;
        }
    }
}
