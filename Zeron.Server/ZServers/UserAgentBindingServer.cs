// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Microsoft.EntityFrameworkCore;
using System.Globalization;
using Zeron.Server.Data;
using Zeron.Server.Data.Entities;
using Zeron.ZCore;
using Zeron.ZCore.Type;

namespace Zeron.Server.ZServers
{
    /// <summary>
    /// UserAgentBindingServer - Admin-managed user to Demand agent bindings.
    /// </summary>
    public class UserAgentBindingServer
    {
        // Database context.
        private readonly ZeronServerDbContext m_DbContext;

        /// <summary>
        /// UserAgentBindingServer
        /// </summary>
        /// <param name="dbContext"></param>
        /// <returns>Returns void.</returns>
        public UserAgentBindingServer(
            ZeronServerDbContext dbContext)
        {
            m_DbContext = dbContext;
        }

        /// <summary>
        /// GetBindingsAsync
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns bindings.</returns>
        public async Task<List<UserAgentBindingInfoType>> GetBindingsAsync(
            Guid? userId = null,
            CancellationToken cancellationToken = default)
        {
            IQueryable<UserAgentBindingEntity> query = m_DbContext.UserAgentBindings
                .Include(binding => binding.User)
                .AsQueryable();

            if (userId.HasValue)
            {
                query = query.Where(binding => binding.UserId == userId.Value);
            }

            List<UserAgentBindingEntity> bindings = await query.ToListAsync(cancellationToken);

            bindings = bindings
                .OrderBy(binding => binding.User?.Username ?? "")
                .ThenBy(binding => binding.AgentKey)
                .ToList();

            Dictionary<string, string?> machineNames = await LoadMachineNamesAsync(
                bindings.Select(binding => binding.AgentKey).Distinct(),
                cancellationToken);

            return bindings.Select(binding => ToInfo(binding, machineNames)).ToList();
        }

        /// <summary>
        /// CreateBindingAsync
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns binding or error.</returns>
        public async Task<(UserAgentBindingInfoType? Binding, string? Error)> CreateBindingAsync(
            UserAgentBindingRequestType request,
            CancellationToken cancellationToken = default)
        {
            if (!Guid.TryParse(request.UserId, out Guid userId))
            {
                return (null, "User id is required.");
            }

            string agentKey = (request.AgentKey ?? "").Trim();

            if (agentKey.Length == 0)
            {
                return (null, "Agent key is required.");
            }

            UserEntity? user = await m_DbContext.Users
                .FirstOrDefaultAsync(item => item.Id == userId, cancellationToken);

            if (user == null)
            {
                return (null, "User not found.");
            }

            bool alreadyBound = await m_DbContext.UserAgentBindings
                .AnyAsync(binding => binding.UserId == userId && binding.AgentKey == agentKey, cancellationToken);

            if (alreadyBound)
            {
                return (null, "User is already bound to this agent.");
            }

            UserAgentBindingEntity binding = new()
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                AgentKey = agentKey,
                BoundAt = DateTime.UtcNow,
                User = user
            };

            m_DbContext.UserAgentBindings.Add(binding);
            await m_DbContext.SaveChangesAsync(cancellationToken);

            ZNLogger.Common.Info(string.Format(CultureInfo.InvariantCulture,
                "UserAgentBindingServer bound user '{0}' to agent '{1}'.", user.Username, agentKey));

            Dictionary<string, string?> machineNames = await LoadMachineNamesAsync([agentKey], cancellationToken);

            return (ToInfo(binding, machineNames), null);
        }

        /// <summary>
        /// UnbindAsync
        /// </summary>
        /// <param name="bindingId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns error or null.</returns>
        public async Task<string?> UnbindAsync(
            Guid bindingId,
            CancellationToken cancellationToken = default)
        {
            UserAgentBindingEntity? binding = await m_DbContext.UserAgentBindings
                .FirstOrDefaultAsync(item => item.Id == bindingId, cancellationToken);

            if (binding == null)
            {
                return "Binding not found.";
            }

            m_DbContext.UserAgentBindings.Remove(binding);
            await m_DbContext.SaveChangesAsync(cancellationToken);

            ZNLogger.Common.Info(string.Format(CultureInfo.InvariantCulture,
                "UserAgentBindingServer unbound agent '{0}' from user '{1}'.",
                binding.AgentKey,
                binding.UserId));

            return null;
        }

        /// <summary>
        /// IsUserBoundToAgentAsync
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="agentKey"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns bool.</returns>
        public async Task<bool> IsUserBoundToAgentAsync(
            Guid userId,
            string agentKey,
            CancellationToken cancellationToken = default)
        {
            return await m_DbContext.UserAgentBindings.AnyAsync(
                binding => binding.UserId == userId && binding.AgentKey == agentKey,
                cancellationToken);
        }

        /// <summary>
        /// LoadMachineNamesAsync
        /// </summary>
        /// <param name="agentKeys"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns machine name map.</returns>
        private async Task<Dictionary<string, string?>> LoadMachineNamesAsync(
            IEnumerable<string> agentKeys,
            CancellationToken cancellationToken)
        {
            List<string> keys = agentKeys.Where(key => !string.IsNullOrWhiteSpace(key)).Distinct().ToList();

            if (keys.Count == 0)
            {
                return [];
            }

            List<AgentEntity> agents = await m_DbContext.Agents
                .Where(agent => keys.Contains(agent.AgentKey))
                .ToListAsync(cancellationToken);

            return agents.ToDictionary(agent => agent.AgentKey, agent => agent.MachineName);
        }

        /// <summary>
        /// ToInfo
        /// </summary>
        /// <param name="binding"></param>
        /// <param name="machineNames"></param>
        /// <returns>Returns UserAgentBindingInfoType.</returns>
        private static UserAgentBindingInfoType ToInfo(
            UserAgentBindingEntity binding,
            Dictionary<string, string?> machineNames)
        {
            machineNames.TryGetValue(binding.AgentKey, out string? machineName);

            return new UserAgentBindingInfoType
            {
                Id = binding.Id.ToString(),
                UserId = binding.UserId.ToString(),
                Username = binding.User?.Username,
                AgentKey = binding.AgentKey,
                MachineName = machineName,
                BoundAt = binding.BoundAt
            };
        }
    }
}
