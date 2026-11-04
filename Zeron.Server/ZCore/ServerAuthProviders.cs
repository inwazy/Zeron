// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.Server.ZCore
{
    /// <summary>
    /// AuthProvider values stored on UserEntity.
    /// </summary>
    public static class ServerAuthProviders
    {
        /// <summary>
        /// Local username/password (default when null/empty).
        /// </summary>
        public const string Local = "local";

        /// <summary>
        /// Enterprise OIDC SSO.
        /// </summary>
        public const string Oidc = "oidc";
    }
}
