// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

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
                var task = await taskDispatcher.CreateTaskAsync(request);

                return Results.Created($"/api/tasks/{task.Id}", task);
            }).RequireAuthorization(ServerPolicies.OperatorOrAbove);

            app.MapGet("/api/tasks", async (TaskDispatcherServer taskDispatcher) =>
            {
                return Results.Ok(await taskDispatcher.GetTasksAsync());
            }).RequireAuthorization(ServerPolicies.ViewerOrAbove);

            app.MapGet("/api/tasks/{taskId:guid}", async (Guid taskId, TaskDispatcherServer taskDispatcher) =>
            {
                var task = await taskDispatcher.GetTaskAsync(taskId);

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
                AuthServer authServer) =>
            {
                IResult? authResult = context.ValidateAgentApiKey(authServer);

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
