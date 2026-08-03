// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Zeron.Server.ZCore;
using Zeron.Server.ZServers;
using Zeron.ZCore.Utils;

namespace Zeron.Server.Endpoints
{
    /// <summary>
    /// AgentApiKeyExtensions
    /// </summary>
    public static class AgentApiKeyExtensions
    {
        /// <summary>
        /// ValidateAgentApiKey (legacy sync key check).
        /// </summary>
        /// <param name="context"></param>
        /// <param name="authServer"></param>
        /// <returns>Returns IResult or null when valid.</returns>
        public static IResult? ValidateAgentApiKey(
            this HttpContext context, 
            AuthServer authServer)
        {
            string? apiKey = context.Request.Headers[AgentHmacServer.AgentKeyHeader].FirstOrDefault()
                ?? context.Request.Query["apiKey"].FirstOrDefault();

            if (authServer.ValidateAgentApiKey(apiKey))
            {
                return null;
            }

            return Results.Unauthorized();
        }

        /// <summary>
        /// ValidateAgentRequestAsync — API key, optional HTTPS, optional HMAC.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="authServer"></param>
        /// <param name="settings"></param>
        /// <returns>Returns IResult or null when valid.</returns>
        public static async Task<IResult?> ValidateAgentRequestAsync(
            this HttpContext context,
            AuthServer authServer,
            ServerSettings settings)
        {
            if (settings.RequireHttpsAgents && !context.Request.IsHttps)
            {
                string? forwardedProto = context.Request.Headers["X-Forwarded-Proto"].FirstOrDefault();

                if (!string.Equals(forwardedProto, "https", StringComparison.OrdinalIgnoreCase))
                {
                    return Results.BadRequest(new { error = "HTTPS required for agent API." });
                }
            }

            string? apiKey = context.Request.Headers[AgentHmacServer.AgentKeyHeader].FirstOrDefault()
                ?? context.Request.Query["apiKey"].FirstOrDefault();

            if (!authServer.ValidateAgentApiKey(apiKey))
            {
                return Results.Unauthorized();
            }

            if (!settings.AgentHmacRequired)
            {
                return null;
            }

            context.Request.Body.Position = 0;
            using MemoryStream memoryStream = new();
            await context.Request.Body.CopyToAsync(memoryStream);
            byte[] body = memoryStream.ToArray();
            context.Request.Body.Position = 0;

            string path = context.Request.Path.HasValue ? context.Request.Path.Value! : "/";
            string? timestamp = context.Request.Headers[AgentHmacServer.TimestampHeader].FirstOrDefault();
            string? signature = context.Request.Headers[AgentHmacServer.SignatureHeader].FirstOrDefault();

            if (!AgentHmacServer.TryValidateAny(
                AgentApiKeyServer.SplitKeys(settings.AgentApiKey),
                context.Request.Method,
                path,
                timestamp,
                signature,
                body,
                settings.AgentHmacSkewSeconds,
                out _))
            {
                return Results.Unauthorized();
            }

            return null;
        }
    }
}
