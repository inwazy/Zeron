// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Microsoft.AspNetCore.Components;
using Zeron.Server.ZServers;
using Zeron.ZCore.Type;

namespace Zeron.Server.Components.Pages
{
    /// <summary>
    /// ChangePassword
    /// </summary>
    public partial class ChangePassword
    {
        // Required.
        [SupplyParameterFromQuery(Name = "required")]
        public string? Required { get; set; }

        // Error code.
        [SupplyParameterFromQuery(Name = "error")]
        public string? ErrorCode { get; set; }

        // Is required.
        private bool m_IsRequired;

        // Error.
        private string? m_Error;

        /// <summary>
        /// OnInitialized
        /// </summary>
        /// <returns>Returns void.</returns>
        protected override void OnInitialized()
        {
            m_IsRequired = Required == "1";
            m_Error = ErrorCode switch
            {
                "mismatch" => "New password and confirmation do not match.",
                "current" => "Current password is incorrect.",
                "same" => "New password must be different from the current password.",
                "invalid" => "Unable to update password. Use at least 6 characters.",
                _ => null
            };
        }

    }
}
