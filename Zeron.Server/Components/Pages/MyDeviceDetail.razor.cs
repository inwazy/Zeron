// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using Zeron.Server.Data.Entities;
using Zeron.Server.ZCore.Type;
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

        // Catalog packages.
        private List<ManagedPackageInfoType> m_Packages = [];

        // Deploy form.
        private readonly DeployDeviceFormModelType m_Deploy = new();

        // Error.
        private string? m_Error;

        // Deploy message.
        private string? m_DeployMessage;

        // Deploy succeeded.
        private bool m_DeploySucceeded;

        // Busy.
        private bool m_IsBusy;

        // Current user id.
        private Guid m_UserId;

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

                if (!Guid.TryParse(userIdValue, out m_UserId))
                {
                    m_Error = "Unable to resolve the current user.";
                    m_Device = null;
                    m_Events = [];
                    return;
                }

                m_Packages = await CatalogServer.GetPackagesAsync(enabledOnly: true);
                m_Device = await PortalServer.GetMyDeviceAsync(m_UserId, AgentKey);

                if (m_Device == null)
                {
                    m_Events = [];
                    return;
                }

                m_Events = await PortalServer.GetMyInstallEventsAsync(m_UserId, AgentKey, 20) ?? [];
            }
            finally
            {
                m_IsBusy = false;
            }
        }

        /// <summary>
        /// DeployAsync
        /// </summary>
        /// <returns>Returns Task.</returns>
        private async Task DeployAsync()
        {
            m_IsBusy = true;
            m_DeployMessage = null;

            try
            {
                (PackageDeployResponseType? response, string? error) = await PortalServer.DeployToMyDeviceAsync(
                    m_UserId,
                    AgentKey,
                    new DeviceDeployRequestType
                    {
                        Operation = m_Deploy.Operation,
                        PackageName = m_Deploy.PackageName,
                        ExtraArgs = m_Deploy.ExtraArgs
                    });

                if (error != null || response == null || !response.Success)
                {
                    m_DeploySucceeded = false;
                    m_DeployMessage = error ?? response?.Message ?? "Deploy failed.";
                    return;
                }

                m_DeploySucceeded = true;
                m_DeployMessage = $"Deploy queued: {response.Command} (task {response.TaskId}).";

                await ReloadAsync();
            }
            finally
            {
                m_IsBusy = false;
            }
        }
    }
}
