// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Zeron.Server.ZCore;
using Zeron.Server.ZServers;

namespace Zeron.Server.Endpoints
{
    /// <summary>
    /// AuditEndpoints
    /// </summary>
    public static class AuditEndpoints
    {
        /// <summary>
        /// MapAuditEndpoints
        /// </summary>
        /// <param name="app"></param>
        /// <returns>Returns WebApplication.</returns>
        public static WebApplication MapAuditEndpoints(
            this WebApplication app)
        {
            app.MapGet("/api/audit", async (
                string? action,
                string? actor,
                string? target,
                string? source,
                int? limit,
                AuditLogServer auditLogServer) =>
            {
                return Results.Ok(await auditLogServer.QueryAsync(
                    action,
                    actor,
                    target,
                    source,
                    limit ?? 100));
            }).RequireAuthorization(ServerPolicies.ViewerOrAbove);

            return app;
        }
    }
}
