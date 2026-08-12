// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Zeron.Server.ZServers;
using Zeron.ZCore.Type;

namespace Zeron.Server.Components.Pages
{
    /// <summary>
    /// Home
    /// </summary>
    public partial class Home : IAsyncDisposable
    {
        // Summary.
        private DashboardSummaryType? m_Summary;

        // Hub connection.
        private HubConnection? m_HubConnection;

        // Refresh timer.
        private PeriodicTimer? m_RefreshTimer;

        // Refresh cancellation token source.
        private CancellationTokenSource? m_RefreshCts;

        // Is loading.
        private bool m_IsLoading;

        // Password changed.
        private bool m_PasswordChanged;

        // Password changed query.
        [SupplyParameterFromQuery(Name = "passwordChanged")]
        public string? PasswordChanged { get; set; }

        /// <summary>
        /// OnInitializedAsync
        /// </summary>
        /// <returns>Returns Task.</returns>
        protected override async Task OnInitializedAsync()
        {
            m_PasswordChanged = PasswordChanged == "1";

            await ReloadAsync();
            await ConnectHubAsync();

            StartRefreshTimer();
        }

        /// <summary>
        /// ReloadAsync
        /// </summary>
        /// <returns>Returns Task.</returns>
        private async Task ReloadAsync()
        {
            m_IsLoading = true;

            try
            {
                m_Summary = await DashboardSummaryServer.GetSummaryAsync();
            }
            finally
            {
                m_IsLoading = false;
            }
        }

        /// <summary>
        /// StartRefreshTimer
        /// </summary>
        /// <returns>Returns void.</returns>
        private void StartRefreshTimer()
        {
            m_RefreshCts = new CancellationTokenSource();
            m_RefreshTimer = new PeriodicTimer(TimeSpan.FromSeconds(15));
            _ = RunRefreshLoopAsync(m_RefreshCts.Token);
        }

        /// <summary>
        /// RunRefreshLoopAsync
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns Task.</returns>
        private async Task RunRefreshLoopAsync(
            CancellationToken cancellationToken)
        {
            try
            {
                while (await m_RefreshTimer!.WaitForNextTickAsync(cancellationToken))
                {
                    await ReloadAsync();
                    await InvokeAsync(StateHasChanged);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        /// <summary>
        /// ConnectHubAsync
        /// </summary>
        /// <returns>Returns Task.</returns>
        private async Task ConnectHubAsync()
        {
            m_HubConnection = DashboardHubClient.Create(Navigation, HttpContextAccessor);

            m_HubConnection.On<object>("AgentStatusChanged", async _ => await RefreshFromHubAsync());
            m_HubConnection.On<object>("AlertReceived", async _ => await RefreshFromHubAsync());
            m_HubConnection.On<object>("EventReceived", async _ => await RefreshFromHubAsync());

            await DashboardHubClient.TryStartAsync(m_HubConnection);
        }

        /// <summary>
        /// RefreshFromHubAsync
        /// </summary>
        /// <returns>Returns Task.</returns>
        private async Task RefreshFromHubAsync()
        {
            await ReloadAsync();
            await InvokeAsync(StateHasChanged);
        }

        /// <summary>
        /// DisposeAsync
        /// </summary>
        /// <returns>Returns ValueTask.</returns>
        public async ValueTask DisposeAsync()
        {
            m_RefreshCts?.Cancel();
            m_RefreshTimer?.Dispose();
            m_RefreshCts?.Dispose();

            if (m_HubConnection != null)
            {
                await m_HubConnection.DisposeAsync();
            }
        }
    }
}
