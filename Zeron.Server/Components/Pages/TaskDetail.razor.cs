// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Microsoft.AspNetCore.Components;
using Zeron.Server.Data.Entities;

namespace Zeron.Server.Components.Pages
{
    /// <summary>
    /// TaskDetail
    /// </summary>
    public partial class TaskDetail
    {
        // Task ID.
        [Parameter]
        public Guid TaskId { get; set; }

        // Task.
        private TaskEntity? m_Task;

        /// <summary>
        /// OnParametersSetAsync
        /// </summary>
        /// <returns>Returns Task.</returns>
        protected override async Task OnParametersSetAsync()
        {
            m_Task = await TaskDispatcher.GetTaskAsync(TaskId);
        }

        /// <summary>
        /// CancelTaskAsync
        /// </summary>
        /// <returns>Returns Task.</returns>
        private async Task CancelTaskAsync()
        {
            await TaskDispatcher.CancelTaskAsync(TaskId);

            m_Task = await TaskDispatcher.GetTaskAsync(TaskId);
        }
    }
}
