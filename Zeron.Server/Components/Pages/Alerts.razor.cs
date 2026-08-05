// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Zeron.Server.Data.Entities;
using Zeron.Server.ZCore;
using Zeron.Server.ZCore.Type;
using Zeron.Server.ZServers;

namespace Zeron.Server.Components.Pages
{
    /// <summary>
    /// Alerts
    /// </summary>
    public partial class Alerts : IAsyncDisposable
    {
        // Alerts.
        private List<AlertEntity> m_Alerts = [];

        // Hub connection.
        private HubConnection? m_HubConnection;

        /// <summary>
        /// OnInitializedAsync
        /// </summary>
        /// <returns>Returns Task.</returns>
        protected override async Task OnInitializedAsync()
        {
            await LoadAlertsAsync(AlertStatusesType.Open);
            await ConnectHubAsync();
        }

        /// <summary>
        /// LoadAlertsAsync
        /// </summary>
        /// <param name="status"></param>
        /// <returns>Returns Task.</returns>
        private async Task LoadAlertsAsync(
            string? status)
        {
            m_Alerts = await AlertRuleServer.GetAlertsAsync(status, 100);
        }

        /// <summary>
        /// AcknowledgeAsync
        /// </summary>
        /// <param name="alertId"></param>
        /// <returns>Returns Task.</returns>
        private async Task AcknowledgeAsync(
            Guid alertId)
        {
            await AlertRuleServer.AcknowledgeAlertAsync(alertId);
            await LoadAlertsAsync(AlertStatusesType.Open);
        }

        /// <summary>
        /// ConnectHubAsync
        /// </summary>
        /// <returns>Returns Task.</returns>
        private async Task ConnectHubAsync()
        {
            m_HubConnection = DashboardHubClient.Create(Navigation, HttpContextAccessor);

            m_HubConnection.On<Guid, string, string?, string, string, string, string, DateTime>(
                "AlertReceived",
                async (_, __, ___, ____, _____, ______, _______, ________) =>
            {
                await LoadAlertsAsync(AlertStatusesType.Open);
                await InvokeAsync(StateHasChanged);
            });

            await DashboardHubClient.TryStartAsync(m_HubConnection);
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
