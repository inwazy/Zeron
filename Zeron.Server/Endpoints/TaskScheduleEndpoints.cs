// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Zeron.Server.ZCore;
using Zeron.Server.ZServers;
using Zeron.ZCore.Type;

namespace Zeron.Server.Endpoints
{
    /// <summary>
    /// TaskScheduleEndpoints
    /// </summary>
    public static class TaskScheduleEndpoints
    {
        /// <summary>
        /// MapTaskScheduleEndpoints
        /// </summary>
        /// <param name="app"></param>
        /// <returns>Returns WebApplication.</returns>
        public static WebApplication MapTaskScheduleEndpoints(
            this WebApplication app)
        {
            app.MapGet("/api/schedules", async (TaskScheduleServer scheduleServer) =>
            {
                return Results.Ok(await scheduleServer.GetSchedulesAsync());
            }).RequireAuthorization(ServerPolicies.ViewerOrAbove);

            app.MapGet("/api/schedules/{scheduleId:guid}", async (
                Guid scheduleId,
                TaskScheduleServer scheduleServer) =>
            {
                TaskScheduleInfoType? schedule = await scheduleServer.GetScheduleAsync(scheduleId);

                return schedule == null ? Results.NotFound() : Results.Ok(schedule);
            }).RequireAuthorization(ServerPolicies.ViewerOrAbove);

            app.MapPost("/api/schedules", async (
                TaskScheduleCreateRequestType request,
                TaskScheduleServer scheduleServer) =>
            {
                (TaskScheduleInfoType? schedule, string? error) = await scheduleServer.CreateScheduleAsync(request);

                if (error != null)
                {
                    return Results.BadRequest(new { success = false, message = error });
                }

                return Results.Created($"/api/schedules/{schedule!.Id}", schedule);
            }).RequireAuthorization(ServerPolicies.OperatorOrAbove);

            app.MapPut("/api/schedules/{scheduleId:guid}", async (
                Guid scheduleId,
                TaskScheduleUpdateRequestType request,
                TaskScheduleServer scheduleServer) =>
            {
                (TaskScheduleInfoType? schedule, string? error) = await scheduleServer.UpdateScheduleAsync(
                    scheduleId,
                    request);

                if (error == "Schedule not found.")
                {
                    return Results.NotFound();
                }

                if (error != null)
                {
                    return Results.BadRequest(new { success = false, message = error });
                }

                return Results.Ok(schedule);
            }).RequireAuthorization(ServerPolicies.OperatorOrAbove);

            app.MapPost("/api/schedules/{scheduleId:guid}/enable", async (
                Guid scheduleId,
                TaskScheduleServer scheduleServer) =>
            {
                (TaskScheduleInfoType? schedule, string? error) = await scheduleServer.SetEnabledAsync(
                    scheduleId,
                    true);

                return error == "Schedule not found."
                    ? Results.NotFound()
                    : error != null
                        ? Results.BadRequest(new { success = false, message = error })
                        : Results.Ok(schedule);
            }).RequireAuthorization(ServerPolicies.OperatorOrAbove);

            app.MapPost("/api/schedules/{scheduleId:guid}/disable", async (
                Guid scheduleId,
                TaskScheduleServer scheduleServer) =>
            {
                (TaskScheduleInfoType? schedule, string? error) = await scheduleServer.SetEnabledAsync(
                    scheduleId,
                    false);

                return error == "Schedule not found."
                    ? Results.NotFound()
                    : error != null
                        ? Results.BadRequest(new { success = false, message = error })
                        : Results.Ok(schedule);
            }).RequireAuthorization(ServerPolicies.OperatorOrAbove);

            app.MapPost("/api/schedules/{scheduleId:guid}/run", async (
                Guid scheduleId,
                TaskScheduleServer scheduleServer) =>
            {
                (Guid? taskId, string? error) = await scheduleServer.TriggerNowAsync(scheduleId);

                if (error == "Schedule not found.")
                {
                    return Results.NotFound();
                }

                if (error != null || taskId == null)
                {
                    return Results.BadRequest(new { success = false, message = error });
                }

                return Results.Ok(new { success = true, taskId });
            }).RequireAuthorization(ServerPolicies.OperatorOrAbove);

            app.MapDelete("/api/schedules/{scheduleId:guid}", async (
                Guid scheduleId,
                TaskScheduleServer scheduleServer) =>
            {
                bool deleted = await scheduleServer.DeleteScheduleAsync(scheduleId);

                return deleted ? Results.Ok(new { success = true }) : Results.NotFound();
            }).RequireAuthorization(ServerPolicies.AdminOnly);

            return app;
        }
    }
}
