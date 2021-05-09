// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using NLog.Internal;

namespace Zeron.Core.Base
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
        public virtual void LoadConfig(ConfigurationManager aConfig)
        {

        }
    }
}
