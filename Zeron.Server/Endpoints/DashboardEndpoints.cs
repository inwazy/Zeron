// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Zeron.Server.ZCore;
using Zeron.Server.ZServers;

namespace Zeron.Server.Endpoints
{
    /// <summary>
    /// DashboardEndpoints
    /// </summary>
    public static class DashboardEndpoints
    {
        /// <summary>
        /// MapDashboardEndpoints
        /// </summary>
        /// <param name="app"></param>
        /// <returns>Returns WebApplication.</returns>
        public static WebApplication MapDashboardEndpoints(
            this WebApplication app)
        {
            app.MapGet("/api/dashboard/summary", async (DashboardSummaryServer summaryServer) =>
            {
                return Results.Ok(await summaryServer.GetSummaryAsync());
            }).RequireAuthorization(ServerPolicies.ViewerOrAbove);

            return app;
        }
    }
}
