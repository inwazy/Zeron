// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Microsoft.AspNetCore.Components.Authorization;
using Zeron.Server.ZCore;
using Zeron.ZCore.Type;

namespace Zeron.Server.Components.Pages
{
    /// <summary>
    /// Account - self-service email and password.
    /// </summary>
    public partial class Account
    {
        // Profile.
        private UserInfoType? m_Profile;

        // Email draft.
        private string m_Email = "";

        // Password fields.
        private string m_CurrentPassword = "";

        // New password.
        private string m_NewPassword = "";

        // Confirm password.
        private string m_ConfirmPassword = "";

        // Email message.
        private string? m_EmailMessage;

        // Password message.
        private string? m_PasswordMessage;

        // Email succeeded.
        private bool m_EmailSucceeded;

        // Password succeeded.
        private bool m_PasswordSucceeded;

        // Busy.
        private bool m_IsBusy;

        // Home path for Back link.
        private string m_HomePath = "/";

        /// <summary>
        /// OnInitializedAsync
        /// </summary>
        /// <returns>Returns Task.</returns>
        protected override async Task OnInitializedAsync()
        {
            await ReloadAsync();
        }

        /// <summary>
        /// ReloadAsync
        /// </summary>
        /// <returns>Returns Task.</returns>
        private async Task ReloadAsync()
        {
            AuthenticationState authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            
            m_Profile = await AuthServer.GetUserFromPrincipalAsync(authState.User);
            m_Email = m_Profile?.Email ?? "";
            m_HomePath = string.Equals(m_Profile?.Role, ServerRoles.DeviceOwner, StringComparison.OrdinalIgnoreCase)
                ? "/my-devices"
                : "/";
        }

        /// <summary>
        /// SaveEmailAsync
        /// </summary>
        /// <returns>Returns Task.</returns>
        private async Task SaveEmailAsync()
        {
            if (m_Profile == null || !Guid.TryParse(m_Profile.Id, out Guid userId))
            {
                return;
            }

            m_IsBusy = true;
            m_EmailMessage = null;

            try
            {
                (UserInfoType? user, string? error) = await AuthServer.UpdateEmailAsync(userId, m_Email);

                if (error != null)
                {
                    m_EmailSucceeded = false;
                    m_EmailMessage = error;
                    return;
                }

                m_Profile = user;
                m_Email = user?.Email ?? "";
                m_EmailSucceeded = true;
                m_EmailMessage = string.IsNullOrWhiteSpace(m_Email)
                    ? "Email cleared."
                    : "Email saved.";
            }
            finally
            {
                m_IsBusy = false;
            }
        }

        /// <summary>
        /// SavePasswordAsync
        /// </summary>
        /// <returns>Returns Task.</returns>
        private async Task SavePasswordAsync()
        {
            if (m_Profile == null || !Guid.TryParse(m_Profile.Id, out Guid userId))
            {
                return;
            }

            m_IsBusy = true;
            m_PasswordMessage = null;

            try
            {
                if (!string.Equals(m_NewPassword, m_ConfirmPassword, StringComparison.Ordinal))
                {
                    m_PasswordSucceeded = false;
                    m_PasswordMessage = "New password and confirmation do not match.";
                    return;
                }

                (UserInfoType? user, string? error) = await AuthServer.ChangePasswordAsync(
                    userId,
                    m_CurrentPassword,
                    m_NewPassword);

                if (error != null)
                {
                    m_PasswordSucceeded = false;
                    m_PasswordMessage = error;
                    return;
                }

                m_Profile = user;
                m_CurrentPassword = "";
                m_NewPassword = "";
                m_ConfirmPassword = "";
                m_PasswordSucceeded = true;
                m_PasswordMessage = "Password updated.";
            }
            finally
            {
                m_IsBusy = false;
            }
        }
    }
}
