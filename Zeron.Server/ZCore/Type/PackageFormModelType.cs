// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.Server.ZCore.Type
{
    /// <summary>
    /// PackageFormModelType
    /// </summary>
    public sealed class PackageFormModelType
    {
        // Name.
        public string Name { get; set; } = "";

        // URL x86.
        public string Urlx86 { get; set; } = "";

        // URL x64.            
        public string Urlx64 { get; set; } = "";

        // Command install x86.
        public string CmdInstallx86 { get; set; } = "";

        // Command install x64.
        public string CmdInstallx64 { get; set; } = "";

        // Command uninstall x86.
        public string CmdUnInstallx86 { get; set; } = "";

        // Command uninstall x64.
        public string CmdUnInstallx64 { get; set; } = "";

        // SHA256 x86.
        public string Sha256x86 { get; set; } = "";

        // SHA256 x64.
        public string Sha256x64 { get; set; } = "";

        // Install before script.
        public string ScriptInstallBefore { get; set; } = "";

        // Install after script.
        public string ScriptInstallAfter { get; set; } = "";

        // Uninstall before script.
        public string ScriptUnInstallBefore { get; set; } = "";

        // Uninstall after script.
        public string ScriptUnInstallAfter { get; set; } = "";

        // Script engine id.
        public string ScriptEngine { get; set; } = "powershell";

        // Enabled.
        public bool IsEnabled { get; set; } = true;
    }
}
