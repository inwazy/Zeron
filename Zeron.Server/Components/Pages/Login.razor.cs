// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Microsoft.AspNetCore.Components;
using Zeron.Server.ZServers;
using Zeron.ZCore.Type;

namespace Zeron.Server.Components.Pages
{
    /// <summary>
    /// Login
    /// </summary>
    public partial class Login
    {
        // Failed query.
        [SupplyParameterFromQuery(Name = "failed")]
        public string? Failed { get; set; }

        // Show error.
        private bool m_ShowError;

        /// <summary>
        /// OnInitialized
        /// </summary>
        /// <returns>Returns void.</returns>    
        protected override void OnInitialized()
        {
            m_ShowError = Failed == "1";
    }
}
