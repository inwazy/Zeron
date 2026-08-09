// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using System.Security.Claims;
using Zeron.Server.ZCore;
using Zeron.Server.ZServers;
using Zeron.ZCore.Type;

namespace Zeron.Server.Endpoints
{
    /// <summary>
    /// UserAgentBindingEndpoints
    /// </summary>
    public static class UserAgentBindingEndpoints
    {
        /// <summary>
        /// MapUserAgentBindingEndpoints
        /// </summary>
        /// <param name="app"></param>
        /// <returns>Returns WebApplication.</returns>
        public static WebApplication MapUserAgentBindingEndpoints(
            this WebApplication app)
        {
            app.MapGet("/api/user-agent-bindings", async (
                Guid? userId,
                UserAgentBindingServer bindingServer) =>
            {
                return Results.Ok(await bindingServer.GetBindingsAsync(userId));
            }).RequireAuthorization(ServerPolicies.AdminOnly);

            app.MapPost("/api/user-agent-bindings", async (
                UserAgentBindingRequestType request,
                ClaimsPrincipal principal,
                UserAgentBindingServer bindingServer) =>
            {
                (UserAgentBindingInfoType? binding, string? error) = await bindingServer.CreateBindingAsync(
                    request,
                    actor: AuditLogServer.FromPrincipal(principal));

                if (error != null)
                {
                    return Results.BadRequest(new { success = false, message = error });
                }

                return Results.Created($"/api/user-agent-bindings/{binding!.Id}", binding);
            }).RequireAuthorization(ServerPolicies.AdminOnly);

            app.MapDelete("/api/user-agent-bindings/{bindingId:guid}", async (
                Guid bindingId,
                ClaimsPrincipal principal,
                UserAgentBindingServer bindingServer) =>
            {
                string? error = await bindingServer.UnbindAsync(
                    bindingId,
                    actor: AuditLogServer.FromPrincipal(principal));

                if (error != null)
                {
                    return Results.NotFound(new { success = false, message = error });
                }

                return Results.Ok(new { success = true });
            }).RequireAuthorization(ServerPolicies.AdminOnly);

            return app;
        }
    }
}
