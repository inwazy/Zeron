// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Zeron.Server.ZServers;
using Zeron.ZCore.Type;

namespace Zeron.Server.Components.Pages
{
    /// <summary>
    /// Schedules
    /// </summary>
    public partial class Schedules
    {
        // Schedules.
        private List<TaskScheduleInfoType> m_Schedules = [];

        /// <summary>
        /// OnInitializedAsync
        /// </summary>
        /// <returns>Returns Task.</returns>
        protected override async Task OnInitializedAsync()
        {
            m_Schedules = await TaskScheduleServer.GetSchedulesAsync();
        }
    }
}
