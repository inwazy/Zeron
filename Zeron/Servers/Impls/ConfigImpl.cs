// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using NLog.Internal;
using System;
using System.Globalization;
using System.IO;
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
            try
            {
                ConfigurationManager config = new ConfigurationManager();
                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

                foreach (Assembly assembly in assemblies)
                {
                    if (!assembly.FullName.StartsWith("Zeron", StringComparison.InvariantCulture))
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
                            ConfigurationTable item = Activator.CreateInstance(assemblyType) as ConfigurationTable;

                            item.LoadConfig(config);
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
                System.Configuration.ConfigurationManager.RefreshSection("appSettings");

                PrepareObjects();
            }
            catch (Exception e)
            {
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "ConfigImpl OnChanged Error:{0}\n{1}", e.Message, e.StackTrace));
            }
        }
    }
}
