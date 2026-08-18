// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.SignalR.Client;
using System.Security.Claims;
using Zeron.Server.Data.Entities;
using Zeron.Server.Hubs;
using Zeron.Server.ZCore.Type;
using Zeron.Server.ZServers;
using Zeron.ZCore.Type;

namespace Zeron.Server.Components.Pages
{
    /// <summary>
    /// MyDeviceDetail
    /// </summary>
    public partial class MyDeviceDetail : IAsyncDisposable
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

        // Unread install-result notifications for this agent.
        private List<UserNotificationInfoType> m_Notifications = [];

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

        // Hub connection.
        private HubConnection? m_HubConnection;

        /// <summary>
        /// OnInitializedAsync
        /// </summary>
        /// <returns>Returns Task.</returns>
        protected override async Task OnInitializedAsync()
        {
            await ConnectHubAsync();
        }

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
                await LoadDataAsync();
            }
            finally
            {
                m_IsBusy = false;
            }
        }

        /// <summary>
        /// LoadDataAsync
        /// </summary>
        /// <returns>Returns Task.</returns>
        private async Task LoadDataAsync()
        {
            AuthenticationState authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            string? userIdValue = authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!Guid.TryParse(userIdValue, out m_UserId))
            {
                m_Error = "Unable to resolve the current user.";
                m_Device = null;
                m_Events = [];
                m_Notifications = [];
                return;
            }

            m_Packages = await CatalogServer.GetPackagesAsync(enabledOnly: true);
            m_Device = await PortalServer.GetMyDeviceAsync(m_UserId, AgentKey);
            m_Notifications = await LoadUnreadForAgentAsync(m_UserId);

            if (m_Device == null)
            {
                m_Events = [];
                return;
            }

            m_Events = await PortalServer.GetMyInstallEventsAsync(m_UserId, AgentKey, 20) ?? [];
        }

        /// <summary>
        /// LoadUnreadForAgentAsync
        /// </summary>
        /// <param name="userId"></param>
        /// <returns>Returns matching unread tips.</returns>
        private async Task<List<UserNotificationInfoType>> LoadUnreadForAgentAsync(
            Guid userId)
        {
            List<UserNotificationInfoType> unread = await NotificationServer.GetNotificationsAsync(
                userId,
                unreadOnly: true,
                limit: 20);

            return unread
                .Where(item =>
                    string.Equals(item.Kind, UserNotificationServer.KindInstallResult, StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrWhiteSpace(item.AgentKey)
                    && string.Equals(item.AgentKey, AgentKey, StringComparison.OrdinalIgnoreCase))
                .Take(10)
                .ToList();
        }

        /// <summary>
        /// ConnectHubAsync
        /// </summary>
        /// <returns>Returns Task.</returns>
        private async Task ConnectHubAsync()
        {
            if (m_HubConnection != null)
            {
                return;
            }

            m_HubConnection = DashboardHubClient.Create(Navigation, HttpContextAccessor);
            m_HubConnection.On<UserNotificationInfoType>(
                DashboardHub.InstallResultReceived,
                async note => await OnInstallResultReceivedAsync(note));

            await DashboardHubClient.TryStartAsync(m_HubConnection);
        }

        /// <summary>
        /// OnInstallResultReceivedAsync
        /// </summary>
        /// <param name="note"></param>
        /// <returns>Returns Task.</returns>
        private async Task OnInstallResultReceivedAsync(
            UserNotificationInfoType note)
        {
            if (!string.Equals(note.Kind, UserNotificationServer.KindInstallResult, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(note.AgentKey)
                || !string.Equals(note.AgentKey, AgentKey, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            await InvokeAsync(async () =>
            {
                if (!string.IsNullOrWhiteSpace(note.Id)
                    && m_Notifications.Any(item => item.Id == note.Id))
                {
                    return;
                }

                m_Notifications.Insert(0, note);

                if (m_Notifications.Count > 10)
                {
                    m_Notifications.RemoveRange(10, m_Notifications.Count - 10);
                }

                if (m_UserId != Guid.Empty)
                {
                    m_Device = await PortalServer.GetMyDeviceAsync(m_UserId, AgentKey);
                    m_Events = await PortalServer.GetMyInstallEventsAsync(m_UserId, AgentKey, 20) ?? [];
                }

                StateHasChanged();
            });
        }

        /// <summary>
        /// DismissAsync
        /// </summary>
        /// <param name="note"></param>
        /// <returns>Returns Task.</returns>
        private async Task DismissAsync(
            UserNotificationInfoType note)
        {
            if (!Guid.TryParse(note.Id, out Guid notificationId) || m_UserId == Guid.Empty)
            {
                return;
            }

            m_IsBusy = true;

            try
            {
                await NotificationServer.MarkReadAsync(m_UserId, notificationId);
                m_Notifications = await LoadUnreadForAgentAsync(m_UserId);
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

        /// <summary>
        /// DisposeAsync
        /// </summary>
        /// <returns>Returns ValueTask.</returns>
        public async ValueTask DisposeAsync()
        {
            if (m_HubConnection != null)
            {
                await m_HubConnection.DisposeAsync();
            }
        }
    }
}
