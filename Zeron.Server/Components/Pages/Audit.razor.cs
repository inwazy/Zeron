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

        // Action Filters.
        private string m_ActionFilter = "";

        // Actor Filters.
        private string m_ActorFilter = "";

        // Target Filters.
        private string m_TargetFilter = "";

        // Source Filters.
        private string m_SourceFilter = "";

        // Busy.
        private bool m_IsBusy;

        // Pagination.
        private const int c_PageSize = 50;
        private int m_PageIndex;
        private bool m_HasNextPage;

        // Current page rows.
        private List<AuditLogInfoType> m_PageRows = [];

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
                int offset = m_PageIndex * c_PageSize;
                m_Rows = await AuditLogServer.QueryAsync(
                    string.IsNullOrWhiteSpace(m_ActionFilter) ? null : m_ActionFilter,
                    string.IsNullOrWhiteSpace(m_ActorFilter) ? null : m_ActorFilter,
                    string.IsNullOrWhiteSpace(m_TargetFilter) ? null : m_TargetFilter,
                    string.IsNullOrWhiteSpace(m_SourceFilter) ? null : m_SourceFilter,
                    limit: c_PageSize + 1,
                    offset: offset);

                m_HasNextPage = m_Rows.Count > c_PageSize;
                m_PageRows = m_Rows.Take(c_PageSize).ToList();
            }
            finally
            {
                m_IsBusy = false;
            }
        }

        /// <summary>
        /// ApplyFiltersAsync
        /// </summary>
        /// <returns>Returns Task.</returns>
        private async Task ApplyFiltersAsync()
        {
            m_PageIndex = 0;
            await ReloadAsync();
        }

        /// <summary>
        /// GoPrevPageAsync
        /// </summary>
        /// <returns>Returns Task.</returns>
        private async Task GoPrevPageAsync()
        {
            if (m_PageIndex <= 0)
            {
                return;
            }

            m_PageIndex--;
            await ReloadAsync();
        }

        /// <summary>
        /// GoNextPageAsync
        /// </summary>
        /// <returns>Returns Task.</returns>
        private async Task GoNextPageAsync()
        {
            if (!m_HasNextPage)
            {
                return;
            }

            m_PageIndex++;
            await ReloadAsync();
        }

        /// <summary>
        /// PageSummary
        /// </summary>
        private string PageSummary =>
            m_PageRows.Count == 0
                ? "No records"
                : $"Page {m_PageIndex + 1} · showing {m_PageRows.Count} record(s)";
    }
}
