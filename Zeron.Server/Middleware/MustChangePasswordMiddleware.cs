// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Zeron.Server.ZCore;

namespace Zeron.Server.Middleware
{
    /// <summary>
    /// MustChangePasswordMiddleware
    /// </summary>
    public class MustChangePasswordMiddleware
    {
        // Next request delegate
        private readonly RequestDelegate m_Next;

        /// <summary>
        /// MustChangePasswordMiddleware
        /// </summary>
        /// <param name="next"></param>
        /// <returns>Returns void.</returns>
        public MustChangePasswordMiddleware(
            RequestDelegate next)
        {
            m_Next = next;
        }

        /// <summary>
        /// InvokeAsync
        /// </summary>
        /// <param name="context"></param>
        /// <returns>Returns void.</returns>
        public async Task InvokeAsync(
            HttpContext context)
        {
            if (context.User.Identity?.IsAuthenticated == true
                && context.User.HasClaim(ServerClaimTypes.MustChangePassword, "true")
                && !IsAllowedPath(context.Request.Path))
            {
                if (IsApiRequest(context.Request.Path))
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        success = false,
                        message = "Password change required.",
                        mustChangePassword = true
                    });
                    return;
                }

                context.Response.Redirect("/account/change-password");
                return;
            }

            await m_Next(context);
        }

        /// <summary>
        /// IsAllowedPath
        /// </summary>
        /// <param name="path"></param>
        /// <returns>Returns bool.</returns>
        private static bool IsAllowedPath(
            PathString path)
        {
            string value = path.Value ?? "";

            return value.StartsWith("/account/change-password", StringComparison.OrdinalIgnoreCase)
                || value.StartsWith("/account/logout", StringComparison.OrdinalIgnoreCase)
                || value.StartsWith("/api/auth/change-password", StringComparison.OrdinalIgnoreCase)
                || value.StartsWith("/api/auth/logout", StringComparison.OrdinalIgnoreCase)
                || value.StartsWith("/api/auth/me", StringComparison.OrdinalIgnoreCase)
                || value.StartsWith("/health", StringComparison.OrdinalIgnoreCase)
                || value.StartsWith("/ready", StringComparison.OrdinalIgnoreCase)
                || value.StartsWith("/login", StringComparison.OrdinalIgnoreCase)
                || value.StartsWith("/_blazor", StringComparison.OrdinalIgnoreCase)
                || value.StartsWith("/_framework", StringComparison.OrdinalIgnoreCase)
                || value.StartsWith("/css/", StringComparison.OrdinalIgnoreCase)
                || value.StartsWith("/js/", StringComparison.OrdinalIgnoreCase)
                || value.Equals("/favicon.ico", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// IsApiRequest
        /// </summary>
        /// <param name="path"></param>
        /// <returns>Returns bool.</returns>
        private static bool IsApiRequest(
            PathString path)
        {
            return (path.Value ?? "").StartsWith("/api/", StringComparison.OrdinalIgnoreCase);
        }
    }
}
