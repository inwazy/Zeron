// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Zeron.ZCore.Type;

namespace Zeron.ZInterfaces
{
    /// <summary>
    /// IGateController - register handlers and Resume/Cancel paused work.
    /// Scripts cannot call this API.
    /// </summary>
    public interface IGateController
    {
        /// <summary>
        /// Register
        /// </summary>
        /// <param name="handler"></param>
        /// <returns>Returns void.</returns>
        void Register(
            IGateHandler handler);

        /// <summary>
        /// Unregister
        /// </summary>
        /// <param name="handler"></param>
        /// <returns>Returns bool.</returns>
        bool Unregister(
            IGateHandler handler);

        /// <summary>
        /// Evaluate - run handlers; Pause waits until Resume/Cancel/timeout.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="timeoutMs"></param>
        /// <returns>Returns GateDecision.</returns>
        GateDecisionType Evaluate(
            GateContextType context,
            int timeoutMs = 0);

        /// <summary>
        /// Resume
        /// </summary>
        /// <param name="correlationId"></param>
        /// <param name="reason"></param>
        /// <returns>Returns bool.</returns>
        bool Resume(
            string correlationId,
            string? reason = null);

        /// <summary>
        /// Cancel
        /// </summary>
        /// <param name="correlationId"></param>
        /// <param name="reason"></param>
        /// <returns>Returns bool.</returns>
        bool Cancel(
            string correlationId,
            string? reason = null);

        /// <summary>
        /// Clear - test helper.
        /// </summary>
        /// <returns>Returns void.</returns>
        void Clear();
    }
}
