// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Microsoft.AspNetCore.Components;

namespace Zeron.Server.Components.Shared
{
    /// <summary>
    /// Pager Bar
    /// </summary>
    public partial class PagerBar
    {
        /// <summary>
        /// Title
        /// </summary>
        [Parameter]
        public string Title { get; set; } = "Results";

        /// <summary>
        /// Summary
        /// </summary>
        [Parameter]
        public string Summary { get; set; } = "";

        /// <summary>
        /// Is Loading
        /// </summary>
        [Parameter]
        public bool IsLoading { get; set; }

        /// <summary>
        /// Has Previous
        /// </summary>
        [Parameter]
        public bool HasPrevious { get; set; }

        /// <summary>
        /// Has Next
        /// </summary>
        [Parameter]
        public bool HasNext { get; set; }

        /// <summary>
        /// On Previous
        /// </summary>
        [Parameter]
        public EventCallback OnPrevious { get; set; }

        /// <summary>
        /// On Next
        /// </summary>
        [Parameter]
        public EventCallback OnNext { get; set; }
    }
}

