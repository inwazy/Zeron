// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using System.Security.Claims;
using Zeron.Server.ZCore;
using Zeron.Server.ZServers;
using Zeron.ZCore.Type;

namespace Zeron.Server.Endpoints
{
    /// <summary>
    /// PackageEndpoints
    /// </summary>
    public static class PackageEndpoints
    {
        /// <summary>
        /// MapPackageEndpoints
        /// </summary>
        /// <param name="app"></param>
        /// <returns>Returns WebApplication.</returns>
        public static WebApplication MapPackageEndpoints(this WebApplication app)
        {
            app.MapGet("/api/packages/catalog", async (
                bool? enabledOnly,
                ManagedPackageCatalogServer catalogServer) =>
            {
                return Results.Ok(await catalogServer.GetPackagesAsync(enabledOnly ?? false));
            }).RequireAuthorization(ServerPolicies.ViewerOrAbove);

            app.MapGet("/api/packages/catalog/sync-health", async (CatalogSyncHealthServer syncHealthServer) =>
            {
                return Results.Ok(await syncHealthServer.GetHealthAsync());
            }).RequireAuthorization(ServerPolicies.ViewerOrAbove);

            app.MapPost("/api/packages/catalog/sync-push", async (
                CatalogSyncPushRequestType? request,
                ClaimsPrincipal principal,
                CatalogSyncHealthServer syncHealthServer) =>
            {
                CatalogSyncPushResponseType response = await syncHealthServer.PushSyncAsync(
                    request,
                    AuditLogServer.FromPrincipal(principal));

                return Results.Ok(response);
            }).RequireAuthorization(ServerPolicies.OperatorOrAbove);

            app.MapGet("/api/packages/catalog/sync", async (
                HttpContext context,
                ManagedPackageCatalogServer catalogServer,
                AuthServer authServer,
                ServerSettings settings) =>
            {
                IResult? authResult = await context.ValidateAgentRequestAsync(authServer, settings);

                if (authResult != null)
                {
                    return authResult;
                }

                return Results.Ok(await catalogServer.GetCatalogSyncAsync());
            });

            app.MapGet("/api/packages/catalog/{packageId:guid}", async (
                Guid packageId,
                ManagedPackageCatalogServer catalogServer) =>
            {
                ManagedPackageInfoType? package = await catalogServer.GetPackageAsync(packageId);

                return package == null ? Results.NotFound() : Results.Ok(package);
            }).RequireAuthorization(ServerPolicies.ViewerOrAbove);

            app.MapPost("/api/packages/catalog", async (
                ManagedPackageUpsertRequestType request,
                ClaimsPrincipal principal,
                ManagedPackageCatalogServer catalogServer) =>
            {
                (ManagedPackageInfoType? package, string? error) = await catalogServer.CreatePackageAsync(
                    request,
                    actor: AuditLogServer.FromPrincipal(principal));

                if (error != null)
                {
                    return Results.BadRequest(new { success = false, message = error });
                }

                return Results.Created($"/api/packages/catalog/{package!.Id}", package);
            }).RequireAuthorization(ServerPolicies.OperatorOrAbove);

            app.MapPut("/api/packages/catalog/{packageId:guid}", async (
                Guid packageId,
                ManagedPackageUpsertRequestType request,
                ClaimsPrincipal principal,
                ManagedPackageCatalogServer catalogServer) =>
            {
                (ManagedPackageInfoType? package, string? error) = await catalogServer.UpdatePackageAsync(
                    packageId,
                    request,
                    actor: AuditLogServer.FromPrincipal(principal));

                if (error != null)
                {
                    return error == "Package not found."
                        ? Results.NotFound(new { success = false, message = error })
                        : Results.BadRequest(new { success = false, message = error });
                }

                return Results.Ok(package);
            }).RequireAuthorization(ServerPolicies.OperatorOrAbove);

            app.MapDelete("/api/packages/catalog/{packageId:guid}", async (
                Guid packageId,
                ClaimsPrincipal principal,
                ManagedPackageCatalogServer catalogServer) =>
            {
                string? error = await catalogServer.DeletePackageAsync(
                    packageId,
                    actor: AuditLogServer.FromPrincipal(principal));

                if (error != null)
                {
                    return Results.NotFound(new { success = false, message = error });
                }

                return Results.Ok(new { success = true });
            }).RequireAuthorization(ServerPolicies.OperatorOrAbove);

            app.MapGet("/api/packages/catalog/{packageId:guid}/versions", async (
                Guid packageId,
                ManagedPackageCatalogServer catalogServer) =>
            {
                ManagedPackageInfoType? package = await catalogServer.GetPackageAsync(packageId);

                if (package == null)
                {
                    return Results.NotFound();
                }

                return Results.Ok(await catalogServer.GetPackageVersionsAsync(packageId));
            }).RequireAuthorization(ServerPolicies.ViewerOrAbove);

            app.MapGet("/api/packages/catalog/{packageId:guid}/versions/{versionNumber:int}", async (
                Guid packageId,
                int versionNumber,
                ManagedPackageCatalogServer catalogServer) =>
            {
                ManagedPackageVersionInfoType? version = await catalogServer.GetPackageVersionAsync(
                    packageId,
                    versionNumber);

                return version == null ? Results.NotFound() : Results.Ok(version);
            }).RequireAuthorization(ServerPolicies.ViewerOrAbove);

            app.MapPost("/api/packages/catalog/{packageId:guid}/rollback", async (
                Guid packageId,
                ManagedPackageRollbackRequestType request,
                ClaimsPrincipal principal,
                ManagedPackageCatalogServer catalogServer) =>
            {
                (ManagedPackageInfoType? package, string? error) = await catalogServer.RollbackPackageAsync(
                    packageId,
                    request.VersionNumber,
                    actor: AuditLogServer.FromPrincipal(principal));

                if (error != null)
                {
                    return error is "Package not found." or "Version not found."
                        ? Results.NotFound(new { success = false, message = error })
                        : Results.BadRequest(new { success = false, message = error });
                }

                return Results.Ok(package);
            }).RequireAuthorization(ServerPolicies.OperatorOrAbove);

            app.MapPost("/api/packages/deploy", async (
                PackageDeployRequestType request,
                ClaimsPrincipal principal,
                PackageDeployServer packageDeployServer) =>
            {
                PackageDeployResponseType response = await packageDeployServer.DeployAsync(
                    request,
                    actor: AuditLogServer.FromPrincipal(principal));

                return response.Success
                    ? Results.Created($"/api/tasks/{response.TaskId}", response)
                    : Results.BadRequest(response);
            }).RequireAuthorization(ServerPolicies.OperatorOrAbove);

            app.MapGet("/api/packages/deploys", async (int? limit, PackageDeployServer packageDeployServer) =>
            {
                return Results.Ok(await packageDeployServer.GetRecentDeploysAsync(limit ?? 20));
            }).RequireAuthorization(ServerPolicies.ViewerOrAbove);

            app.MapGet("/api/packages/install-events", async (
                string? packageName,
                string? agentKey,
                int? limit,
                PackageDeployServer packageDeployServer) =>
            {
                return Results.Ok(await packageDeployServer.GetInstallEventsAsync(
                    packageName,
                    agentKey,
                    limit ?? 50));
            }).RequireAuthorization(ServerPolicies.ViewerOrAbove);

            return app;
        }
    }
}
