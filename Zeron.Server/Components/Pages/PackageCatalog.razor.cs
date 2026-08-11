// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Microsoft.AspNetCore.Components.Authorization;
using Zeron.Server.ZServers;
using Zeron.ZCore.Type;

namespace Zeron.Server.Components.Pages
{
    /// <summary>
    /// PackageCatalog
    /// </summary>
    public partial class PackageCatalog
    {
        // Packages.
        private List<ManagedPackageInfoType> m_Packages = [];

        // History package.
        private ManagedPackageInfoType? m_HistoryPackage;

        // Versions for history panel.
        private List<ManagedPackageVersionInfoType> m_Versions = [];

        // Form.
        private readonly PackageFormModel m_Form = new();

        // Edit id.
        private Guid? m_EditId;

        // Message.
        private string? m_Message;

        // History message.
        private string? m_HistoryMessage;

        // Succeeded.
        private bool m_Succeeded;

        // History succeeded.
        private bool m_HistorySucceeded;

        // Busy.
        private bool m_IsBusy;

        /// <summary>
        /// OnInitializedAsync
        /// </summary>
        /// <returns>Returns Task.</returns>
        protected override async Task OnInitializedAsync()
        {
            await ReloadAsync();
        }

        /// <summary>
        /// ReloadAsync
        /// </summary>
        /// <returns>Returns Task.</returns>
        private async Task ReloadAsync()
        {
            m_Packages = await CatalogServer.GetPackagesAsync();

            if (m_HistoryPackage != null && Guid.TryParse(m_HistoryPackage.Id, out Guid packageId))
            {
                m_Versions = await CatalogServer.GetPackageVersionsAsync(packageId);
                m_HistoryPackage = m_Packages.FirstOrDefault(item => item.Id == m_HistoryPackage.Id)
                    ?? m_HistoryPackage;
            }
        }

        /// <summary>
        /// BeginEdit
        /// </summary>
        /// <param name="package"></param>
        /// <returns>Returns void.</returns>
        private void BeginEdit(
            ManagedPackageInfoType package)
        {
            if (!Guid.TryParse(package.Id, out Guid packageId))
            {
                return;
            }

            m_EditId = packageId;
            m_Form.Name = package.Name ?? "";
            m_Form.Urlx86 = package.Urlx86 ?? "";
            m_Form.Urlx64 = package.Urlx64 ?? "";
            m_Form.CmdInstallx86 = package.CmdInstallx86 ?? "";
            m_Form.CmdInstallx64 = package.CmdInstallx64 ?? "";
            m_Form.CmdUnInstallx86 = package.CmdUnInstallx86 ?? "";
            m_Form.CmdUnInstallx64 = package.CmdUnInstallx64 ?? "";
            m_Form.Sha256x86 = package.Sha256x86 ?? "";
            m_Form.Sha256x64 = package.Sha256x64 ?? "";
            m_Form.ScriptEngine = string.IsNullOrWhiteSpace(package.ScriptEngine) ? "powershell" : package.ScriptEngine;
            m_Form.IsEnabled = package.IsEnabled;
            m_Message = null;
        }

        /// <summary>
        /// CancelEdit
        /// </summary>
        /// <returns>Returns void.</returns>
        private void CancelEdit()
        {
            m_EditId = null;
            ResetForm();
            m_Message = null;
        }

        /// <summary>
        /// ShowHistoryAsync
        /// </summary>
        /// <param name="package"></param>
        /// <returns>Returns Task.</returns>
        private async Task ShowHistoryAsync(
            ManagedPackageInfoType package)
        {
            if (!Guid.TryParse(package.Id, out Guid packageId))
            {
                return;
            }

            m_HistoryPackage = package;
            m_HistoryMessage = null;
            m_IsBusy = true;

            try
            {
                m_Versions = await CatalogServer.GetPackageVersionsAsync(packageId);
            }
            finally
            {
                m_IsBusy = false;
            }
        }

        /// <summary>
        /// CloseHistory
        /// </summary>
        /// <returns>Returns void.</returns>
        private void CloseHistory()
        {
            m_HistoryPackage = null;
            m_Versions = [];
            m_HistoryMessage = null;
        }

        /// <summary>
        /// RollbackAsync
        /// </summary>
        /// <param name="versionNumber"></param>
        /// <returns>Returns Task.</returns>
        private async Task RollbackAsync(
            int versionNumber)
        {
            if (m_HistoryPackage == null || !Guid.TryParse(m_HistoryPackage.Id, out Guid packageId))
            {
                return;
            }

            m_IsBusy = true;
            m_HistoryMessage = null;

            try
            {
                (ManagedPackageInfoType? package, string? error) = await CatalogServer.RollbackPackageAsync(
                    packageId,
                    versionNumber,
                    actor: await GetActorAsync());

                if (error != null)
                {
                    m_HistorySucceeded = false;
                    m_HistoryMessage = error;
                    return;
                }

                m_HistorySucceeded = true;
                m_HistoryMessage = $"Restored '{package!.Name}' to version {versionNumber} and pushed catalog sync.";
                m_HistoryPackage = package;
                await ReloadAsync();
            }
            finally
            {
                m_IsBusy = false;
            }
        }

        /// <summary>
        /// VersionKindClass
        /// </summary>
        /// <param name="changeKind"></param>
        /// <returns>Returns css class.</returns>
        private static string VersionKindClass(
            string? changeKind)
        {
            return changeKind switch
            {
                "create" => "healthy",
                "rollback" => "stale",
                "update" => "pending",
                _ => "offline"
            };
        }

        /// <summary>
        /// SaveAsync
        /// </summary>
        /// <returns>Returns Task.</returns>
        private async Task SaveAsync()
        {
            m_IsBusy = true;
            m_Message = null;

            try
            {
                ManagedPackageUpsertRequestType request = new()
                {
                    Name = m_Form.Name,
                    Urlx86 = m_Form.Urlx86,
                    Urlx64 = m_Form.Urlx64,
                    CmdInstallx86 = m_Form.CmdInstallx86,
                    CmdInstallx64 = m_Form.CmdInstallx64,
                    CmdUnInstallx86 = m_Form.CmdUnInstallx86,
                    CmdUnInstallx64 = m_Form.CmdUnInstallx64,
                    Sha256x86 = m_Form.Sha256x86,
                    Sha256x64 = m_Form.Sha256x64,
                    ScriptEngine = m_Form.ScriptEngine,
                    IsEnabled = m_Form.IsEnabled
                };

                AuditActorType? actor = await GetActorAsync();

                if (m_EditId == null)
                {
                    (ManagedPackageInfoType? package, string? error) = await CatalogServer.CreatePackageAsync(
                        request,
                        actor: actor);

                    if (error != null)
                    {
                        m_Succeeded = false;
                        m_Message = error;
                        return;
                    }

                    m_Succeeded = true;
                    m_Message = $"Created package '{package!.Name}'.";
                    ResetForm();
                }
                else
                {
                    (ManagedPackageInfoType? package, string? error) = await CatalogServer.UpdatePackageAsync(
                        m_EditId.Value,
                        request,
                        actor: actor);

                    if (error != null)
                    {
                        m_Succeeded = false;
                        m_Message = error;
                        return;
                    }

                    m_Succeeded = true;
                    m_Message = $"Updated package '{package!.Name}'.";
                    m_EditId = null;
                    ResetForm();
                }

                await ReloadAsync();
            }
            finally
            {
                m_IsBusy = false;
            }
        }

        /// <summary>
        /// DeleteAsync
        /// </summary>
        /// <param name="package"></param>
        /// <returns>Returns Task.</returns>
        private async Task DeleteAsync(
            ManagedPackageInfoType package)
        {
            if (!Guid.TryParse(package.Id, out Guid packageId))
            {
                return;
            }

            m_IsBusy = true;
            m_Message = null;

            try
            {
                string? error = await CatalogServer.DeletePackageAsync(
                    packageId,
                    actor: await GetActorAsync());

                if (error != null)
                {
                    m_Succeeded = false;
                    m_Message = error;
                    return;
                }

                m_Succeeded = true;
                m_Message = $"Deleted package '{package.Name}'.";

                if (m_EditId == packageId)
                {
                    CancelEdit();
                }

                if (m_HistoryPackage?.Id == package.Id)
                {
                    CloseHistory();
                }

                await ReloadAsync();
            }
            finally
            {
                m_IsBusy = false;
            }
        }

        /// <summary>
        /// GetActorAsync
        /// </summary>
        /// <returns>Returns audit actor.</returns>
        private async Task<AuditActorType?> GetActorAsync()
        {
            AuthenticationState authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            return AuditLogServer.FromPrincipal(authState.User);
        }

        /// <summary>
        /// ResetForm
        /// </summary>
        /// <returns>Returns void.</returns>
        private void ResetForm()
        {
            m_Form.Name = "";
            m_Form.Urlx86 = "";
            m_Form.Urlx64 = "";
            m_Form.CmdInstallx86 = "";
            m_Form.CmdInstallx64 = "";
            m_Form.CmdUnInstallx86 = "";
            m_Form.CmdUnInstallx64 = "";
            m_Form.Sha256x86 = "";
            m_Form.Sha256x64 = "";
            m_Form.ScriptEngine = "powershell";
            m_Form.IsEnabled = true;
        }

        /// <summary>
        /// PackageFormModel
        /// </summary>
        private sealed class PackageFormModel
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

            // Script engine id.
            public string ScriptEngine { get; set; } = "powershell";

            // Enabled.
            public bool IsEnabled { get; set; } = true;
        }
    }
}
