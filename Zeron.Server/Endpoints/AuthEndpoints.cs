// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
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

                if (!response.Success || response.User == null)
                {
                    return Results.Redirect("/login?failed=1");
                }

                UserEntity userEntity = new()
                {
                    Id = Guid.Parse(response.User.Id!),
                    Username = response.User.Username!,
                    Role = response.User.Role!
                };

                ClaimsPrincipal principal = JwtTokenServer.CreateClaimsPrincipal(userEntity);

                await context.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    principal,
                    new AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
                    });

                return Results.Redirect("/");
            }).AllowAnonymous().DisableAntiforgery();

            app.MapPost("/api/auth/login", async (
                LoginRequestType request,
                AuthServer authServer,
                JwtTokenServer jwtTokenServer,
                HttpContext context) =>
            {
                LoginResponseType response = await authServer.LoginAsync(request.Username, request.Password);

                if (!response.Success || response.User == null)
                {
                    return Results.Json(response, statusCode: StatusCodes.Status401Unauthorized);
                }

                UserEntity userEntity = new()
                {
                    Id = Guid.Parse(response.User.Id!),
                    Username = response.User.Username!,
                    Role = response.User.Role!
                };

                ClaimsPrincipal principal = JwtTokenServer.CreateClaimsPrincipal(userEntity);

                await context.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    principal,
                    new AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
                    });

                return Results.Ok(response);
            }).AllowAnonymous();

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
            }).RequireAuthorization(ServerPolicies.ViewerOrAbove);

            return app;
        }
    }
}
