// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Microsoft.AspNetCore.Components.Authorization;
using Zeron.Server.Data.Entities;
using Zeron.Server.ZServers;
using Zeron.ZCore.Type;

namespace Zeron.Server.Components.Pages
{
    /// <summary>
    /// DeviceBindings
    /// </summary>
    public partial class DeviceBindings
    {
        // Bindings.
        private List<UserAgentBindingInfoType> m_Bindings = [];

        // Users.
        private List<UserInfoType> m_Users = [];

        // Agents.
        private List<AgentEntity> m_Agents = [];

        // Form.
        private readonly BindFormModel m_Form = new();

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
            m_Bindings = await BindingServer.GetBindingsAsync();
            m_Users = await UserManager.GetUsersAsync();
            m_Agents = await AgentManager.GetAgentsAsync();
        }

        /// <summary>
        /// BindAsync
        /// </summary>
        /// <returns>Returns Task.</returns>
        private async Task BindAsync()
        {
            m_IsBusy = true;
            m_Message = null;

            try
            {
                AuthenticationState authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
                (UserAgentBindingInfoType? binding, string? error) = await BindingServer.CreateBindingAsync(
                    new UserAgentBindingRequestType
                    {
                        UserId = m_Form.UserId,
                        AgentKey = m_Form.AgentKey
                    },
                    actor: AuditLogServer.FromPrincipal(authState.User));

                if (error != null)
                {
                    m_Succeeded = false;
                    m_Message = error;
                    return;
                }

                m_Succeeded = true;
                m_Message = $"Bound '{binding!.Username}' to '{binding.AgentKey}'.";
                m_Form.UserId = "";
                m_Form.AgentKey = "";
                await ReloadAsync();
            }
            finally
            {
                m_IsBusy = false;
            }
        }

        /// <summary>
        /// UnbindAsync
        /// </summary>
        /// <param name="binding"></param>
        /// <returns>Returns Task.</returns>
        private async Task UnbindAsync(
            UserAgentBindingInfoType binding)
        {
            if (!Guid.TryParse(binding.Id, out Guid bindingId))
            {
                return;
            }

            m_IsBusy = true;
            m_Message = null;

            try
            {
                AuthenticationState authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
                string? error = await BindingServer.UnbindAsync(
                    bindingId,
                    actor: AuditLogServer.FromPrincipal(authState.User));

                if (error != null)
                {
                    m_Succeeded = false;
                    m_Message = error;
                    return;
                }

                m_Succeeded = true;
                m_Message = "Binding removed.";
                await ReloadAsync();
            }
            finally
            {
                m_IsBusy = false;
            }
        }

        /// <summary>
        /// BindFormModel
        /// </summary>
        private sealed class BindFormModel
        {
            public string UserId { get; set; } = "";
            public string AgentKey { get; set; } = "";
        }
    }
}
