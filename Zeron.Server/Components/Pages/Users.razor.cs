// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using Zeron.Server.ZCore;
using Zeron.Server.ZServers;
using Zeron.ZCore.Type;

namespace Zeron.Server.Components.Pages
{
    /// <summary>
    /// Users
    /// </summary>
    public partial class Users
    {
        // Rows.
        private List<UserEditRow> m_Rows = [];

        // Create model.
        private readonly CreateFormModel m_CreateModel = new();

        // Current user ID.
        private string? m_CurrentUserId;

        // Create message.
        private string? m_CreateMessage;

        // Update message.
        private string? m_UpdateMessage;

        // Create succeeded.
        private bool m_CreateSucceeded;

        // Update succeeded.
        private bool m_UpdateSucceeded;

        // Is busy.
        private bool m_IsBusy;

        /// <summary>
        /// OnInitializedAsync
        /// </summary>
        /// <returns>Returns Task.</returns>
        protected override async Task OnInitializedAsync()
        {
            AuthenticationState authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            m_CurrentUserId = authState.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            await ReloadAsync();
        }

        /// <summary>
        /// ReloadAsync
        /// </summary>
        /// <returns>Returns Task.</returns>
        private async Task ReloadAsync()
        {
            List<UserInfoType> users = await UserManager.GetUsersAsync();

            m_Rows = users
                .Where(user => !string.IsNullOrWhiteSpace(user.Id))
                .Select(user => new UserEditRow
                {
                    Id = user.Id!,
                    Username = user.Username ?? "",
                    Role = user.Role ?? ServerRoles.Viewer,
                    IsActive = user.IsActive,
                    MustChangePassword = user.MustChangePassword,
                    CreatedAt = user.CreatedAt,
                    NewPassword = ""
                })
                .ToList();
        }

        /// <summary>
        /// CreateUserAsync
        /// </summary>
        /// <returns>Returns Task.</returns>
        private async Task CreateUserAsync()
        {
            m_CreateMessage = null;
            m_IsBusy = true;

            try
            {
                (UserInfoType? user, string? error) = await UserManager.CreateUserAsync(new UserCreateRequestType
                {
                    Username = m_CreateModel.Username,
                    Password = m_CreateModel.Password,
                    Role = m_CreateModel.Role
                });

                if (error != null)
                {
                    m_CreateSucceeded = false;
                    m_CreateMessage = error;
                    return;
                }

                m_CreateSucceeded = true;
                m_CreateMessage = $"Created user '{user!.Username}'.";
                m_CreateModel.Username = "";
                m_CreateModel.Password = "";
                m_CreateModel.Role = ServerRoles.Viewer;
                await ReloadAsync();
            }
            finally
            {
                m_IsBusy = false;
            }
        }

        /// <summary>
        /// SaveUserAsync
        /// </summary>
        /// <param name="row"></param>
        /// <returns>Returns Task.</returns>
        private async Task SaveUserAsync(
            UserEditRow row)
        {
            if (!Guid.TryParse(row.Id, out Guid userId))
            {
                return;
            }

            m_UpdateMessage = null;
            m_IsBusy = true;

            try
            {
                (UserInfoType? updated, string? error) = await UserManager.UpdateUserAsync(
                    userId,
                    new UserUpdateRequestType
                    {
                        Role = row.Role,
                        Password = string.IsNullOrWhiteSpace(row.NewPassword) ? null : row.NewPassword
                    },
                    ParseCurrentUserId());

                if (error != null)
                {
                    m_UpdateSucceeded = false;
                    m_UpdateMessage = error;
                    return;
                }

                m_UpdateSucceeded = true;
                m_UpdateMessage = $"Updated user '{updated!.Username}'.";
                await ReloadAsync();
            }
            finally
            {
                m_IsBusy = false;
            }
        }

        /// <summary>
        /// SetActiveAsync
        /// </summary>
        /// <param name="row"></param>
        /// <param name="isActive"></param>
        /// <returns>Returns Task.</returns>
        private async Task SetActiveAsync(
            UserEditRow row, 
            bool isActive)
        {
            if (!Guid.TryParse(row.Id, out Guid userId))
            {
                return;
            }

            m_UpdateMessage = null;
            m_IsBusy = true;

            try
            {
                (UserInfoType? updated, string? error) = await UserManager.UpdateUserAsync(
                    userId,
                    new UserUpdateRequestType { IsActive = isActive },
                    ParseCurrentUserId());

                if (error != null)
                {
                    m_UpdateSucceeded = false;
                    m_UpdateMessage = error;
                    return;
                }

                m_UpdateSucceeded = true;
                m_UpdateMessage = isActive
                    ? $"Activated user '{updated!.Username}'."
                    : $"Deactivated user '{updated!.Username}'.";
                await ReloadAsync();
            }
            finally
            {
                m_IsBusy = false;
            }
        }

        /// <summary>
        /// ParseCurrentUserId
        /// </summary>
        /// <returns>Returns Guid?</returns>
        private Guid? ParseCurrentUserId()
        {
            return Guid.TryParse(m_CurrentUserId, out Guid userId) ? userId : null;
        }

        /// <summary>
        /// CreateFormModel
        /// </summary>
        /// <returns>Returns void.</returns>
        private sealed class CreateFormModel
        {
            // Username.
            public string Username { get; set; } = "";

            // Password.
            public string Password { get; set; } = "";

            // Role.
            public string Role { get; set; } = ServerRoles.Viewer;
        }

        /// <summary>
        /// UserEditRow
        /// </summary>
        /// <returns>Returns void.</returns>
        private sealed class UserEditRow
        {
            // ID.
            public string Id { get; set; } = "";

            // Username.
            public string Username { get; set; } = "";

            // Role.
            public string Role { get; set; } = ServerRoles.Viewer;

            // Is active.
            public bool IsActive { get; set; }

            // Must change password.
            public bool MustChangePassword { get; set; }

            // Created at.
            public DateTime? CreatedAt { get; set; }

            // New password.
            public string NewPassword { get; set; } = "";
        }

    }
}
