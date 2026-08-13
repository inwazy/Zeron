// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Microsoft.AspNetCore.Components.Authorization;
using Zeron.Server.ZCore.Type;
using Zeron.Server.ZServers;
using Zeron.ZCore.Type;

namespace Zeron.Server.Components.Pages
{
    /// <summary>
    /// PackageDeploy
    /// </summary>
    public partial class PackageDeploy
    {
        // Model.
        private readonly DeployFormModelType m_Model = new();

        // Catalog packages.
        private List<ManagedPackageInfoType> m_Packages = [];

        // Error.
        private string? m_Error;

        // Is submitting.
        private bool m_IsSubmitting;

        // Command preview.
        private string m_CommandPreview =>
            string.IsNullOrWhiteSpace(m_Model.PackageName)
                ? ""
                : PackageDeployServer.BuildCommand(
                    m_Model.Operation,
                    m_Model.PackageName.Trim(),
                    m_Model.ExtraArgs);

        /// <summary>
        /// OnInitializedAsync
        /// </summary>
        /// <returns>Returns Task.</returns>
        protected override async Task OnInitializedAsync()
        {
            m_Packages = await CatalogServer.GetPackagesAsync(enabledOnly: true);
        }

        /// <summary>
        /// HandleDeployAsync
        /// </summary>
        /// <returns>Returns Task.</returns>
        private async Task HandleDeployAsync()
        {
            m_Error = null;
            m_IsSubmitting = true;

            try
            {
                AuthenticationState authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
                PackageDeployResponseType response = await PackageDeployServer.DeployAsync(
                    new PackageDeployRequestType
                    {
                        Operation = m_Model.Operation,
                        PackageName = m_Model.PackageName,
                        ExtraArgs = m_Model.ExtraArgs,
                        Name = string.IsNullOrWhiteSpace(m_Model.Name) ? null : m_Model.Name,
                        TargetType = m_Model.TargetType,
                        AgentIds = string.IsNullOrWhiteSpace(m_Model.AgentId) ? null : [m_Model.AgentId],
                        HostnamePattern = m_Model.HostnamePattern
                    },
                    actor: AuditLogServer.FromPrincipal(authState.User));

                if (!response.Success || response.TaskId == null)
                {
                    m_Error = response.Message ?? "Deploy failed.";

                    return;
                }

                Navigation.NavigateTo($"/tasks/{response.TaskId}");
            }
            catch (Exception ex)
            {
                m_Error = ex.Message;
            }
            finally
            {
                m_IsSubmitting = false;
            }
        }
    }
}
