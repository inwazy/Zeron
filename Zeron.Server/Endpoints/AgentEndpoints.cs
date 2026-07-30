// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Zeron.Server.ZServers;
using Zeron.ZCore.Type;

namespace Zeron.Server.Endpoints
{
    /// <summary>
    /// AgentEndpoints
    /// </summary>
    public static class AgentEndpoints
    {
        /// <summary>
        /// MapAgentEndpoints
        /// </summary>
        /// <param name="app"></param>
        /// <returns>Returns WebApplication.</returns>
        public static WebApplication MapAgentEndpoints(this WebApplication app)
        {
            app.MapPost("/api/agents/heartbeat", async (AgentHeartbeatRequestType request, HttpContext context, AgentManagerServer agentManager) =>
            {
                string? ipAddress = context.Connection.RemoteIpAddress?.ToString();
                AgentHeartbeatResponseType response = await agentManager.ProcessHeartbeatAsync(request, ipAddress);

                return response.Success ? Results.Ok(response) : Results.BadRequest(response);
            });

            app.MapGet("/api/agents", async (AgentManagerServer agentManager) =>
            {
                return Results.Ok(await agentManager.GetAgentsAsync());
            });

            app.MapGet("/api/agents/{agentKey}", async (string agentKey, AgentManagerServer agentManager) =>
            {
                var agent = await agentManager.GetAgentByKeyAsync(agentKey);

                return agent == null ? Results.NotFound() : Results.Ok(agent);
            });

            return app;
        }
    }
}
