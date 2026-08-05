// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.Demand.ZCore
{
    /// <summary>
    /// RemoteCommandContext - flows AssignmentId into service handlers during SUB dispatch.
    /// </summary>
    internal static class RemoteCommandContext
    {
        // Assignment id for the current remote command invocation.
        private static readonly AsyncLocal<string?> s_AssignmentId = new();

        /// <summary>
        /// AssignmentId
        /// </summary>
        public static string? AssignmentId
        {
            get => s_AssignmentId.Value;
            set => s_AssignmentId.Value = value;
        }
    }
}
