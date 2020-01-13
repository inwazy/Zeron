using System;

namespace Zeron.Core
{
    /// <summary>
    /// ServicesSubAttribute
    /// </summary>
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
