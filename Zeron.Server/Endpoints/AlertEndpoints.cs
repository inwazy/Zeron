// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Zeron.Server.ZCore;
using Zeron.Server.ZServers;

namespace Zeron.Server.Endpoints
{
    /// <summary>
    /// AlertEndpoints
    /// </summary>
    public static class AlertEndpoints
    {
        /// <summary>
        /// MapAlertEndpoints
        /// </summary>
        /// <param name="app"></param>
        /// <returns>Returns WebApplication.</returns>
        public static WebApplication MapAlertEndpoints(
            this WebApplication app)
        {
            app.MapGet("/api/alerts", async (string? status, int? limit, AlertRuleServer alertRuleServer) =>
            {
                int take = limit.GetValueOrDefault(100);

                return Results.Ok(await alertRuleServer.GetAlertsAsync(status, take));
            }).RequireAuthorization(ServerPolicies.ViewerOrAbove);

            app.MapGet("/api/alerts/count", async (AlertRuleServer alertRuleServer) =>
            {
                return Results.Ok(new { open = await alertRuleServer.GetOpenAlertCountAsync() });
            }).RequireAuthorization(ServerPolicies.ViewerOrAbove);

            app.MapPost("/api/alerts/{alertId:guid}/acknowledge", async (Guid alertId, AlertRuleServer alertRuleServer) =>
            {
                bool acknowledged = await alertRuleServer.AcknowledgeAlertAsync(alertId);

                return acknowledged ? Results.Ok(new { success = true }) : Results.BadRequest(new { success = false });
            }).RequireAuthorization(ServerPolicies.OperatorOrAbove);

            app.MapGet("/api/agents/{agentKey}/heartbeats", async (
                string agentKey,
                int? limit,
                AgentManagerServer agentManager) =>
            {
                int take = limit.GetValueOrDefault(50);

                return Results.Ok(await agentManager.GetAgentHeartbeatsAsync(agentKey, take));
            }).RequireAuthorization(ServerPolicies.ViewerOrAbove);

            return app;
        }
    }
}
