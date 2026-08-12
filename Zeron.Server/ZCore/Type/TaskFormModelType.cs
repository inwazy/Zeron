// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.


namespace Zeron.Server.ZCore.Type
{
    /// <summary>
    /// TaskFormModelType
    /// </summary>
    /// <returns>Returns void.</returns>
    public sealed class TaskFormModelType
    {
        // Name.
        public string Name { get; set; } = "";

        // Description.
        public string? Description { get; set; }

        // Target API.
        public string TargetApi { get; set; } = "HealthCheck";

        // Command.
        public string Command { get; set; } = "";

        // Target type.
        public string TargetType { get; set; } = "all";

        // Agent ID.
        public string? AgentId { get; set; }

        // Hostname pattern.
        public string? HostnamePattern { get; set; }
    }
}
