// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Zeron.ZCore.Type;

namespace Zeron.ZInterfaces
{
    /// <summary>
    /// IGateHandler - .NET-only intercept hook.
    /// </summary>
    public interface IGateHandler
    {
        /// <summary>
        /// Handle
        /// </summary>
        /// <param name="context"></param>
        /// <returns>Returns void.</returns>
        void Handle(
            GateContextType context);
    }
}
