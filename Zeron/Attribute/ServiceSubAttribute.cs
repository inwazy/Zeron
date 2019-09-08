using System;

namespace Zeron.Core
{
    /// <summary>
    /// ServicesSubAttribute
    /// </summary>
    public class ServicesSubAttribute : Attribute
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
