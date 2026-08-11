// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using System.Security.Claims;
using Zeron.Server.ZCore;
using Zeron.Server.ZServers;
using Zeron.ZCore.Type;

namespace Zeron.Server.Endpoints
{
    /// <summary>
    /// DevicePortalEndpoints
    /// </summary>
    public static class DevicePortalEndpoints
    {
        /// <summary>
        /// MapDevicePortalEndpoints
        /// </summary>
        /// <param name="app"></param>
        /// <returns>Returns WebApplication.</returns>
        public static WebApplication MapDevicePortalEndpoints(
            this WebApplication app)
        {
            app.MapGet("/api/my/devices", async (
                ClaimsPrincipal user,
                DevicePortalServer portalServer) =>
            {
                if (!TryGetUserId(user, out Guid userId))
                {
                    return Results.Unauthorized();
                }

                return Results.Ok(await portalServer.GetMyDevicesAsync(userId));
            }).RequireAuthorization(ServerPolicies.DeviceOwnerOrStaff);

            app.MapGet("/api/my/devices/{agentKey}", async (
                string agentKey,
                ClaimsPrincipal user,
                DevicePortalServer portalServer) =>
            {
                if (!TryGetUserId(user, out Guid userId))
                {
                    return Results.Unauthorized();
                }

                var device = await portalServer.GetMyDeviceAsync(userId, agentKey);

                return device == null ? Results.NotFound() : Results.Ok(device);
            }).RequireAuthorization(ServerPolicies.DeviceOwnerOrStaff);

            app.MapGet("/api/my/devices/{agentKey}/install-events", async (
                string agentKey,
                int? limit,
                ClaimsPrincipal user,
                DevicePortalServer portalServer) =>
            {
                if (!TryGetUserId(user, out Guid userId))
                {
                    return Results.Unauthorized();
                }

                var events = await portalServer.GetMyInstallEventsAsync(userId, agentKey, limit ?? 20);

                return events == null ? Results.NotFound() : Results.Ok(events);
            }).RequireAuthorization(ServerPolicies.DeviceOwnerOrStaff);

            app.MapPost("/api/my/devices/{agentKey}/deploy", async (
                string agentKey,
                DeviceDeployRequestType request,
                ClaimsPrincipal user,
                DevicePortalServer portalServer) =>
            {
                if (!TryGetUserId(user, out Guid userId))
                {
                    return Results.Unauthorized();
                }

                (PackageDeployResponseType? response, string? error) = await portalServer.DeployToMyDeviceAsync(
                    userId,
                    agentKey,
                    request);

                if (error != null && response == null)
                {
                    return Results.NotFound(new { success = false, message = error });
                }

                if (response == null || !response.Success)
                {
                    return Results.BadRequest(response ?? new PackageDeployResponseType
                    {
                        Success = false,
                        Message = error ?? "Deploy failed."
                    });
                }

                return Results.Created($"/api/tasks/{response.TaskId}", response);
            }).RequireAuthorization(ServerPolicies.DeviceOwnerOrStaff);

            app.MapGet("/api/my/notifications", async (
                bool? unreadOnly,
                int? limit,
                ClaimsPrincipal user,
                UserNotificationServer notificationServer) =>
            {
                if (!TryGetUserId(user, out Guid userId))
                {
                    return Results.Unauthorized();
                }

                return Results.Ok(await notificationServer.GetNotificationsAsync(
                    userId,
                    unreadOnly ?? false,
                    limit ?? 20));
            }).RequireAuthorization(ServerPolicies.DeviceOwnerOrStaff);

            app.MapPost("/api/my/notifications/{id:guid}/read", async (
                Guid id,
                ClaimsPrincipal user,
                UserNotificationServer notificationServer) =>
            {
                if (!TryGetUserId(user, out Guid userId))
                {
                    return Results.Unauthorized();
                }

                bool ok = await notificationServer.MarkReadAsync(userId, id);

                return ok ? Results.Ok(new { success = true }) : Results.NotFound();
            }).RequireAuthorization(ServerPolicies.DeviceOwnerOrStaff);

            app.MapPost("/api/my/notifications/read-all", async (
                ClaimsPrincipal user,
                UserNotificationServer notificationServer) =>
            {
                if (!TryGetUserId(user, out Guid userId))
                {
                    return Results.Unauthorized();
                }

                int count = await notificationServer.MarkAllReadAsync(userId);

                return Results.Ok(new { success = true, count });
            }).RequireAuthorization(ServerPolicies.DeviceOwnerOrStaff);

            return app;
        }

        /// <summary>
        /// TryGetUserId
        /// </summary>
        /// <param name="user"></param>
        /// <param name="userId"></param>
        /// <returns>Returns bool.</returns>
        private static bool TryGetUserId(
            ClaimsPrincipal user,
            out Guid userId)
        {
            string? value = user.FindFirstValue(ClaimTypes.NameIdentifier);

            return Guid.TryParse(value, out userId);
        }
    }
}
