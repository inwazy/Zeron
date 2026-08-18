// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Microsoft.AspNetCore.SignalR.Client;
using Zeron.Server.Data.Entities;
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

        // Current page rows.
        private List<AlertEntity> m_PageRows = [];

        // Hub connection.
        private HubConnection? m_HubConnection;

        // Busy.
        private bool m_IsBusy;

        // Current status filter.
        private string? m_StatusFilter = AlertStatusesType.Open;

        // Pagination.
        private const int c_PageSize = 50;
        private int m_PageIndex;
        private bool m_HasNextPage;

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
            m_StatusFilter = status;
            m_PageIndex = 0;
            await ReloadPageAsync();
        }

        /// <summary>
        /// ReloadPageAsync
        /// </summary>
        /// <returns>Returns Task.</returns>
        private async Task ReloadPageAsync()
        {
            m_IsBusy = true;

            try
            {
                int offset = m_PageIndex * c_PageSize;
                m_Alerts = await AlertRuleServer.GetAlertsAsync(
                    m_StatusFilter,
                    limit: c_PageSize + 1,
                    offset: offset);

                m_HasNextPage = m_Alerts.Count > c_PageSize;
                m_PageRows = m_Alerts.Take(c_PageSize).ToList();
            }
            finally
            {
                m_IsBusy = false;
            }
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
            await ReloadPageAsync();
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
                await ReloadPageAsync();
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

        /// <summary>
        /// GoPrevPageAsync
        /// </summary>
        /// <returns>Returns Task.</returns>
        private async Task GoPrevPageAsync()
        {
            if (m_PageIndex <= 0)
            {
                return;
            }

            m_PageIndex--;
            await ReloadPageAsync();
        }

        /// <summary>
        /// GoNextPageAsync
        /// </summary>
        /// <returns>Returns Task.</returns>
        private async Task GoNextPageAsync()
        {
            if (!m_HasNextPage)
            {
                return;
            }

            m_PageIndex++;
            await ReloadPageAsync();
        }

        /// <summary>
        /// PageSummary
        /// </summary>
        private string PageSummary =>
            m_PageRows.Count == 0
                ? "No records"
                : $"Page {m_PageIndex + 1} · showing {m_PageRows.Count} alert(s)";
    }
}
