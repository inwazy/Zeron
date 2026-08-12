// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.Server.ZCore.Type
{
    /// <summary>
    /// CreateFormModelType
    /// </summary>
    /// <returns>Returns void.</returns>
    public sealed class CreateFormModelType
    {
        // Username.
        public string Username { get; set; } = "";

        // Password.
        public string Password { get; set; } = "";

        // Email.
        public string Email { get; set; } = "";

        // Role.
        public string Role { get; set; } = ServerRoles.Viewer;
    }
}
