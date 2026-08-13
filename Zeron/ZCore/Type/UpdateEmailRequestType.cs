// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.ZCore.Type
{
    /// <summary>
    /// UpdateEmailRequestType - self-service email update.
    /// </summary>
    public sealed class UpdateEmailRequestType
    {
        /// <summary>
        /// Email - empty clears the notification address
        /// </summary>
        public string? Email
        {
            get;
            set;
        }
    }
}
