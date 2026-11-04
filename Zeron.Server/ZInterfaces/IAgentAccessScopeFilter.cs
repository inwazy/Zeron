// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using System.Security.Claims;

namespace Zeron.Server.ZInterfaces
{
    /// <summary>
    /// Optional agent-resource scope filter (enterprise editions register an implementation).
    /// null allow-list means unrestricted; empty set means no agents.
    /// </summary>
    public interface IAgentAccessScopeFilter
    {
        /// <summary>
        /// Returns allowed agent keys for the caller, or null when unrestricted.
        /// </summary>
        /// <param name="user">Caller principal.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Allow-list or null.</returns>
        Task<IReadOnlySet<string>?> GetAllowedAgentKeysAsync(
            ClaimsPrincipal? user,
            CancellationToken cancellationToken = default);
    }
}
