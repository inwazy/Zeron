// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.Server.Components.Shared
{
    /// <summary>
    /// AlertRowAction — optional per-row link from editions (e.g. Copilot explain).
    /// </summary>
    public sealed class AlertRowAction
    {
        /// <summary>
        /// Href
        /// </summary>
        public string Href { get; init; } = "";

        /// <summary>
        /// Text
        /// </summary>
        public string Text { get; init; } = "";
    }
}
