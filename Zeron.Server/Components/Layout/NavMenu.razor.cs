// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System.Reflection;

namespace Zeron.Server.Components.Layout
{
    /// <summary>
    /// NavMenu
    /// </summary>
    public partial class NavMenu
    {
        // Show About modal.
        private bool m_ShowAbout;

        // Focus About modal.
        private bool m_FocusAbout;

        // About modal panel.
        private ElementReference m_AboutPanel;

        // Version.
        private readonly string m_Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";

        /// <summary>
        /// GetRole
        /// </summary>
        /// <param name="user"></param>
        /// <returns>Returns string.</returns>
        private static string GetRole(
            System.Security.Claims.ClaimsPrincipal user)
        {
            return user.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "";
        }

        /// <summary>
        /// OpenAbout
        /// </summary>
        /// <returns>Returns void.</returns>
        private void OpenAbout()
        {
            m_ShowAbout = true;
            m_FocusAbout = true;
        }

        /// <summary>
        /// CloseAbout
        /// </summary>
        /// <returns>Returns void.</returns>
        private void CloseAbout()
        {
            m_ShowAbout = false;
            m_FocusAbout = false;
        }

        /// <summary>
        /// OnAboutKeyDown
        /// </summary>
        /// <param name="args"></param>
        /// <returns>Returns void.</returns>
        private void OnAboutKeyDown(
            KeyboardEventArgs args)
        {
            if (args.Key == "Escape")
            {
                CloseAbout();
            }
        }

        /// <summary>
        /// OnAfterRenderAsync
        /// </summary>
        /// <param name="firstRender"></param>
        /// <returns>Returns Task.</returns>
        protected override async Task OnAfterRenderAsync(
            bool firstRender)
        {
            if (m_ShowAbout && m_FocusAbout)
            {
                m_FocusAbout = false;

                try
                {
                    await m_AboutPanel.FocusAsync();
                }
                catch
                {
                }
            }
        }

    }
}
