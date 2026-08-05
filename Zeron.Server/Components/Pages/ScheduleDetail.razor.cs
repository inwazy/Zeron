// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Microsoft.AspNetCore.Components;
using Zeron.Server.ZServers;
using Zeron.ZCore.Type;

namespace Zeron.Server.Components.Pages
{
    /// <summary>
    /// ScheduleDetail
    /// </summary>
    public partial class ScheduleDetail
    {
        // Schedule ID.
        [Parameter]
        public Guid ScheduleId { get; set; }

        // Schedule.
        private TaskScheduleInfoType? m_Schedule;

        // Edit form model.
        private readonly EditFormModel m_Edit = new();

        // Message.
        private string? m_Message;

        // Is succeeded.
        private bool m_Succeeded;

        // Is busy.
        private bool m_IsBusy;

        /// <summary>
        /// OnParametersSetAsync
        /// </summary>
        /// <returns>Returns Task.</returns>
        protected override async Task OnParametersSetAsync()
        {
            await ReloadAsync();
        }

        /// <summary>
        /// ReloadAsync
        /// </summary>
        /// <returns>Returns Task.</returns>
        private async Task ReloadAsync()
        {
            m_Schedule = await TaskScheduleServer.GetScheduleAsync(ScheduleId);

            if (m_Schedule == null)
            {
                return;
            }

            m_Edit.Name = m_Schedule.Name;
            m_Edit.Description = m_Schedule.Description ?? "";
            m_Edit.Cron = m_Schedule.Cron;
            m_Edit.TargetApi = m_Schedule.TargetApi;
            m_Edit.Command = m_Schedule.Command;
            m_Edit.TargetType = m_Schedule.TargetType;
            m_Edit.AgentId = m_Schedule.AgentIds?.FirstOrDefault() ?? "";
            m_Edit.HostnamePattern = m_Schedule.HostnamePattern ?? "";
        }

        /// <summary>
        /// SaveAsync
        /// </summary>
        /// <returns>Returns Task.</returns>
        private async Task SaveAsync()
        {
            m_Message = null;
            m_IsBusy = true;

            try
            {
                (TaskScheduleInfoType? updated, string? error) = await TaskScheduleServer.UpdateScheduleAsync(
                    ScheduleId,
                    new TaskScheduleUpdateRequestType
                    {
                        Name = m_Edit.Name,
                        Description = m_Edit.Description,
                        Cron = m_Edit.Cron,
                        TargetApi = m_Edit.TargetApi,
                        Command = m_Edit.Command,
                        TargetType = m_Edit.TargetType,
                        AgentIds = string.IsNullOrWhiteSpace(m_Edit.AgentId) ? null : [m_Edit.AgentId],
                        HostnamePattern = string.IsNullOrWhiteSpace(m_Edit.HostnamePattern) ? null : m_Edit.HostnamePattern
                    });

                if (error != null)
                {
                    m_Succeeded = false;
                    m_Message = error;
                    return;
                }

                m_Schedule = updated;
                m_Succeeded = true;
                m_Message = "Schedule updated.";
                await ReloadAsync();
            }
            finally
            {
                m_IsBusy = false;
            }
        }

        /// <summary>
        /// SetEnabledAsync
        /// </summary>
        /// <param name="enabled"></param>
        /// <returns>Returns Task.</returns>
        private async Task SetEnabledAsync(
            bool enabled)
        {
            m_Message = null;
            m_IsBusy = true;

            try
            {
                (TaskScheduleInfoType? updated, string? error) = await TaskScheduleServer.SetEnabledAsync(ScheduleId, enabled);

                if (error != null)
                {
                    m_Succeeded = false;
                    m_Message = error;
                    return;
                }

                m_Schedule = updated;
                m_Succeeded = true;
                m_Message = enabled ? "Schedule enabled." : "Schedule disabled.";
            }
            finally
            {
                m_IsBusy = false;
            }
        }

        /// <summary>
        /// RunNowAsync
        /// </summary>
        /// <returns>Returns Task.</returns>
        private async Task RunNowAsync()
        {
            m_Message = null;
            m_IsBusy = true;

            try
            {
                (Guid? taskId, string? error) = await TaskScheduleServer.TriggerNowAsync(ScheduleId);

                if (error != null || taskId == null)
                {
                    m_Succeeded = false;
                    m_Message = error ?? "Failed to run schedule.";
                    return;
                }

                Navigation.NavigateTo($"/tasks/{taskId}");
            }
            finally
            {
                m_IsBusy = false;
            }
        }

        /// <summary>
        /// DeleteAsync
        /// </summary>
        /// <returns>Returns Task.</returns>
        private async Task DeleteAsync()
        {
            m_IsBusy = true;

            try
            {
                bool deleted = await TaskScheduleServer.DeleteScheduleAsync(ScheduleId);

                if (!deleted)
                {
                    m_Succeeded = false;
                    m_Message = "Schedule not found.";
                    return;
                }

                Navigation.NavigateTo("/schedules");
            }
            finally
            {
                m_IsBusy = false;
            }
        }

        /// <summary>
        /// EditFormModel
        /// </summary>
        /// <returns>Returns void.</returns>
        private sealed class EditFormModel
        {
            // Name.
            public string Name { get; set; } = "";

            // Description.
            public string Description { get; set; } = "";

            // Cron.
            public string Cron { get; set; } = "";

            // Target API.
            public string TargetApi { get; set; } = "";

            // Command.
            public string Command { get; set; } = "";

            // Target type.
            public string TargetType { get; set; } = "all";

            // Agent ID.
            public string AgentId { get; set; } = "";

            // Hostname pattern.
            public string HostnamePattern { get; set; } = "";
        }

    }
}
