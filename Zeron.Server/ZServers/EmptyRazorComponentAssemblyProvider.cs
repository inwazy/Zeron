// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using System.Reflection;
using Zeron.Server.ZInterfaces;

namespace Zeron.Server.ZServers
{
    /// <summary>
    /// EmptyRazorComponentAssemblyProvider
    /// </summary>
    public sealed class EmptyRazorComponentAssemblyProvider : IRazorComponentAssemblyProvider
    {
        /// <summary>
        /// GetAdditionalAssemblies
        /// </summary>
        /// <returns>Returns an empty list.</returns>
        public IReadOnlyList<Assembly> GetAdditionalAssemblies() => [];
    }
}
