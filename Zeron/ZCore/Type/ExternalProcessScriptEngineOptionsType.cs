// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.ZCore.Type
{
    /// <summary>
    /// ExternalProcessScriptEngineOptionsType
    /// </summary>
    public sealed class ExternalProcessScriptEngineOptionsType
    {
        /// <summary>
        /// Id
        /// </summary>
        public string Id
        {
            get;
            set;
        } = "";

        /// <summary>
        /// DisplayName
        /// </summary>
        public string? DisplayName
        {
            get;
            set;
        }

        /// <summary>
        /// ExecutablePath
        /// </summary>
        public string ExecutablePath
        {
            get;
            set;
        } = "";

        /// <summary>
        /// ArgumentsTemplate - supports {scriptPath}, {arguments}, {script}.
        /// </summary>
        public string ArgumentsTemplate
        {
            get;
            set;
        } = "{scriptPath} {arguments}";

        /// <summary>
        /// Platforms
        /// </summary>
        public IReadOnlyList<string> Platforms
        {
            get;
            set;
        } = ["windows"];

        /// <summary>
        /// InlineMode
        /// </summary>
        public ExternalScriptInlineModeType InlineMode
        {
            get;
            set;
        } = ExternalScriptInlineModeType.StdIn;

        /// <summary>
        /// Enabled
        /// </summary>
        public bool Enabled
        {
            get;
            set;
        } = true;
    }
}
