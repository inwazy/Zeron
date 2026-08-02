// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Microsoft.EntityFrameworkCore;
using Zeron.Server.Data;
using Zeron.ZCore.Type;

namespace Zeron.Server.Endpoints
{
    /// <summary>
    /// HealthEndpoints
    /// </summary>
    public static class HealthEndpoints
    {
        /// <summary>
        /// MapHealthEndpoints
        /// </summary>
        /// <param name="app"></param>
        /// <returns>Returns WebApplication.</returns>
        public static WebApplication MapHealthEndpoints(
            this WebApplication app)
        {
            app.MapGet("/health", () =>
            {
                HealthStatusType status = new()
                {
                    Status = "healthy",
                    Service = "Zeron.Server",
                    Version = typeof(ServerHost).Assembly.GetName().Version?.ToString(),
                    TimestampUtc = DateTime.UtcNow
                };

                return Results.Ok(status);
            }).AllowAnonymous();

            app.MapGet("/ready", async (ZeronServerDbContext dbContext) =>
            {
                Dictionary<string, string> checks = new();
                bool ready = true;

                try
                {
                    bool canConnect = await dbContext.Database.CanConnectAsync();
                    checks["database"] = canConnect ? "healthy" : "unhealthy";
                    ready = canConnect;
                }
                catch (Exception ex)
                {
                    checks["database"] = "unhealthy: " + ex.Message;
                    ready = false;
                }

                HealthStatusType status = new()
                {
                    Status = ready ? "healthy" : "unhealthy",
                    Service = "Zeron.Server",
                    Version = typeof(ServerHost).Assembly.GetName().Version?.ToString(),
                    TimestampUtc = DateTime.UtcNow,
                    Checks = checks
                };

                return ready
                    ? Results.Ok(status)
                    : Results.Json(status, statusCode: StatusCodes.Status503ServiceUnavailable);
            }).AllowAnonymous();

            return app;
        }
    }
}
