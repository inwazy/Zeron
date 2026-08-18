// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Microsoft.AspNetCore.Components;

namespace Zeron.Server.Components.Pages
{
    /// <summary>
    /// Login
    /// </summary>
    public partial class Login
    {
        /// <summary>
        /// Failed
        /// </summary>
        [SupplyParameterFromQuery(Name = "failed")]
        public string? Failed { get; set; }

        // Error message.
        private string? m_Error;

        // Field-level errors.
        private string? m_UsernameError;
        private string? m_PasswordError;

        /// <summary>
        /// OnInitialized
        /// </summary>
        /// <returns>Returns void.</returns>
        protected override void OnInitialized()
        {
            m_Error = Failed switch
            {
                "username" => "Username is required.",
                "password" => "Password is required.",
                "credentials" or "1" => "Invalid username or password.",
                _ => null
            };

            m_UsernameError = null;
            m_PasswordError = null;

            switch (Failed)
            {
                case "username":
                    m_UsernameError = m_Error;
                    break;
                case "password":
                    m_PasswordError = m_Error;
                    break;
                case "credentials":
                case "1":
                    m_UsernameError = m_Error;
                    m_PasswordError = m_Error;
                    break;
            }
        }
    }
}
