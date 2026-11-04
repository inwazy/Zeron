// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using System.Reflection;

namespace Zeron.Server.ZInterfaces
{
    /// <summary>
    /// IRazorComponentAssemblyProvider — optional extra Razor page assemblies (editions).
    /// </summary>
    public interface IRazorComponentAssemblyProvider
    {
        /// <summary>
        /// GetAdditionalAssemblies
        /// </summary>
        /// <returns>Returns assemblies that contain @page components beyond the host App assembly.</returns>
        IReadOnlyList<Assembly> GetAdditionalAssemblies();
    }
}
