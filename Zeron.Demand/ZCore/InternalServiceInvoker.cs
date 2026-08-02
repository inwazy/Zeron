// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using System.Dynamic;
using Zeron.ZCore;

namespace Zeron.Demand.ZCore
{
    /// <summary>
    /// InternalServiceInvoker - invokes registered REP services without NetMQ.
    /// </summary>
    internal static class InternalServiceInvoker
    {
        /// <summary>
        /// Invoke
        /// </summary>
        /// <param name="apiName"></param>
        /// <param name="command"></param>
        /// <returns>Returns JSON response.</returns>
        public static string Invoke(
            string? apiName, 
            string? command)
        {
            dynamic request = new ExpandoObject();
            request.Command = command ?? "";

            return ServiceRegistry.InvokeRep(apiName, request);
        }
    }
}
