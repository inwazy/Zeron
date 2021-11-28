// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using System.Collections.Specialized;
using System.Configuration;
using System.Globalization;
using System.Reflection;
using Zeron.ZAttribute;
using Zeron.ZCore;
using Zeron.ZCore.Foundation;
using Zeron.ZInterfaces;

namespace Zeron.ZServers.Impls
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
            try
            {
                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                NameValueCollection config = ConfigurationManager.AppSettings;

                foreach (Assembly assembly in assemblies)
                {
                    if (assembly == null)
                    {
                        continue;
                    }

                    string? assemblyName = assembly.FullName;

                    if (assemblyName == null || assemblyName == "")
                    {
                        continue;
                    }

                    if (!assemblyName.StartsWith("Zeron", StringComparison.InvariantCulture))
                    {
                        continue;
                    }

                    foreach (Type assemblyType in assembly.GetTypes())
                    {
                        if (assemblyType.GetCustomAttribute<ConfigAttribute>() == null || assemblyType.IsAbstract)
                        {
                            continue;
                        }

                        try
                        {
                            if (Activator.CreateInstance(assemblyType) is ConfigurationTable item)
                            {
                                item.LoadConfig(config);
                            }
                        }
                        catch (Exception e)
                        {
                            ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "PrepareObjects CreateInstance Error:{0}\n{1}", e.Message, e.StackTrace));
                        }
                    }
                }
            }
            catch (AppDomainUnloadedException e)
            {
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "ConfigImpl PrepareObjects Error:{0}\n{1}", e.Message, e.StackTrace));
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
                ConfigurationManager.RefreshSection("appSettings");

                PrepareObjects();
            }
            catch (Exception e)
            {
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "ConfigImpl OnChanged Error:{0}\n{1}", e.Message, e.StackTrace));
            }
        }
    }
}
