// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

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

        // Form.
        private readonly PackageFormModel m_Form = new();

        // Edit id.
        private Guid? m_EditId;

        // Message.
        private string? m_Message;

        // Succeeded.
        private bool m_Succeeded;

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
                    IsEnabled = m_Form.IsEnabled
                };

                if (m_EditId == null)
                {
                    (ManagedPackageInfoType? package, string? error) = await CatalogServer.CreatePackageAsync(request);

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
                    (ManagedPackageInfoType? package, string? error) = await CatalogServer.UpdatePackageAsync(m_EditId.Value, request);

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
                string? error = await CatalogServer.DeletePackageAsync(packageId);

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

                await ReloadAsync();
            }
            finally
            {
                m_IsBusy = false;
            }
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
            m_Form.IsEnabled = true;
        }

        /// <summary>
        /// PackageFormModel
        /// </summary>
        private sealed class PackageFormModel
        {
            public string Name { get; set; } = "";
            public string Urlx86 { get; set; } = "";
            public string Urlx64 { get; set; } = "";
            public string CmdInstallx86 { get; set; } = "";
            public string CmdInstallx64 { get; set; } = "";
            public string CmdUnInstallx86 { get; set; } = "";
            public string CmdUnInstallx64 { get; set; } = "";
            public bool IsEnabled { get; set; } = true;
        }
    }
}
