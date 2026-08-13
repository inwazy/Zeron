// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.ZInterfaces
{
    /// <summary>
    /// IZeronServerPlugin - Server-side .NET plugin (event bus + gate).
    /// Register in-process; directory scan is optional later.
    /// </summary>
    public interface IZeronServerPlugin
    {
        /// <summary>
        /// Id
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Initialize
        /// </summary>
        /// <param name="bus"></param>
        /// <param name="gate"></param>
        /// <returns>Returns void.</returns>
        void Initialize(
            IZeronEventBus bus,
            IGateController gate);

        /// <summary>
        /// Stop
        /// </summary>
        /// <returns>Returns void.</returns>
        void Stop();
    }
}
