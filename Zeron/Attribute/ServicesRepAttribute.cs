using System;

namespace Zeron.Core
{
    /// <summary>
    /// ServicesRepAttribute
    /// </summary>
    public class ServicesRepAttribute : Attribute
    {
        // ZmqApiName
        public string ZmqApiName
        {
            get;
            set;
        }

        // ZmqApiEnabled
        public bool ZmqApiEnabled
        {
            get;
            set;
        }

        // ZmqNotifySubscriber
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
            ZmqApiEnabled = true;
        }
    }
}
