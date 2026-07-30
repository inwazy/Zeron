// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Microsoft.AspNetCore.SignalR;
using Zeron.Server.Data.Entities;
using Zeron.Server.Hubs;
using Zeron.Server.ZInterfaces;

namespace Zeron.Server.ZServers
{
    /// <summary>
    /// DashboardNotifierServer
    /// </summary>
    public class DashboardNotifierServer : IDashboardNotifier
    {
        // HubContext
        private readonly IHubContext<DashboardHub> m_HubContext;

        /// <summary>
        /// DashboardNotifierServer
        /// </summary>
        /// <param name="hubContext"></param>
        /// <returns>Returns void.</returns>
        public DashboardNotifierServer(IHubContext<DashboardHub> hubContext)
        {
            m_HubContext = hubContext;
        }

        /// <summary>
        /// NotifyEventAsync
        /// </summary>
        /// <param name="eventEntity"></param>
        /// <returns>Returns void.</returns>
        public async Task NotifyEventAsync(EventEntity eventEntity)
        {
            await m_HubContext.Clients.All.SendAsync("EventReceived", new
            {
                eventEntity.Id,
                AgentKey = eventEntity.Agent?.AgentKey,
                eventEntity.Topic,
                eventEntity.Payload,
                eventEntity.ReceivedAt
            });
        }

        /// <summary>
        /// NotifyAgentStatusAsync
        /// </summary>
        /// <param name="agent"></param>
        /// <returns>Returns void.</returns>
        public async Task NotifyAgentStatusAsync(AgentEntity agent)
        {
            await m_HubContext.Clients.All.SendAsync("AgentStatusChanged", new
            {
                agent.AgentKey,
                agent.MachineName,
                agent.Status,
                agent.LastHeartbeatAt
            });
        }

        /// <summary>
        /// NotifyAlertAsync
        /// </summary>
        /// <param name="alert"></param>
        /// <returns>Returns void.</returns>
        public async Task NotifyAlertAsync(AlertEntity alert)
        {
            await m_HubContext.Clients.All.SendAsync("AlertReceived", new
            {
                alert.Id,
                alert.RuleType,
                alert.AgentKey,
                alert.Title,
                alert.Message,
                alert.Severity,
                alert.Status,
                alert.CreatedAt
            });
        }
    }
}
