// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Microsoft.AspNetCore.Components;

/// <summary>
/// Notification Banner
/// </summary>
namespace Zeron.Server.Components.Shared
{
    /// <summary>
    /// Notification Banner
    /// </summary>
    public partial class NotificationBanner
    {
        /// <summary>
        /// Success
        /// </summary>
        [Parameter, EditorRequired]
        public bool Success { get; set; }

        /// <summary>
        /// Title
        /// </summary>
        [Parameter]
        public string? Title { get; set; }

        /// <summary>
        /// Message
        /// </summary>
        [Parameter]
        public string? Message { get; set; }

        /// <summary>
        /// Show Dismiss
        /// </summary>
        [Parameter]
        public bool ShowDismiss { get; set; }

        /// <summary>
        /// On Dismiss
        /// </summary>
        [Parameter]
        public EventCallback OnDismiss { get; set; }

        /// <summary>
        /// Disable Dismiss
        /// </summary>
        [Parameter]
        public bool DisableDismiss { get; set; }

        /// <summary>
        /// Child Content
        /// </summary>
        [Parameter]
        public RenderFragment? ChildContent { get; set; }
    }
}

