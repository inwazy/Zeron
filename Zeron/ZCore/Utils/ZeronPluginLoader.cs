// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using System.Globalization;
using System.Reflection;
using Zeron.ZInterfaces;

namespace Zeron.ZCore.Utils
{
    /// <summary>
    /// ZeronPluginLoader - loads IZeronAgentPlugin / IZeronServerPlugin from a directory.
    /// </summary>
    public static class ZeronPluginLoader
    {
        /// <summary>
        /// LoadAgentPlugins
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="bus"></param>
        /// <param name="gate"></param>
        /// <returns>Returns loaded plugins.</returns>
        public static List<IZeronAgentPlugin> LoadAgentPlugins(
            string? directory,
            IZeronEventBus bus,
            IGateController gate)
        {
            List<IZeronAgentPlugin> plugins = [];

            foreach (global::System.Type type in DiscoverTypes(directory, typeof(IZeronAgentPlugin)))
            {
                try
                {
                    if (Activator.CreateInstance(type) is not IZeronAgentPlugin plugin)
                    {
                        continue;
                    }

                    plugin.Initialize(bus, gate);
                    plugins.Add(plugin);
                    ZNLogger.Common.Info(string.Format(CultureInfo.InvariantCulture,
                        "ZeronPluginLoader loaded agent plugin '{0}' ({1}).", plugin.Id, type.FullName));
                }
                catch (Exception e)
                {
                    ZNLogger.Common.Warn(string.Format(CultureInfo.InvariantCulture,
                        "ZeronPluginLoader failed to load {0}: {1}", type.FullName, e.Message));
                }
            }

            return plugins;
        }

        /// <summary>
        /// LoadServerPlugins
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="bus"></param>
        /// <param name="gate"></param>
        /// <returns>Returns loaded plugins.</returns>
        public static List<IZeronServerPlugin> LoadServerPlugins(
            string? directory,
            IZeronEventBus bus,
            IGateController gate)
        {
            List<IZeronServerPlugin> plugins = [];

            foreach (global::System.Type type in DiscoverTypes(directory, typeof(IZeronServerPlugin)))
            {
                try
                {
                    if (Activator.CreateInstance(type) is not IZeronServerPlugin plugin)
                    {
                        continue;
                    }

                    plugin.Initialize(bus, gate);
                    plugins.Add(plugin);
                    ZNLogger.Common.Info(string.Format(CultureInfo.InvariantCulture,
                        "ZeronPluginLoader loaded server plugin '{0}' ({1}).", plugin.Id, type.FullName));
                }
                catch (Exception e)
                {
                    ZNLogger.Common.Warn(string.Format(CultureInfo.InvariantCulture,
                        "ZeronPluginLoader failed to load {0}: {1}", type.FullName, e.Message));
                }
            }

            return plugins;
        }

        /// <summary>
        /// DiscoverTypes
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="pluginInterface"></param>
        /// <returns>Returns list of types.</returns>
        private static List<global::System.Type> DiscoverTypes(
            string? directory,
            global::System.Type pluginInterface)
        {
            List<global::System.Type> types = [];

            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
            {
                return types;
            }

            foreach (string file in Directory.GetFiles(directory, "*.dll", SearchOption.TopDirectoryOnly))
            {
                string name = Path.GetFileName(file);

                if (name.StartsWith("Zeron.", StringComparison.OrdinalIgnoreCase)
                    || name.StartsWith("System.", StringComparison.OrdinalIgnoreCase)
                    || name.StartsWith("Microsoft.", StringComparison.OrdinalIgnoreCase)
                    || name.StartsWith("netstandard", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                try
                {
                    Assembly assembly = Assembly.LoadFrom(file);

                    foreach (global::System.Type type in assembly.GetExportedTypes())
                    {
                        if (type.IsAbstract || type.IsInterface || !pluginInterface.IsAssignableFrom(type))
                        {
                            continue;
                        }

                        types.Add(type);
                    }
                }
                catch (Exception e)
                {
                    ZNLogger.Common.Warn(string.Format(CultureInfo.InvariantCulture,
                        "ZeronPluginLoader skipped '{0}': {1}", name, e.Message));
                }
            }

            return types;
        }
    }
}
