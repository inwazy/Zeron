// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.Server.ZCore.Type
{
    /// <summary>
    /// DeployFormModelType
    /// </summary>
    /// <returns>Returns void.</returns>
    public sealed class DeployFormModelType
    {
        // Operation.
        public string Operation { get; set; } = "install";

        // Package name.
        public string PackageName { get; set; } = "";

        // Extra args.
        public string? ExtraArgs { get; set; }

        // Name.
        public string? Name { get; set; }

        // Target type.
        public string TargetType { get; set; } = "all";

        // Agent ID.
        public string? AgentId { get; set; }

        // Hostname pattern.
        public string? HostnamePattern { get; set; }
    }
}
