// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Microsoft.AspNetCore.Components;
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
        private readonly DeployFormModel m_Model = new();

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
        /// HandleDeployAsync
        /// </summary>
        /// <returns>Returns Task.</returns>
        private async Task HandleDeployAsync()
        {
            m_Error = null;
            m_IsSubmitting = true;

            try
            {
                PackageDeployResponseType response = await PackageDeployServer.DeployAsync(new PackageDeployRequestType
                {
                    Operation = m_Model.Operation,
                    PackageName = m_Model.PackageName,
                    ExtraArgs = m_Model.ExtraArgs,
                    Name = string.IsNullOrWhiteSpace(m_Model.Name) ? null : m_Model.Name,
                    TargetType = m_Model.TargetType,
                    AgentIds = string.IsNullOrWhiteSpace(m_Model.AgentId) ? null : [m_Model.AgentId],
                    HostnamePattern = m_Model.HostnamePattern
                });

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

        /// <summary>
        /// DeployFormModel
        /// </summary>
        /// <returns>Returns void.</returns>
        private sealed class DeployFormModel
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
}
