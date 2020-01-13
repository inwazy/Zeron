using NLog;

namespace Zeron.Core
{
    /// <summary>
    /// ZNLogger
    /// </summary>
    public static class ZNLogger
    {
        /// <summary>
        /// Common
        /// </summary>
        public static Logger Common
        {
            get
            {
                return LogManager.GetLogger("common");
            }
        }

        /// <summary>
        /// Error
        /// </summary>
        public static Logger Error
        {
            get
            {
                return LogManager.GetLogger("error");
            }
        }

        /// <summary>
        /// NativeError
        /// </summary>
        public static Logger NativeError
        {
            get
            {
                return LogManager.GetLogger("native_error");
            }
        }
    }
}
