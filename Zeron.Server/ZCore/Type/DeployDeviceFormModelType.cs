// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.Server.ZCore.Type
{
    /// <summary>
    /// DeployDeviceFormModelType
    /// </summary>
    public sealed class DeployDeviceFormModelType
    {
        // Operation.
        public string Operation { get; set; } = "install";

        // Package name.
        public string PackageName { get; set; } = "";

        // Extra args.
        public string? ExtraArgs { get; set; }
    }
}
