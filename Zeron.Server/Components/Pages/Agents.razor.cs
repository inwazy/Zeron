// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Zeron.Server.Data.Entities;
using Zeron.Server.ZCore;
using Zeron.Server.ZServers;
using Zeron.ZCore.Type;

namespace Zeron.Server.Components.Pages
{
    /// <summary>
    /// Agents
    /// </summary>
    public partial class Agents : IAsyncDisposable
    {
        // Agents.
        private List<AgentEntity> m_Agents = [];

        // Diagnostics.
        private List<AgentDiagnosticType> m_Diagnostics = [];

        // Hub connection.
        private HubConnection? m_HubConnection;

        // Refresh timer.
        private PeriodicTimer? m_RefreshTimer;

        // Refresh cancellation token source.
        private CancellationTokenSource? m_RefreshCts;

        /// <summary>
        /// OnInitializedAsync
        /// </summary>
        /// <returns>Returns Task.</returns>
        protected override async Task OnInitializedAsync()
        {
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
            m_Agents = await AgentManager.GetAgentsAsync();
            m_Diagnostics = await AgentDiagnosticServer.GetDiagnosticsAsync();
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

            m_HubConnection.On<string, string, string, DateTime>("AgentStatusChanged", async (_, __, ___, ____) =>
            {
                await ReloadAsync();
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
            m_RefreshCts?.Cancel();
            m_RefreshTimer?.Dispose();
            m_RefreshCts?.Dispose();
            await m_HubConnection?.DisposeAsync();
        }
    }
}
