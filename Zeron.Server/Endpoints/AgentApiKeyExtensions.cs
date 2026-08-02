// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Zeron.Server.ZServers;

namespace Zeron.Server.Endpoints
{
    /// <summary>
    /// AgentApiKeyExtensions
    /// </summary>
    public static class AgentApiKeyExtensions
    {
        /// <summary>
        /// ValidateAgentApiKey
        /// </summary>
        /// <param name="context"></param>
        /// <param name="authServer"></param>
        /// <returns>Returns IResult or null when valid.</returns>
        public static IResult? ValidateAgentApiKey(
            this HttpContext context, 
            AuthServer authServer)
        {
            string? apiKey = context.Request.Headers["X-Zeron-Agent-Key"].FirstOrDefault()
                ?? context.Request.Query["apiKey"].FirstOrDefault();

            if (authServer.ValidateAgentApiKey(apiKey))
            {
                return null;
            }

            return Results.Unauthorized();
        }
    }
}
