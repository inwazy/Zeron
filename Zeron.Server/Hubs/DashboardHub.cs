// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
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
        /// InstallResultReceived
        /// </summary>
        public const string InstallResultReceived = "InstallResultReceived";

        /// <summary>
        /// OnConnectedAsync
        /// </summary>
        /// <returns>Returns Task.</returns>
        public override async Task OnConnectedAsync()
        {
            if (!IsDeviceOwnerOnly())
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, StaffGroup);
            }

            await base.OnConnectedAsync();
        }

        /// <summary>
        /// IsDeviceOwnerOnly
        /// </summary>
        /// <returns>Returns bool.</returns>
        private bool IsDeviceOwnerOnly()
        {
            string? role = Context.User?.FindFirstValue(ClaimTypes.Role);

            return string.Equals(role, ServerRoles.DeviceOwner, StringComparison.OrdinalIgnoreCase);
        }
    }
}
