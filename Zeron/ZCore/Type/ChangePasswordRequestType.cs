// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.ZCore.Type
{
    /// <summary>
    /// ChangePasswordRequestType
    /// </summary>
    public sealed class ChangePasswordRequestType
    {
        /// <summary>
        /// CurrentPassword
        /// </summary>
        public string? CurrentPassword
        {
            get;
            set;
        }

        /// <summary>
        /// NewPassword
        /// </summary>
        public string? NewPassword
        {
            get;
            set;
        }
    }
}
