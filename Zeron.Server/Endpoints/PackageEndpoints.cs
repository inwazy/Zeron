// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

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
            app.MapPost("/api/packages/deploy", async (
                PackageDeployRequestType request,
                PackageDeployServer packageDeployServer) =>
            {
                PackageDeployResponseType response = await packageDeployServer.DeployAsync(request);

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
