// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Zeron.Server.ZCore;
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
            app.MapPost("/api/agents/heartbeat", async (
                AgentHeartbeatRequestType request,
                HttpContext context,
                AgentManagerServer agentManager,
                AuthServer authServer) =>
            {
                IResult? authResult = context.ValidateAgentApiKey(authServer);

                if (authResult != null)
                {
                    return authResult;
                }

                string? ipAddress = context.Connection.RemoteIpAddress?.ToString();
                AgentHeartbeatResponseType response = await agentManager.ProcessHeartbeatAsync(request, ipAddress);

                return response.Success ? Results.Ok(response) : Results.BadRequest(response);
            });

            app.MapGet("/api/agents", async (AgentManagerServer agentManager) =>
            {
                return Results.Ok(await agentManager.GetAgentsAsync());
            }).RequireAuthorization(ServerPolicies.ViewerOrAbove);

            app.MapGet("/api/agents/diagnostics", async (AgentDiagnosticServer diagnosticServer) =>
            {
                return Results.Ok(await diagnosticServer.GetDiagnosticsAsync());
            }).RequireAuthorization(ServerPolicies.ViewerOrAbove);

            app.MapGet("/api/agents/{agentKey}", async (string agentKey, AgentManagerServer agentManager) =>
            {
                var agent = await agentManager.GetAgentByKeyAsync(agentKey);

                return agent == null ? Results.NotFound() : Results.Ok(agent);
            }).RequireAuthorization(ServerPolicies.ViewerOrAbove);

            app.MapGet("/api/agents/{agentKey}/diagnostics", async (
                string agentKey,
                AgentDiagnosticServer diagnosticServer) =>
            {
                AgentDiagnosticType? diagnostic = await diagnosticServer.GetDiagnosticAsync(agentKey);

                return diagnostic == null ? Results.NotFound() : Results.Ok(diagnostic);
            }).RequireAuthorization(ServerPolicies.ViewerOrAbove);
            
            app.MapPatch("/api/agents/{agentKey}", async (
                string agentKey,
                AgentUpdateRequestType request,
                AgentManagerServer agentManager) =>
            {
                var agent = await agentManager.UpdateAgentAsync(agentKey, request);

                return agent == null ? Results.NotFound() : Results.Ok(agent);
            }).RequireAuthorization(ServerPolicies.AdminOnly);

            return app;
        }
    }
}
