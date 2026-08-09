// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Microsoft.EntityFrameworkCore;
using Zeron.Server.Data;
using Zeron.Server.Data.Entities;
using Zeron.ZCore.Type;

namespace Zeron.Server.ZServers
{
    /// <summary>
    /// DevicePortalServer - self-service Demand status for bound users.
    /// </summary>
    public class DevicePortalServer
    {
        // Database context.
        private readonly ZeronServerDbContext m_DbContext;

        // Binding server.
        private readonly UserAgentBindingServer m_BindingServer;

        // Package deploy server (install events).
        private readonly PackageDeployServer m_PackageDeployServer;

        /// <summary>
        /// DevicePortalServer
        /// </summary>
        /// <param name="dbContext"></param>
        /// <param name="bindingServer"></param>
        /// <param name="packageDeployServer"></param>
        /// <returns>Returns void.</returns>
        public DevicePortalServer(
            ZeronServerDbContext dbContext,
            UserAgentBindingServer bindingServer,
            PackageDeployServer packageDeployServer)
        {
            m_DbContext = dbContext;
            m_BindingServer = bindingServer;
            m_PackageDeployServer = packageDeployServer;
        }

        /// <summary>
        /// GetMyDevicesAsync
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns bound device statuses.</returns>
        public async Task<List<DeviceAgentStatusType>> GetMyDevicesAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            List<UserAgentBindingInfoType> bindings = await m_BindingServer.GetBindingsAsync(
                userId,
                cancellationToken);

            List<DeviceAgentStatusType> devices = [];

            foreach (UserAgentBindingInfoType binding in bindings)
            {
                if (string.IsNullOrWhiteSpace(binding.AgentKey))
                {
                    continue;
                }

                DeviceAgentStatusType? status = await BuildStatusAsync(binding.AgentKey, cancellationToken);

                if (status != null)
                {
                    devices.Add(status);
                }
                else
                {
                    devices.Add(new DeviceAgentStatusType
                    {
                        AgentKey = binding.AgentKey,
                        MachineName = binding.MachineName,
                        Status = "unknown"
                    });
                }
            }

            return devices
                .OrderBy(device => device.MachineName ?? device.AgentKey)
                .ToList();
        }

        /// <summary>
        /// GetMyDeviceAsync
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="agentKey"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns status or null when unbound/missing.</returns>
        public async Task<DeviceAgentStatusType?> GetMyDeviceAsync(
            Guid userId,
            string agentKey,
            CancellationToken cancellationToken = default)
        {
            if (!await m_BindingServer.IsUserBoundToAgentAsync(userId, agentKey, cancellationToken))
            {
                return null;
            }

            return await BuildStatusAsync(agentKey, cancellationToken)
                ?? new DeviceAgentStatusType
                {
                    AgentKey = agentKey,
                    Status = "unknown"
                };
        }

        /// <summary>
        /// GetMyInstallEventsAsync
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="agentKey"></param>
        /// <param name="limit"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns install events or null when unbound.</returns>
        public async Task<List<EventEntity>?> GetMyInstallEventsAsync(
            Guid userId,
            string agentKey,
            int limit = 20,
            CancellationToken cancellationToken = default)
        {
            if (!await m_BindingServer.IsUserBoundToAgentAsync(userId, agentKey, cancellationToken))
            {
                return null;
            }

            return await m_PackageDeployServer.GetInstallEventsAsync(
                packageName: null,
                agentKey: agentKey,
                limit: limit,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// DeployToMyDeviceAsync - self-service install/uninstall for a bound agent.
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="agentKey"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns deploy response or authorization error.</returns>
        public async Task<(PackageDeployResponseType? Response, string? Error)> DeployToMyDeviceAsync(
            Guid userId,
            string agentKey,
            DeviceDeployRequestType request,
            CancellationToken cancellationToken = default)
        {
            if (!await m_BindingServer.IsUserBoundToAgentAsync(userId, agentKey, cancellationToken))
            {
                return (null, "Device is not bound to your account.");
            }

            PackageDeployResponseType response = await m_PackageDeployServer.DeployAsync(new PackageDeployRequestType
            {
                Operation = request.Operation,
                PackageName = request.PackageName,
                ExtraArgs = request.ExtraArgs,
                TargetType = "agent",
                AgentIds = [agentKey],
                Name = $"self-{request.Operation}-{request.PackageName}-{DateTime.UtcNow:yyyyMMddHHmmss}"
            }, cancellationToken);

            return response.Success
                ? (response, null)
                : (response, response.Message);
        }

        /// <summary>
        /// BuildStatusAsync
        /// </summary>
        /// <param name="agentKey"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns status or null when agent unknown.</returns>
        private async Task<DeviceAgentStatusType?> BuildStatusAsync(
            string agentKey,
            CancellationToken cancellationToken)
        {
            AgentEntity? agent = await m_DbContext.Agents
                .FirstOrDefaultAsync(item => item.AgentKey == agentKey, cancellationToken);

            if (agent == null)
            {
                return null;
            }

            AgentHeartbeatEntity? heartbeat = await m_DbContext.AgentHeartbeats
                .Where(item => item.AgentId == agent.Id)
                .OrderByDescending(item => item.ReportedAt)
                .FirstOrDefaultAsync(cancellationToken);

            return new DeviceAgentStatusType
            {
                AgentKey = agent.AgentKey,
                MachineName = agent.MachineName,
                Status = agent.Status,
                Version = agent.Version,
                IpAddress = agent.IpAddress,
                LastHeartbeatAt = agent.LastHeartbeatAt == default ? null : agent.LastHeartbeatAt,
                InstallQueueCount = heartbeat?.InstallQueueCount ?? 0,
                InstallRunning = heartbeat?.InstallRunning ?? false,
                SchedulerTaskCount = heartbeat?.SchedulerTaskCount ?? 0,
                UptimeSeconds = heartbeat?.UptimeSeconds ?? 0,
                LastCatalogSyncAt = agent.LastCatalogSyncAt
            };
        }
    }
}
