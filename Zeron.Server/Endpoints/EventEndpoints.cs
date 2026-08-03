// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Zeron.Server.ZCore;
using Zeron.Server.ZServers;
using Zeron.ZCore.Type;

namespace Zeron.Server.Endpoints
{
    /// <summary>
    /// EventEndpoints
    /// </summary>
    public static class EventEndpoints
    {
        /// <summary>
        /// MapEventEndpoints
        /// </summary>
        /// <param name="app"></param>
        /// <returns>Returns WebApplication.</returns>
        public static WebApplication MapEventEndpoints(
            this WebApplication app)
        {
            app.MapPost("/api/events", async (
                AgentEventReportType report,
                HttpContext context,
                EventIngestorServer eventIngestor,
                AuthServer authServer,
                ServerSettings settings) =>
            {
                IResult? authResult = await context.ValidateAgentRequestAsync(authServer, settings);

                if (authResult != null)
                {
                    return authResult;
                }

                bool saved = await eventIngestor.IngestEventAsync(report);

                return saved ? Results.Ok(new { success = true }) : Results.BadRequest(new { success = false });
            });

            app.MapGet("/api/events", async (string? agentKey, string? topic, int? limit, EventIngestorServer eventIngestor) =>
            {
                int take = limit.GetValueOrDefault(100);

                return Results.Ok(await eventIngestor.GetEventsAsync(agentKey, topic, take));
            }).RequireAuthorization(ServerPolicies.ViewerOrAbove);

            return app;
        }
    }
}
