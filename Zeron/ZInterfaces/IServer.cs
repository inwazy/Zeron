// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.ZInterfaces
{
    /// <summary>
    /// IServer
    /// </summary>
    public interface IServer
    {
        /// <summary>
        /// Initialize
        /// </summary>
        /// <returns>Returns void.</returns>
        void Initialize();

        /// <summary>
        /// Stop
        /// </summary>
        /// <returns>Returns void.</returns>
        void Stop();
    }
}
