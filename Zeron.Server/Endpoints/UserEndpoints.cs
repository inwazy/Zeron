// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using System.Security.Claims;
using Zeron.Server.ZCore;
using Zeron.Server.ZServers;
using Zeron.ZCore.Type;

namespace Zeron.Server.Endpoints
{
    /// <summary>
    /// UserEndpoints
    /// </summary>
    public static class UserEndpoints
    {
        /// <summary>
        /// MapUserEndpoints
        /// </summary>
        /// <param name="app"></param>
        /// <returns>Returns WebApplication.</returns>
        public static WebApplication MapUserEndpoints(
            this WebApplication app)
        {
            app.MapGet("/api/users", async (UserManagerServer userManager) =>
            {
                return Results.Ok(await userManager.GetUsersAsync());
            }).RequireAuthorization(ServerPolicies.AdminOnly);

            app.MapGet("/api/users/{userId:guid}", async (Guid userId, UserManagerServer userManager) =>
            {
                UserInfoType? user = await userManager.GetUserAsync(userId);

                return user == null ? Results.NotFound() : Results.Ok(user);
            }).RequireAuthorization(ServerPolicies.AdminOnly);

            app.MapPost("/api/users", async (UserCreateRequestType request, UserManagerServer userManager) =>
            {
                (UserInfoType? user, string? error) = await userManager.CreateUserAsync(request);

                if (error != null)
                {
                    return Results.BadRequest(new { success = false, message = error });
                }

                return Results.Created($"/api/users/{user!.Id}", user);
            }).RequireAuthorization(ServerPolicies.AdminOnly);

            app.MapPatch("/api/users/{userId:guid}", async (
                Guid userId,
                UserUpdateRequestType request,
                ClaimsPrincipal principal,
                UserManagerServer userManager) =>
            {
                Guid? actorUserId = null;
                string? actorId = principal.FindFirstValue(ClaimTypes.NameIdentifier);

                if (Guid.TryParse(actorId, out Guid parsedActorId))
                {
                    actorUserId = parsedActorId;
                }

                (UserInfoType? user, string? error) = await userManager.UpdateUserAsync(
                    userId,
                    request,
                    actorUserId);

                if (error == "User not found.")
                {
                    return Results.NotFound();
                }

                if (error != null)
                {
                    return Results.BadRequest(new { success = false, message = error });
                }

                return Results.Ok(user);
            }).RequireAuthorization(ServerPolicies.AdminOnly);

            return app;
        }
    }
}
