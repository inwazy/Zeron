// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Microsoft.AspNetCore.Components;

namespace Zeron.Server.Components.Shared
{
    /// <summary>
    /// Loading Button
    /// </summary>
    public partial class LoadingButton
    {
        /// <summary>
        /// Css Class
        /// </summary>
        [Parameter]
        public string CssClass { get; set; } = "btn";

        /// <summary>
        /// Button Type
        /// </summary>
        [Parameter]
        public string ButtonType { get; set; } = "button";

        /// <summary>
        /// Is Loading
        /// </summary>
        [Parameter]
        public bool IsLoading { get; set; }

        /// <summary>
        /// Disabled
        /// </summary>
        [Parameter]
        public bool Disabled { get; set; }

        /// <summary>
        /// Loading Text
        /// </summary>
        [Parameter]
        public string LoadingText { get; set; } = "Working...";

        /// <summary>
        /// Text
        /// </summary>
        [Parameter]
        public string Text { get; set; } = "";

        /// <summary>
        /// Child Content
        /// </summary>
        [Parameter]
        public RenderFragment? ChildContent { get; set; }

        /// <summary>
        /// On Click
        /// </summary>
        [Parameter]
        public EventCallback OnClick { get; set; }
    }
}

