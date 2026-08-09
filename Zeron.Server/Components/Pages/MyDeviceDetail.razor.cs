// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using Zeron.Server.Data.Entities;
using Zeron.ZCore.Type;

namespace Zeron.Server.Components.Pages
{
    /// <summary>
    /// MyDeviceDetail
    /// </summary>
    public partial class MyDeviceDetail
    {
        /// <summary>
        /// AgentKey
        /// </summary>
        [Parameter]
        public string AgentKey { get; set; } = "";

        // Device.
        private DeviceAgentStatusType? m_Device;

        // Events.
        private List<EventEntity> m_Events = [];

        // Error.
        private string? m_Error;

        // Busy.
        private bool m_IsBusy;

        /// <summary>
        /// OnParametersSetAsync
        /// </summary>
        /// <returns>Returns Task.</returns>
        protected override async Task OnParametersSetAsync()
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
                    m_Device = null;
                    m_Events = [];
                    return;
                }

                m_Device = await PortalServer.GetMyDeviceAsync(userId, AgentKey);

                if (m_Device == null)
                {
                    m_Events = [];
                    return;
                }

                m_Events = await PortalServer.GetMyInstallEventsAsync(userId, AgentKey, 20) ?? [];
            }
            finally
            {
                m_IsBusy = false;
            }
        }
    }
}
