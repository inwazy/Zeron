// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Microsoft.AspNetCore.Components;
using Zeron.Server.Data.Entities;
using Zeron.Server.ZServers;
using Zeron.ZCore.Type;

namespace Zeron.Server.Components.Pages
{
    /// <summary>
    /// Packages
    /// </summary>
    public partial class Packages
    {
        // Deploys.
        private List<TaskEntity> m_Deploys = [];

        // Install events.
        private List<EventEntity> m_InstallEvents = [];

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
            m_Deploys = await PackageDeployServer.GetRecentDeploysAsync(20);
            m_InstallEvents = await PackageDeployServer.GetInstallEventsAsync(limit: 15);
        }

        /// <summary>
        /// FormatAssignmentOutcome
        /// </summary>
        /// <param name="task"></param>
        /// <returns>Returns string.</returns>
        private static string FormatAssignmentOutcome(
            TaskEntity task)
        {
            int completed = task.Assignments.Count(item => item.Status == "completed");
            int failed = task.Assignments.Count(item => item.Status == "failed");
            int running = task.Assignments.Count(item => item.Status is "running" or "dispatched" or "pending");

            return $"{completed} ok / {failed} failed / {running} in progress";
        }

    }
}
