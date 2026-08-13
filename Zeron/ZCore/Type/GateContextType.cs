// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.ZCore.Type
{
    /// <summary>
    /// GateContextType - passed to .NET gate handlers.
    /// </summary>
    public sealed class GateContextType
    {
        /// <summary>
        /// Topic
        /// </summary>
        public string Topic
        {
            get;
            init;
        } = "";

        /// <summary>
        /// PayloadJson
        /// </summary>
        public string PayloadJson
        {
            get;
            set;
        } = "{}";

        /// <summary>
        /// CorrelationId - required for Pause/Resume.
        /// </summary>
        public string? CorrelationId
        {
            get;
            init;
        }

        /// <summary>
        /// Decision
        /// </summary>
        public GateDecisionType Decision
        {
            get;
            set;
        } = GateDecisionType.Proceed;

        /// <summary>
        /// Reason
        /// </summary>
        public string? Reason
        {
            get;
            set;
        }
    }
}
