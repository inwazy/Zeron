// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Microsoft.AspNetCore.Components;

namespace Zeron.Server.Components.Shared
{
    /// <summary>
    /// FilterChipsBar
    /// </summary>
    public partial class FilterChipsBar
    {
        /// <summary>
        /// Options
        /// </summary>
        /// <value>The options.</value>
        [Parameter]
        public IReadOnlyList<FilterChipOption> Options { get; set; } = [];

        /// <summary>
        /// SelectedValue
        /// </summary>
        /// <value>The selected value.</value>
        [Parameter]
        public string? SelectedValue { get; set; }

        /// <summary>
        /// IsLoading
        /// </summary>
        /// <value>The is loading.</value>
        [Parameter]
        public bool IsLoading { get; set; }

        /// <summary>
        /// OnSelect
        /// </summary>
        /// <value>The on select.</value>
        [Parameter]
        public EventCallback<string?> OnSelect { get; set; }

        /// <summary>
        /// FilterChipOption
        /// </summary>  
        public sealed class FilterChipOption
        {
            /// <summary>
            /// Label
            /// </summary>
            /// <value>The label.</value>
            public string Label { get; init; } = "";

            /// <summary>
            /// Value
            /// </summary>
            /// <value>The value.</value>
            public string? Value { get; init; }
        }
    }
}

