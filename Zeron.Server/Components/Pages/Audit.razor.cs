// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Zeron.ZCore.Type;

namespace Zeron.Server.Components.Pages
{
    /// <summary>
    /// Audit
    /// </summary>
    public partial class Audit
    {
        // Rows.
        private List<AuditLogInfoType> m_Rows = [];

        // Filters.
        private string m_ActionFilter = "";
        private string m_ActorFilter = "";
        private string m_TargetFilter = "";
        private string m_SourceFilter = "";

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
            m_IsBusy = true;

            try
            {
                m_Rows = await AuditLogServer.QueryAsync(
                    string.IsNullOrWhiteSpace(m_ActionFilter) ? null : m_ActionFilter,
                    string.IsNullOrWhiteSpace(m_ActorFilter) ? null : m_ActorFilter,
                    string.IsNullOrWhiteSpace(m_TargetFilter) ? null : m_TargetFilter,
                    string.IsNullOrWhiteSpace(m_SourceFilter) ? null : m_SourceFilter);
            }
            finally
            {
                m_IsBusy = false;
            }
        }
    }
}
