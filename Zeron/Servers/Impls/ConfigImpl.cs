using NLog.Internal;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Zeron.Core;
using Zeron.Core.Base;
using Zeron.Interfaces;

namespace Zeron.Servers.Impls
{
    /// <summary>
    /// ConfigImpl
    /// </summary>
    internal class ConfigImpl : IImpl
    {
        /// <summary>
        /// Dispose
        /// </summary>
        /// <returns>Returns void.</returns>
        public void Dispose()
        {

        }

        /// <summary>
        /// PrepareObjects
        /// </summary>
        /// <returns>Returns void.</returns>
        public void PrepareObjects()
        {
            ConfigurationManager config = new ConfigurationManager();
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (Assembly assembly in assemblies)
            {
                if (!assembly.FullName.StartsWith("Zeron", StringComparison.InvariantCulture))
                    continue;

                foreach (Type assemblyType in assembly.GetTypes())
                {
                    if (assemblyType.GetCustomAttribute<ConfigAttribute>() == null || assemblyType.IsAbstract)
                        continue;

                    ConfigurationTable item = Activator.CreateInstance(assemblyType) as ConfigurationTable;

                    item.LoadConfig(config);
                }
            }
        }

        /// <summary>
        /// OnChanged
        /// </summary>
        /// <param name="source"></param>
        /// <param name="eventArgs"></param>
        /// <returns>Returns void.</returns>
        public void OnChanged(object source, FileSystemEventArgs eventArgs)
        {
            try
            {
                System.Configuration.ConfigurationManager.RefreshSection("appSettings");

                PrepareObjects();
            }
            catch (Exception e)
            {
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "ConfigImpl Error:{0}\n{1}", e.Message, e.StackTrace));
            }
        }
    }
}
