// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.ZCore.Type
{
    /// <summary>
    /// GateDecisionType - .NET gate handler outcome.
    /// </summary>
    public enum GateDecisionType
    {
        /// <summary>
        /// Proceed
        /// </summary>
        Proceed = 0,

        /// <summary>
        /// Pause - wait for Resume/Cancel/timeout.
        /// </summary>
        Pause = 1,

        /// <summary>
        /// Cancel - abort the work.
        /// </summary>
        Cancel = 2
    }
}
