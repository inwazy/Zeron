// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.Server.Components
{
    /// <summary>
    /// RedirectToLogin
    /// </summary>
    public partial class RedirectToLogin
    {
        /// <summary>
        /// OnInitialized
        /// </summary>
        /// <returns>Returns void.</returns>
        protected override void OnInitialized()
        {
            Navigation.NavigateTo("/login", true);
        }
    }
}
