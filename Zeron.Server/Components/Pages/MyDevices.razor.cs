// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using Zeron.ZCore.Type;

namespace Zeron.Server.Components.Pages
{
    /// <summary>
    /// MyDevices
    /// </summary>
    public partial class MyDevices
    {
        // Devices.
        private List<DeviceAgentStatusType> m_Devices = [];

        // Error.
        private string? m_Error;

        // Busy.
        private bool m_IsBusy;

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
            m_IsBusy = true;
            m_Error = null;

            try
            {
                AuthenticationState authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
                string? userIdValue = authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!Guid.TryParse(userIdValue, out Guid userId))
                {
                    m_Error = "Unable to resolve the current user.";
                    return;
                }

                m_Devices = await PortalServer.GetMyDevicesAsync(userId);
            }
            finally
            {
                m_IsBusy = false;
            }
        }
    }
}
