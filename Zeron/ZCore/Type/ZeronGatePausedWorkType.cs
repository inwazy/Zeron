// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.ZCore.Type
{
    /// <summary>
    /// ZeronGatePausedWorkType
    /// </summary>
    public sealed class ZeronGatePausedWorkType : IDisposable
    {
        /// <summary>
        /// Signal
        /// </summary>
        public ManualResetEventSlim Signal { get; } = new(false);

        /// <summary>
        /// ResumeDecision
        /// </summary>
        public GateDecisionType ResumeDecision { get; set; } = GateDecisionType.Proceed;

        /// <summary>
        /// Reason
        /// </summary>
        public string? Reason { get; set; }

        /// <summary>
        /// Dispose
        /// </summary>
        /// <returns>Returns void.</returns>
        public void Dispose()
        {
            Signal.Dispose();
        }
    }
}
