// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Microsoft.AspNetCore.Components.Authorization;
using Zeron.Server.ZServers;
using Zeron.ZCore.Type;

namespace Zeron.Server.Components.Pages
{
    /// <summary>
    /// CatalogSyncHealth
    /// </summary>
    public partial class CatalogSyncHealth
    {
        // Summary.
        private CatalogSyncHealthSummaryType? m_Summary;

        // Filter.
        private string? m_Filter;

        // Message.
        private string? m_Message;

        // Succeeded.
        private bool m_Succeeded;

        // Busy.
        private bool m_IsBusy;

        /// <summary>
        /// FilteredAgents
        /// </summary>
        private List<CatalogSyncHealthItemType> FilteredAgents =>
            m_Summary == null
                ? []
                : string.IsNullOrWhiteSpace(m_Filter)
                    ? m_Summary.Agents
                    : m_Summary.Agents.Where(item => item.SyncState == m_Filter).ToList();

        /// <summary>
        /// OnInitializedAsync
        /// </summary>
        /// <returns>Returns Task.</returns>
        protected override async Task OnInitializedAsync()
        {
            await ReloadAsync();
        }

        /// <summary>
        /// SetFilter
        /// </summary>
        /// <param name="filter"></param>
        /// <returns>Returns void.</returns>
        private void SetFilter(
            string? filter)
        {
            m_Filter = filter;
        }

        /// <summary>
        /// ReloadAsync
        /// </summary>
        /// <returns>Returns Task.</returns>
        private async Task ReloadAsync()
        {
            m_IsBusy = true;

            try
            {
                m_Summary = await SyncHealthServer.GetHealthAsync();
            }
            finally
            {
                m_IsBusy = false;
            }
        }

        /// <summary>
        /// PushAsync
        /// </summary>
        /// <param name="onlyUnhealthy"></param>
        /// <returns>Returns Task.</returns>
        private async Task PushAsync(
            bool onlyUnhealthy)
        {
            m_IsBusy = true;
            m_Message = null;

            try
            {
                AuthenticationState authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
                CatalogSyncPushResponseType response = await SyncHealthServer.PushSyncAsync(
                    new CatalogSyncPushRequestType { OnlyUnhealthy = onlyUnhealthy },
                    AuditLogServer.FromPrincipal(authState.User));

                m_Succeeded = response.Success;
                m_Message = response.Message;
                await ReloadAsync();
            }
            finally
            {
                m_IsBusy = false;
            }
        }

        /// <summary>
        /// PushOneAsync
        /// </summary>
        /// <param name="agentKey"></param>
        /// <returns>Returns Task.</returns>
        private async Task PushOneAsync(
            string agentKey)
        {
            m_IsBusy = true;
            m_Message = null;

            try
            {
                AuthenticationState authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
                CatalogSyncPushResponseType response = await SyncHealthServer.PushSyncAsync(
                    new CatalogSyncPushRequestType
                    {
                        AgentKeys = [agentKey],
                        OnlyUnhealthy = false
                    },
                    AuditLogServer.FromPrincipal(authState.User));

                m_Succeeded = response.Success;
                m_Message = response.Message;
                await ReloadAsync();
            }
            finally
            {
                m_IsBusy = false;
            }
        }

    }
}
