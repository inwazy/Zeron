// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using NLog;

namespace Zeron.ZCore
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
