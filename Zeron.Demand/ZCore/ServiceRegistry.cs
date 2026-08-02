// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using System.Collections.Concurrent;
using System.Reflection;
using Zeron.ZAttribute;
using Zeron.ZInterfaces;

namespace Zeron.Demand.ZCore
{
    /// <summary>
    /// ServiceRegistry - unified reflection-based registry for REP and SUB handlers.
    /// </summary>
    internal static class ServiceRegistry
    {
        // Dictionary of REP services.
        private static readonly ConcurrentDictionary<string, RepEntry> s_RepServices = new(StringComparer.OrdinalIgnoreCase);

        // Dictionary of SUB services.
        private static readonly ConcurrentDictionary<string, SubEntry> s_SubServices = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// RegisterFromAssembly
        /// </summary>
        /// <param name="assembly"></param>
        /// <returns>Returns void.</returns>
        public static void RegisterFromAssembly(
            Assembly assembly)
        {
            s_RepServices.Clear();
            s_SubServices.Clear();

            foreach (global::System.Type type in assembly.GetTypes())
            {
                ServicesRepAttribute? repAttribute = type.GetCustomAttribute<ServicesRepAttribute>();

                if (repAttribute != null && repAttribute.ZmqApiEnabled)
                {
                    string apiName = ResolveApiName(repAttribute.ZmqApiName, type.Name);
                    s_RepServices.TryAdd(apiName, new RepEntry(repAttribute, type));
                }

                ServicesSubAttribute? subAttribute = type.GetCustomAttribute<ServicesSubAttribute>();

                if (subAttribute != null && subAttribute.ZmqApiEnabled)
                {
                    string apiName = ResolveApiName(subAttribute.ZmqApiName, type.Name);
                    s_SubServices.TryAdd(apiName, new SubEntry(subAttribute, type));
                }
            }
        }

        /// <summary>
        /// TryGetRepEntry
        /// </summary>
        /// <param name="apiName"></param>
        /// <param name="entry"></param>
        /// <returns>Returns bool.</returns>
        public static bool TryGetRepEntry(
            string? apiName, 
            out RepEntry? entry)
        {
            if (apiName != null && s_RepServices.TryGetValue(apiName, out RepEntry? found))
            {
                entry = found;

                return true;
            }

            entry = null;

            return false;
        }

        /// <summary>
        /// TryGetSubEntry
        /// </summary>
        /// <param name="apiName"></param>
        /// <param name="entry"></param>
        /// <returns>Returns bool.</returns>
        public static bool TryGetSubEntry(
            string? apiName, 
            out SubEntry? entry)
        {
            if (apiName != null && s_SubServices.TryGetValue(apiName, out SubEntry? found))
            {
                entry = found;

                return true;
            }

            entry = null;

            return false;
        }

        /// <summary>
        /// InvokeRep
        /// </summary>
        /// <param name="apiName"></param>
        /// <param name="request"></param>
        /// <param name="asyncTask"></param>
        /// <returns>Returns response JSON.</returns>
        public static string InvokeRep(
            string? apiName, 
            dynamic request, 
            bool asyncTask = false)
        {
            if (!TryGetRepEntry(apiName, out RepEntry? entry) || entry == null)
            {
                return ServiceResponse.SerializeFailure($"Unknown API: {apiName}");
            }

            IServices? service = Activator.CreateInstance(entry.ServiceType) as IServices;

            if (service == null)
            {
                return ServiceResponse.SerializeFailure($"Unable to create service: {apiName}");
            }

            return asyncTask ? service.OnRequestAsync(request) : service.OnRequest(request);
        }

        /// <summary>
        /// InvokeSub
        /// </summary>
        /// <param name="apiName"></param>
        /// <param name="request"></param>
        /// <param name="asyncTask"></param>
        /// <returns>Returns response JSON.</returns>
        public static string InvokeSub(
            string? apiName, 
            dynamic request, 
            bool asyncTask = false)
        {
            if (!TryGetSubEntry(apiName, out SubEntry? entry) || entry == null)
            {
                return ServiceResponse.SerializeFailure($"Unknown SUB API: {apiName}");
            }

            IServices? service = Activator.CreateInstance(entry.ServiceType) as IServices;

            if (service == null)
            {
                return ServiceResponse.SerializeFailure($"Unable to create SUB service: {apiName}");
            }

            return asyncTask ? service.OnSubscriberAsync(request) : service.OnSubscriber(request);
        }

        /// <summary>
        /// ResolveApiName
        /// </summary>
        /// <param name="configuredName"></param>
        /// <param name="fallbackName"></param>
        /// <returns>Returns api name.</returns>
        private static string ResolveApiName(
            string? configuredName, 
            string fallbackName)
        {
            return string.IsNullOrWhiteSpace(configuredName) ? fallbackName : configuredName;
        }

        /// <summary>
        /// RepEntry
        /// </summary>
        /// <param name="Attribute"></param>
        /// <param name="Type"></param>
        internal sealed record RepEntry(
            ServicesRepAttribute Attribute, 
            global::System.Type ServiceType);

        /// <summary>
        /// SubEntry
        /// </summary>
        internal sealed record SubEntry(
            ServicesSubAttribute Attribute, 
            global::System.Type ServiceType);
    }
}
