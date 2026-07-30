// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Newtonsoft.Json;
using System.Dynamic;
using Zeron.Demand.ZServices;
using Zeron.ZCore.Type;
using Zeron.ZInterfaces;

namespace Zeron.Demand.ZCore
{
    /// <summary>
    /// InternalServiceInvoker - resolves IServices handlers without NetMQ.
    /// </summary>
    internal static class InternalServiceInvoker
    {
        // Factories for IServices.
        private static readonly Dictionary<string, Func<IServices>> s_Factories = new(StringComparer.OrdinalIgnoreCase)
        {
            ["ServerInfo"] = () => new ServerInfo(),
            ["ProcessInfo"] = () => new ProcessInfo(),
            ["ManagedPackage"] = () => new ManagedPackage(),
            ["FileSystem"] = () => new FileSystem(),
            ["ServiceControl"] = () => new ServiceControl(),
            ["Registry"] = () => new RegistryService(),
            ["PowerShell"] = () => new PowerShellService(),
            ["TaskPipeline"] = () => new TaskPipeline(),
            ["Scheduler"] = () => new Scheduler()
        };

        /// <summary>
        /// Invoke
        /// </summary>
        /// <param name="apiName"></param>
        /// <param name="command"></param>
        /// <returns>Returns JSON response.</returns>
        public static string Invoke(string? apiName, string? command)
        {
            if (apiName == null || !s_Factories.TryGetValue(apiName, out Func<IServices>? factory))
            {
                return ServiceResponse.SerializeFailure($"Unknown API: {apiName}");
            }

            dynamic request = new ExpandoObject();
            request.Command = command ?? "";

            return factory().OnRequest(request);
        }
    }
}
