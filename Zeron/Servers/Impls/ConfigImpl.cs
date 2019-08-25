using NLog.Internal;
using System;
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
                if (!assembly.FullName.StartsWith("Zeron"))
                    continue;

                foreach (Type assemblyType in assembly.GetTypes())
                {
                    if (assemblyType.GetCustomAttribute<ConfigAttribute>() == null)
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
        /// <param name="e"></param>
        /// <returns>Returns void.</returns>
        public void OnChanged(object source, FileSystemEventArgs e)
        {
            System.Configuration.ConfigurationManager.RefreshSection("appSettings");

            PrepareObjects();
        }
    }
}
