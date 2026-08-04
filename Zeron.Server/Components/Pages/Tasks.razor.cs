// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Microsoft.AspNetCore.Components;
using Zeron.Server.Data.Entities;
using Zeron.Server.ZServers;
using Zeron.ZCore.Type;

namespace Zeron.Server.Components.Pages
{
    /// <summary>
    /// Tasks
    /// </summary>
    public partial class Tasks
    {
        // Tasks.
        private List<TaskEntity> m_Tasks = [];

        /// <summary>
        /// OnInitializedAsync
        /// </summary>
        /// <returns>Returns Task.</returns>
        protected override async Task OnInitializedAsync()
        {
            m_Tasks = await TaskDispatcher.GetTasksAsync();
        }

    }
}
