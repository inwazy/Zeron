// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Zeron.Server.Data.Entities;
using Zeron.Server.ZCore;
using Zeron.Server.ZServers;
using Zeron.ZCore.Type;

namespace Zeron.Server.Endpoints
{
    /// <summary>
    /// TaskEndpoints
    /// </summary>
    public static class TaskEndpoints
    {
        /// <summary>
        /// MapTaskEndpoints
        /// </summary>
        /// <param name="app"></param>
        /// <returns>Returns WebApplication.</returns>
        public static WebApplication MapTaskEndpoints(
            this WebApplication app)
        {
            app.MapPost("/api/tasks", async (TaskCreateRequestType request, TaskDispatcherServer taskDispatcher) =>
            {
                try
                {
                    TaskEntity task = await taskDispatcher.CreateTaskAsync(request);

                    return Results.Created($"/api/tasks/{task.Id}", task);
                }
                catch (UnauthorizedAccessException ex)
                {
                    return Results.Json(new { success = false, message = ex.Message }, statusCode: StatusCodes.Status403Forbidden);
                }
            }).RequireAuthorization(ServerPolicies.OperatorOrAbove);

            app.MapGet("/api/tasks", async (TaskDispatcherServer taskDispatcher) =>
            {
                return Results.Ok(await taskDispatcher.GetTasksAsync());
            }).RequireAuthorization(ServerPolicies.ViewerOrAbove);

            app.MapGet("/api/tasks/{taskId:guid}", async (Guid taskId, TaskDispatcherServer taskDispatcher) =>
            {
                TaskEntity? task = await taskDispatcher.GetTaskAsync(taskId);

                return task == null ? Results.NotFound() : Results.Ok(task);
            }).RequireAuthorization(ServerPolicies.ViewerOrAbove);

            app.MapPost("/api/tasks/{taskId:guid}/cancel", async (Guid taskId, TaskDispatcherServer taskDispatcher) =>
            {
                bool cancelled = await taskDispatcher.CancelTaskAsync(taskId);

                return cancelled ? Results.Ok(new { success = true }) : Results.BadRequest(new { success = false });
            }).RequireAuthorization(ServerPolicies.AdminOnly);

            app.MapPost("/api/tasks/results", async (
                TaskResultReportType report,
                HttpContext context,
                TaskDispatcherServer taskDispatcher,
                AuthServer authServer,
                ServerSettings settings) =>
            {
                IResult? authResult = await context.ValidateAgentRequestAsync(authServer, settings);

                if (authResult != null)
                {
                    return authResult;
                }

                bool saved = await taskDispatcher.ReportResultAsync(report);

                return saved ? Results.Ok(new { success = true }) : Results.BadRequest(new { success = false });
            });

            return app;
        }
    }
}
