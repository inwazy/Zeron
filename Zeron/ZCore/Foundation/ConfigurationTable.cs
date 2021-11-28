// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using System.Collections.Specialized;
using Zeron.ZAttribute;

namespace Zeron.ZCore.Foundation
{
    [Config]

    /// <summary>
    /// ConfigurationTable
    /// </summary>
    public abstract class ConfigurationTable
    {
        /// <summary>
        /// LoadConfig
        /// </summary>
        /// <param name="aConfig"></param>
        /// <returns>Returns void.</returns>
        public virtual void LoadConfig(NameValueCollection aConfig)
        {
            
        }
    }
}
