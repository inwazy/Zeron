// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.Server.ZCore
{
    /// <summary>
    /// ServerPolicies
    /// </summary>
    public static class ServerPolicies
    {
        /// <summary>
        /// ViewerOrAbove
        /// </summary>
        public const string ViewerOrAbove = "ViewerOrAbove";

        /// <summary>
        /// OperatorOrAbove
        /// </summary>
        public const string OperatorOrAbove = "OperatorOrAbove";

        /// <summary>
        /// AdminOnly
        /// </summary>
        public const string AdminOnly = "AdminOnly";

        /// <summary>
        /// DeviceOwnerOnly - self-service portal access.
        /// </summary>
        public const string DeviceOwnerOnly = "DeviceOwnerOnly";

        /// <summary>
        /// DeviceOwnerOrStaff - DeviceOwner plus staff roles.
        /// </summary>
        public const string DeviceOwnerOrStaff = "DeviceOwnerOrStaff";
    }
}
