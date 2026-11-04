// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;

namespace Zeron.Server.Components.Layout
{
    /// <summary>
    /// Interactive mobile sidebar toggle (separate from <see cref="MainLayout"/> to avoid RenderFragment serialization).
    /// </summary>
    public partial class MobileSidebarControls : IDisposable
    {
        /// <inheritdoc />
        /// <summary>
        /// OnInitialized
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected override void OnInitialized()
        {
            NavigationManager.LocationChanged += OnLocationChanged;
        }

        /// <summary>
        /// Toggles the mobile sidebar via JS (CSS class on <c>.page</c>).
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task ToggleSidebar()
        {
            await Js.InvokeVoidAsync("zeronLayout.toggleSidebar");
        }

        /// <summary>
        /// Closes the mobile sidebar overlay.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task CloseSidebar()
        {
            await Js.InvokeVoidAsync("zeronLayout.closeSidebar");
        }

        /// <summary>
        /// Closes sidebar after in-app navigation.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Location changed arguments.</param>
        private void OnLocationChanged(
            object? sender,
            LocationChangedEventArgs e)
        {
            _ = Js.InvokeVoidAsync("zeronLayout.closeSidebar");
        }

        /// <inheritdoc />
        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            NavigationManager.LocationChanged -= OnLocationChanged;
        }
    }
}
