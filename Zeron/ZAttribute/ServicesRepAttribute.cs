// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.ZAttribute
{
    /// <summary>
    /// ServicesRepAttribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ServicesRepAttribute : Attribute
    {
        /// <summary>
        /// ZmqApiName
        /// </summary>
        public string? ZmqApiName
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
        /// ZmqNotifySubscriber
        /// </summary>
        public bool ZmqNotifySubscriber
        {
            get;
            set;
        }

        /// <summary>
        /// ServicesRepAttribute
        /// </summary>
        /// <returns>Returns void.</returns>
        public ServicesRepAttribute()
        {
            ZmqApiName = "";
            ZmqApiEnabled = true;
            ZmqNotifySubscriber = false;
        }
    }
}
