// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Microsoft.AspNetCore.Components;
using Zeron.Server.ZServers;
using Zeron.ZCore.Type;

namespace Zeron.Server.Components.Pages
{
    /// <summary>
    /// ScheduleCreate
    /// </summary>
    public partial class ScheduleCreate
    {
        // Model.
        private readonly ScheduleFormModel m_Model = new();

        // Error.
        private string? m_Error;

        // Is submitting.
        private bool m_IsSubmitting;

        /// <summary>
        /// HandleCreateAsync
        /// </summary>
        /// <returns>Returns Task.</returns>
        private async Task HandleCreateAsync()
        {
            m_Error = null;
            m_IsSubmitting = true;

            try
            {
                (TaskScheduleInfoType? schedule, string? error) = await TaskScheduleServer.CreateScheduleAsync(
                    new TaskScheduleCreateRequestType
                    {
                        Name = m_Model.Name,
                        Description = m_Model.Description,
                        Cron = m_Model.Cron,
                        Enabled = m_Model.Enabled,
                        TargetApi = m_Model.TargetApi,
                        Command = m_Model.Command,
                        TargetType = m_Model.TargetType,
                        HostnamePattern = m_Model.HostnamePattern,
                        AgentIds = string.IsNullOrWhiteSpace(m_Model.AgentId) ? null : [m_Model.AgentId]
                    });

                if (error != null)
                {
                    m_Error = error;
                    return;
                }

                Navigation.NavigateTo($"/schedules/{schedule!.Id}");
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
        /// ScheduleFormModel
        /// </summary>
        /// <returns>Returns void.</returns>
        private sealed class ScheduleFormModel
        {
            // Name.
            public string Name { get; set; } = "";

            // Description.
            public string? Description { get; set; }

            // Cron.
            public string Cron { get; set; } = "0 8 * * *";

            // Enabled.
            public bool Enabled { get; set; } = true;

            // Target API.
            public string TargetApi { get; set; } = "HealthCheck";

            // Command.
            public string Command { get; set; } = "";

            // Target type.
            public string TargetType { get; set; } = "all";

            // Agent ID.
            public string? AgentId { get; set; }

            // Hostname pattern.
            public string? HostnamePattern { get; set; }
        }

    }
}
