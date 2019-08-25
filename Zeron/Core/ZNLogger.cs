using NLog;

namespace Zeron.Core
{
    /// <summary>
    /// ZNLogger
    /// </summary>
    public static class ZNLogger
    {
        // CommonLog
        public static Logger Common
        {
            get
            {
                return LogManager.GetLogger("common");
            }
        }

        // ErrorLog
        public static Logger Error
        {
            get
            {
                return LogManager.GetLogger("error");
            }
        }

        // NativeErrorLog
        public static Logger NativeError
        {
            get
            {
                return LogManager.GetLogger("native_error");
            }
        }
    }
}
