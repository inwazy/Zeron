// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Newtonsoft.Json;
using System.Dynamic;
using System.Globalization;
using Zeron.ZServers;

namespace Zeron.Demand.ZCore
{
    /// <summary>
    /// ServiceResponse
    /// </summary>
    internal static class ServiceResponse
    {
        /// <summary>
        /// SerializeSuccess
        /// </summary>
        /// <param name="result"></param>
        /// <returns>Returns JSON string.</returns>
        public static string SerializeSuccess(object? result = null)
        {
            dynamic response = new ExpandoObject();
            response.success = true;
            response.result = result;
            response.agentId = AgentServer.AgentId;
            response.timestamp = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);

            return JsonConvert.SerializeObject(response);
        }

        /// <summary>
        /// SerializeFailure
        /// </summary>
        /// <param name="message"></param>
        /// <param name="result"></param>
        /// <returns>Returns JSON string.</returns>
        public static string SerializeFailure(string? message, object? result = null)
        {
            dynamic response = new ExpandoObject();
            response.success = false;
            response.message = message;
            response.result = result;
            response.agentId = AgentServer.AgentId;
            response.timestamp = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);

            return JsonConvert.SerializeObject(response);
        }
    }
}
