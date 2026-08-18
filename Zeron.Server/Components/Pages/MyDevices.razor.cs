// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.SignalR.Client;
using System.Security.Claims;
using System;
using System.Collections.Generic;
using Zeron.Server.Hubs;
using Zeron.Server.ZServers;
using Zeron.ZCore.Type;

namespace Zeron.Server.Components.Pages
{
    /// <summary>
    /// MyDevices
    /// </summary>
    public partial class MyDevices : IAsyncDisposable
    {
        // Devices.
        private List<DeviceAgentStatusType> m_Devices = [];

        // Unread install-result notifications.
        private List<UserNotificationInfoType> m_Notifications = [];

        // Error.
        private string? m_Error;

        // Busy.
        private bool m_IsBusy;

        // Hub connection.
        private HubConnection? m_HubConnection;

        /// <summary>
        /// FilterNotificationsForCurrentDevices
        /// </summary>
        /// <param name="notes"></param>
        /// <returns>Returns filtered list.</returns>
        private List<UserNotificationInfoType> FilterNotificationsForCurrentDevices(
            List<UserNotificationInfoType> notes)
        {
            return notes
                .Where(note =>
                    string.Equals(note.Kind, UserNotificationServer.KindInstallResult, StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrWhiteSpace(note.AgentKey)
                    && m_Devices.Any(device =>
                        string.Equals(device.AgentKey, note.AgentKey, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        /// <summary>
        /// OnInitializedAsync
        /// </summary>
        /// <returns>Returns Task.</returns>
        protected override async Task OnInitializedAsync()
        {
            await ReloadAsync();
            await ConnectHubAsync();
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

            if (!Guid.TryParse(userIdValue, out Guid userId))
            {
                m_Error = "Unable to resolve the current user.";
                return;
            }

            m_Devices = await PortalServer.GetMyDevicesAsync(userId);
            List<UserNotificationInfoType> unread = await NotificationServer.GetNotificationsAsync(userId, unreadOnly: true, limit: 10);
            m_Notifications = FilterNotificationsForCurrentDevices(unread);
        }

        /// <summary>
        /// ConnectHubAsync
        /// </summary>
        /// <returns>Returns Task.</returns>
        private async Task ConnectHubAsync()
        {
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
            await InvokeAsync(async () =>
            {
                if (!string.Equals(note.Kind, UserNotificationServer.KindInstallResult, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                if (string.IsNullOrWhiteSpace(note.AgentKey))
                {
                    return;
                }

                if (!string.IsNullOrWhiteSpace(note.Id)
                    && m_Notifications.Any(item => item.Id == note.Id))
                {
                    return;
                }

                // Refresh the bound agent list once so we don't accidentally show notifications for stale bindings.
                m_Notifications.Insert(0, note);

                if (m_Notifications.Count > 10)
                {
                    m_Notifications.RemoveRange(10, m_Notifications.Count - 10);
                }

                AuthenticationState authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
                string? userIdValue = authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (Guid.TryParse(userIdValue, out Guid userId))
                {
                    m_Devices = await PortalServer.GetMyDevicesAsync(userId);
                }

                // Apply the strict filter after device list refresh.
                m_Notifications = FilterNotificationsForCurrentDevices(m_Notifications);

                // If this notification doesn't match the current bindings, do not show it.
                if (!m_Notifications.Any(item => item.Id == note.Id))
                {
                    return;
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
            if (!Guid.TryParse(note.Id, out Guid notificationId))
            {
                return;
            }

            AuthenticationState authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            string? userIdValue = authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!Guid.TryParse(userIdValue, out Guid userId))
            {
                return;
            }

            m_IsBusy = true;

            try
            {
                await NotificationServer.MarkReadAsync(userId, notificationId);

                List<UserNotificationInfoType> unread = await NotificationServer.GetNotificationsAsync(userId, unreadOnly: true, limit: 10);
                m_Notifications = FilterNotificationsForCurrentDevices(unread);
            }
            finally
            {
                m_IsBusy = false;
            }
        }

        /// <summary>
        /// DismissAllAsync
        /// </summary>
        /// <returns>Returns Task.</returns>
        private async Task DismissAllAsync()
        {
            AuthenticationState authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            string? userIdValue = authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!Guid.TryParse(userIdValue, out Guid userId))
            {
                return;
            }

            m_IsBusy = true;

            try
            {
                await NotificationServer.MarkAllReadAsync(userId);

                m_Notifications = [];
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
