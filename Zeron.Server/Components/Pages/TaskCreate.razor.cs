// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Zeron.Server.Data.Entities;
using Zeron.Server.ZCore.Type;
using Zeron.ZCore.Type;

namespace Zeron.Server.Components.Pages
{
    /// <summary>
    /// TaskCreate
    /// </summary>
    public partial class TaskCreate
    {
        // Model.
        private readonly TaskFormModelType m_Model = new();

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
                TaskCreateRequestType request = new()
                {
                    Name = m_Model.Name,
                    Description = m_Model.Description,
                    TargetApi = m_Model.TargetApi,
                    Command = m_Model.Command,
                    TargetType = m_Model.TargetType,
                    HostnamePattern = m_Model.HostnamePattern,
                    AgentIds = string.IsNullOrWhiteSpace(m_Model.AgentId) ? null : [m_Model.AgentId]
                };

                TaskEntity task = await TaskDispatcher.CreateTaskAsync(request);

                Navigation.NavigateTo($"/tasks/{task.Id}");
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
