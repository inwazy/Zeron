// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.Server.ZCore.Type
{
    /// <summary>
    /// UserEditRowType
    /// </summary>
    /// <returns>Returns void.</returns>
    public sealed class UserEditRowType
    {
        // ID.
        public string Id { get; set; } = "";

        // Username.
        public string Username { get; set; } = "";

        // Role.
        public string Role { get; set; } = ServerRoles.Viewer;

        // Email.
        public string Email { get; set; } = "";

        // Is active.
        public bool IsActive { get; set; }

        // Must change password.
        public bool MustChangePassword { get; set; }

        // Created at.
        public DateTime? CreatedAt { get; set; }

        // New password.
        public string NewPassword { get; set; } = "";
    }
}
