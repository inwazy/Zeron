// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using System;

namespace Zeron.Core
{
    /// <summary>
    /// ServicesSubAttribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ServicesSubAttribute : Attribute
    {
        /// <summary>
        /// ZmqApiName
        /// </summary>
        public string ZmqApiName
        {
            get;
            set;
        }

        /// <summary>
        /// ZmqApiEnabled
        /// </summary>
        public bool ZmqApiEnabled
        {
            get;
            set;
        }

        /// <summary>
        /// ServicesSubAttribute
        /// </summary>
        /// <returns>Returns void.</returns>
        public ServicesSubAttribute()
        {
            ZmqApiEnabled = true;
        }
    }
}
