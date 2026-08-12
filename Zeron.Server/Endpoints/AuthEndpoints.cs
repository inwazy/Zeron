// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Zeron.Server.Data.Entities;
using Zeron.Server.ZCore;
using Zeron.Server.ZServers;
using Zeron.ZCore.Type;

namespace Zeron.Server.Endpoints
{
    /// <summary>
    /// AuthEndpoints
    /// </summary>
    public static class AuthEndpoints
    {
        /// <summary>
        /// MapAuthEndpoints
        /// </summary>
        /// <param name="app"></param>
        /// <returns>Returns WebApplication.</returns>
        public static WebApplication MapAuthEndpoints(
            this WebApplication app)
        {
            app.MapPost("/account/login", async (
                HttpContext context,
                [FromForm] string username,
                [FromForm] string password,
                AuthServer authServer) =>
            {
                LoginResponseType response = await authServer.LoginAsync(username, password);

                if (!response.Success || response.User == null || !Guid.TryParse(response.User.Id, out Guid userId))
                {
                    return Results.Redirect("/login?failed=1");
                }

                UserEntity? userEntity = await authServer.GetUserEntityAsync(userId);

                if (userEntity == null)
                {
                    return Results.Redirect("/login?failed=1");
                }

                await SignInUserAsync(context, userEntity);

                return response.User.MustChangePassword
                    ? Results.Redirect("/account/change-password?required=1")
                    : Results.Redirect(GetHomePath(userEntity.Role));
            }).AllowAnonymous().DisableAntiforgery();

            app.MapPost("/api/auth/login", async (
                LoginRequestType request,
                AuthServer authServer,
                HttpContext context) =>
            {
                LoginResponseType response = await authServer.LoginAsync(request.Username, request.Password);

                if (!response.Success || response.User == null || !Guid.TryParse(response.User.Id, out Guid userId))
                {
                    return Results.Json(response, statusCode: StatusCodes.Status401Unauthorized);
                }

                UserEntity? userEntity = await authServer.GetUserEntityAsync(userId);

                if (userEntity == null)
                {
                    return Results.Json(response, statusCode: StatusCodes.Status401Unauthorized);
                }

                await SignInUserAsync(context, userEntity);

                return Results.Ok(response);
            }).AllowAnonymous();

            app.MapPost("/account/change-password", async (
                HttpContext context,
                [FromForm] string currentPassword,
                [FromForm] string newPassword,
                [FromForm] string confirmPassword,
                AuthServer authServer) =>
            {
                if (!TryGetUserId(context.User, out Guid userId))
                {
                    return Results.Redirect("/login");
                }

                if (!string.Equals(newPassword, confirmPassword, StringComparison.Ordinal))
                {
                    return Results.Redirect("/account/change-password?error=mismatch");
                }

                (UserInfoType? user, string? error) = await authServer.ChangePasswordAsync(
                    userId,
                    currentPassword,
                    newPassword);

                if (error != null)
                {
                    string code = error switch
                    {
                        "Current password is incorrect." => "current",
                        "New password must be different from the current password." => "same",
                        _ => "invalid"
                    };

                    return Results.Redirect("/account/change-password?error=" + code);
                }

                UserEntity? userEntity = await authServer.GetUserEntityAsync(userId);

                if (userEntity != null)
                {
                    await SignInUserAsync(context, userEntity);
                }

                string home = GetHomePath(userEntity?.Role);

                return Results.Redirect(home + (home.Contains('?', StringComparison.Ordinal) ? "&" : "?") + "passwordChanged=1");
            }).RequireAuthorization(ServerPolicies.DeviceOwnerOrStaff).DisableAntiforgery();

            app.MapPost("/api/auth/change-password", async (
                ChangePasswordRequestType request,
                ClaimsPrincipal principal,
                AuthServer authServer,
                HttpContext context) =>
            {
                if (!TryGetUserId(principal, out Guid userId))
                {
                    return Results.Unauthorized();
                }

                (UserInfoType? user, string? error) = await authServer.ChangePasswordAsync(
                    userId,
                    request.CurrentPassword,
                    request.NewPassword);

                if (error != null)
                {
                    return Results.BadRequest(new { success = false, message = error });
                }

                UserEntity? userEntity = await authServer.GetUserEntityAsync(userId);

                if (userEntity != null)
                {
                    await SignInUserAsync(context, userEntity);
                }

                return Results.Ok(new { success = true, user });
            }).RequireAuthorization(ServerPolicies.DeviceOwnerOrStaff);

            app.MapPost("/api/auth/email", async (
                UpdateEmailRequestType request,
                ClaimsPrincipal principal,
                AuthServer authServer) =>
            {
                if (!TryGetUserId(principal, out Guid userId))
                {
                    return Results.Unauthorized();
                }

                (UserInfoType? user, string? error) = await authServer.UpdateEmailAsync(
                    userId,
                    request.Email);

                if (error != null)
                {
                    return Results.BadRequest(new { success = false, message = error });
                }

                return Results.Ok(new { success = true, user });
            }).RequireAuthorization(ServerPolicies.DeviceOwnerOrStaff);

            app.MapPost("/account/logout", async (HttpContext context) =>
            {
                await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                return Results.Redirect("/login");
            }).AllowAnonymous().DisableAntiforgery();

            app.MapPost("/api/auth/logout", async (HttpContext context) =>
            {
                await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                return Results.Ok(new { success = true });
            }).RequireAuthorization();

            app.MapGet("/api/auth/me", async (ClaimsPrincipal user, AuthServer authServer) =>
            {
                UserInfoType? profile = await authServer.GetUserFromPrincipalAsync(user);

                return profile == null ? Results.Unauthorized() : Results.Ok(profile);
            }).RequireAuthorization(ServerPolicies.DeviceOwnerOrStaff);

            return app;
        }

        /// <summary>
        /// SignInUserAsync
        /// </summary>
        /// <param name="context"></param>
        /// <param name="userEntity"></param>
        /// <returns>Returns void.</returns>
        private static async Task SignInUserAsync(
            HttpContext context, 
            UserEntity userEntity)
        {
            ClaimsPrincipal principal = JwtTokenServer.CreateClaimsPrincipal(userEntity);

            await context.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
                });
        }

        /// <summary>
        /// GetHomePath
        /// </summary>
        /// <param name="role"></param>
        /// <returns>Returns home path for role.</returns>
        private static string GetHomePath(
            string? role)
        {
            return string.Equals(role, ServerRoles.DeviceOwner, StringComparison.OrdinalIgnoreCase)
                ? "/my-devices"
                : "/";
        }

        /// <summary>
        /// TryGetUserId
        /// </summary>
        /// <param name="principal"></param>
        /// <param name="userId"></param>
        /// <returns>Returns bool.</returns>
        private static bool TryGetUserId(
            ClaimsPrincipal principal, 
            out Guid userId)
        {
            string? value = principal.FindFirstValue(ClaimTypes.NameIdentifier);

            return Guid.TryParse(value, out userId);
        }
    }
}
