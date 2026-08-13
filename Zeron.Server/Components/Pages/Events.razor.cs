// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Zeron.Server.Data.Entities;
using Zeron.Server.ZServers;

namespace Zeron.Server.Components.Pages
{
    /// <summary>
    /// Events
    /// </summary>
    public partial class Events : IAsyncDisposable
    {
        // Events.
        private List<EventEntity> m_Events = [];

        // Agent key.
        private string? m_AgentKey;

        // Topic.
        private string? m_Topic;

        // Hub connection.
        private HubConnection? m_HubConnection;

        // Topic query.
        [SupplyParameterFromQuery(Name = "topic")]
        public string? TopicQuery { get; set; }

        // Agent key query.
        [SupplyParameterFromQuery(Name = "agentKey")]
        public string? AgentKeyQuery { get; set; }

        /// <summary>
        /// OnInitializedAsync
        /// </summary>
        /// <returns>Returns Task.</returns>
        protected override async Task OnInitializedAsync()
        {
            if (!string.IsNullOrWhiteSpace(TopicQuery))
            {
                m_Topic = TopicQuery;
            }

            if (!string.IsNullOrWhiteSpace(AgentKeyQuery))
            {
                m_AgentKey = AgentKeyQuery;
            }

            await ReloadAsync();
            await ConnectHubAsync();
        }

        /// <summary>
        /// ReloadAsync
        /// </summary>
        /// <returns>Returns Task.</returns>
        private async Task ReloadAsync()
        {
            m_Events = await EventIngestor.GetEventsAsync(m_AgentKey, m_Topic, 100);
        }

        /// <summary>
        /// ConnectHubAsync
        /// </summary>
        /// <returns>Returns Task.</returns>
        private async Task ConnectHubAsync()
        {
            m_HubConnection = DashboardHubClient.Create(Navigation, HttpContextAccessor);

            m_HubConnection.On<long, string?, string, string, DateTime>("EventReceived", async (_, agentKey, topic, payload, receivedAt) =>
            {
                if (!string.IsNullOrWhiteSpace(m_AgentKey) && m_AgentKey != agentKey)
                {
                    return;
                }

                if (!string.IsNullOrWhiteSpace(m_Topic) && !topic.Contains(m_Topic, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

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
            if (m_HubConnection != null)
            {
                await m_HubConnection.DisposeAsync();
            }
        }
    }
}
