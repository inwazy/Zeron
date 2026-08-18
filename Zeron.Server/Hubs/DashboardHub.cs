// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using System;
using Zeron.Server.ZCore;

namespace Zeron.Server.Hubs
{
    /// <summary>
    /// DashboardHub - staff live feed plus per-user DeviceOwner tips.
    /// </summary>
    [Authorize(Policy = ServerPolicies.DeviceOwnerOrStaff)]
    public class DashboardHub : Hub
    {
        /// <summary>
        /// StaffGroup - fleet-wide event/alert/agent updates.
        /// </summary>
        public const string StaffGroup = "staff";

        /// <summary>
        /// UserGroupPrefix - per-user DeviceOwner group name prefix.
        /// </summary>
        public const string UserGroupPrefix = "user:";

        /// <summary>
        /// InstallResultReceived
        /// </summary>
        public const string InstallResultReceived = "InstallResultReceived";

        /// <summary>
        /// OnConnectedAsync
        /// </summary>
        /// <returns>Returns Task.</returns>
        public override async Task OnConnectedAsync()
        {
            string? role = Context.User?.FindFirstValue(ClaimTypes.Role);

            if (string.Equals(role, ServerRoles.DeviceOwner, StringComparison.OrdinalIgnoreCase)
                && TryResolveUserId(out Guid userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, GetUserGroup(userId));
            }
            else
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, StaffGroup);
            }

            await base.OnConnectedAsync();
        }

        /// <summary>
        /// TryResolveUserId
        /// </summary>
        /// <param name="userId"></param>
        /// <returns>Returns bool.</returns>
        private bool TryResolveUserId(
            out Guid userId)
        {
            userId = default;

            string? idValue = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);

            return !string.IsNullOrWhiteSpace(idValue)
                && Guid.TryParse(idValue, out userId);
        }

        /// <summary>
        /// GetUserGroup
        /// </summary>
        /// <param name="userId"></param>
        /// <returns>Returns group name.</returns>
        private static string GetUserGroup(
            Guid userId)
        {
            return UserGroupPrefix + userId.ToString();
        }
    }
}
