// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using System.Collections.Concurrent;
using System.Globalization;
using Zeron.ZCore.Type;
using Zeron.ZInterfaces;

namespace Zeron.ZCore.Utils
{
    /// <summary>
    /// ZeronGateServer - .NET-only intercept pipeline (Pause/Resume/Cancel).
    /// </summary>
    public sealed class ZeronGateServer : IGateController
    {
        /// <summary>
        /// DefaultTimeoutMs
        /// </summary>
        public const int DefaultTimeoutMs = 300000;

        // Process-wide instance.
        private static readonly ZeronGateServer s_Instance = new();

        // Handlers in registration order.
        private readonly List<IGateHandler> m_Handlers = [];

        // Handler list lock.
        private readonly object m_HandlerLock = new();

        // Paused work by correlation id.
        private readonly ConcurrentDictionary<string, ZeronGatePausedWorkType> m_Paused = new(StringComparer.OrdinalIgnoreCase);

        // Default pause timeout.
        private int m_DefaultTimeoutMs = DefaultTimeoutMs;

        /// <summary>
        /// Current
        /// </summary>
        public static IGateController Current => s_Instance;

        /// <summary>
        /// ConfigureDefaultTimeoutMs
        /// </summary>
        /// <param name="timeoutMs"></param>
        /// <returns>Returns void.</returns>
        public static void ConfigureDefaultTimeoutMs(
            int timeoutMs)
        {
            s_Instance.m_DefaultTimeoutMs = timeoutMs > 0 ? timeoutMs : DefaultTimeoutMs;
        }

        /// <summary>
        /// GetDefaultTimeoutMs
        /// </summary>
        /// <returns>Returns int.</returns>
        public static int GetDefaultTimeoutMs()
        {
            return s_Instance.m_DefaultTimeoutMs;
        }

        /// <summary>
        /// Register
        /// </summary>
        /// <param name="handler"></param>
        /// <returns>Returns void.</returns>
        public void Register(
            IGateHandler handler)
        {
            ArgumentNullException.ThrowIfNull(handler);

            lock (m_HandlerLock)
            {
                if (!m_Handlers.Contains(handler))
                {
                    m_Handlers.Add(handler);
                }
            }
        }

        /// <summary>
        /// Unregister
        /// </summary>
        /// <param name="handler"></param>
        /// <returns>Returns bool.</returns>
        public bool Unregister(
            IGateHandler handler)
        {
            if (handler == null)
            {
                return false;
            }

            lock (m_HandlerLock)
            {
                return m_Handlers.Remove(handler);
            }
        }

        /// <summary>
        /// Evaluate
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="payloadJson"></param>
        /// <param name="correlationId"></param>
        /// <param name="timeoutMs"></param>
        /// <returns>Returns GateDecision.</returns>
        public static GateDecisionType Evaluate(
            string topic,
            string? payloadJson,
            string? correlationId,
            int timeoutMs = 0)
        {
            return s_Instance.Evaluate(new GateContextType
            {
                Topic = topic,
                PayloadJson = payloadJson ?? "{}",
                CorrelationId = correlationId
            }, timeoutMs);
        }

        /// <summary>
        /// Evaluate
        /// </summary>
        /// <param name="context"></param>
        /// <param name="timeoutMs"></param>
        /// <returns>Returns GateDecision.</returns>
        public GateDecisionType Evaluate(
            GateContextType context,
            int timeoutMs = 0)
        {
            ArgumentNullException.ThrowIfNull(context);

            if (string.IsNullOrWhiteSpace(context.Topic))
            {
                return GateDecisionType.Proceed;
            }

            context.Decision = GateDecisionType.Proceed;
            List<IGateHandler> handlers;

            lock (m_HandlerLock)
            {
                handlers = [.. m_Handlers];
            }

            if (handlers.Count == 0)
            {
                return GateDecisionType.Proceed;
            }

            foreach (IGateHandler handler in handlers)
            {
                try
                {
                    handler.Handle(context);
                }
                catch (Exception e)
                {
                    ZNLogger.Common.Warn(string.Format(CultureInfo.InvariantCulture,
                        "ZeronGateServer handler error topic={0}: {1}", context.Topic, e.Message));
                }

                if (context.Decision == GateDecisionType.Cancel)
                {
                    PublishCancelled(context);

                    return GateDecisionType.Cancel;
                }
            }

            if (context.Decision != GateDecisionType.Pause)
            {
                return GateDecisionType.Proceed;
            }

            if (string.IsNullOrWhiteSpace(context.CorrelationId))
            {
                context.Decision = GateDecisionType.Cancel;
                context.Reason = string.IsNullOrWhiteSpace(context.Reason)
                    ? "Gate Pause requires CorrelationId."
                    : context.Reason;
                PublishCancelled(context);

                return GateDecisionType.Cancel;
            }

            int waitMs = timeoutMs > 0 ? timeoutMs : m_DefaultTimeoutMs;
            GateDecisionType waited = WaitForResume(context, waitMs);

            if (waited == GateDecisionType.Cancel)
            {
                PublishCancelled(context);
            }

            return waited;
        }

        /// <summary>
        /// Resume
        /// </summary>
        /// <param name="correlationId"></param>
        /// <param name="reason"></param>
        /// <returns>Returns bool.</returns>
        public bool Resume(
            string correlationId,
            string? reason = null)
        {
            return CompletePause(correlationId, GateDecisionType.Proceed, reason);
        }

        /// <summary>
        /// Cancel
        /// </summary>
        /// <param name="correlationId"></param>
        /// <param name="reason"></param>
        /// <returns>Returns bool.</returns>
        public bool Cancel(
            string correlationId,
            string? reason = null)
        {
            return CompletePause(correlationId, GateDecisionType.Cancel, reason);
        }

        /// <summary>
        /// Clear
        /// </summary>
        /// <returns>Returns void.</returns>
        public void Clear()
        {
            lock (m_HandlerLock)
            {
                m_Handlers.Clear();
            }

            foreach (KeyValuePair<string, ZeronGatePausedWorkType> pair in m_Paused)
            {
                try
                {
                    pair.Value.ResumeDecision = GateDecisionType.Cancel;
                    pair.Value.Reason = "Gate cleared.";
                    pair.Value.Signal.Set();
                }
                catch (Exception)
                {
                }
            }

            m_Paused.Clear();
        }

        /// <summary>
        /// WaitForResume
        /// </summary>
        /// <param name="context"></param>
        /// <param name="timeoutMs"></param>
        /// <returns>Returns GateDecisionType.</returns>
        private GateDecisionType WaitForResume(
            GateContextType context,
            int timeoutMs)
        {
            string correlationId = context.CorrelationId!;
            ZeronGatePausedWorkType work = new();

            if (!m_Paused.TryAdd(correlationId, work))
            {
                context.Decision = GateDecisionType.Cancel;
                context.Reason = "Gate correlationId is already paused: " + correlationId;
                work.Dispose();

                return GateDecisionType.Cancel;
            }

            try
            {
                bool signaled = work.Signal.Wait(timeoutMs);

                if (!signaled)
                {
                    context.Decision = GateDecisionType.Cancel;
                    context.Reason = string.Format(CultureInfo.InvariantCulture,
                        "Gate pause timed out after {0} ms.", timeoutMs);

                    return GateDecisionType.Cancel;
                }

                context.Decision = work.ResumeDecision;

                if (!string.IsNullOrWhiteSpace(work.Reason))
                {
                    context.Reason = work.Reason;
                }

                return context.Decision == GateDecisionType.Cancel
                    ? GateDecisionType.Cancel
                    : GateDecisionType.Proceed;
            }
            finally
            {
                m_Paused.TryRemove(correlationId, out _);
                work.Dispose();
            }
        }

        /// <summary>
        /// CompletePause
        /// </summary>
        /// <param name="correlationId"></param>
        /// <param name="decision"></param>
        /// <param name="reason"></param>
        /// <returns>Returns bool.</returns>
        private bool CompletePause(
            string correlationId,
            GateDecisionType decision,
            string? reason)
        {
            if (string.IsNullOrWhiteSpace(correlationId))
            {
                return false;
            }

            if (!m_Paused.TryGetValue(correlationId.Trim(), out ZeronGatePausedWorkType? work) || work == null)
            {
                return false;
            }

            work.ResumeDecision = decision;
            work.Reason = reason;
            work.Signal.Set();

            return true;
        }

        /// <summary>
        /// PublishCancelled
        /// </summary>
        /// <param name="context"></param>
        /// <returns>Returns void.</returns>
        private static void PublishCancelled(
            GateContextType context)
        {
            try
            {
                ZeronEventBus.PublishObject(
                    ZeronEventTopics.GateCancelled,
                    new
                    {
                        topic = context.Topic,
                        correlationId = context.CorrelationId,
                        reason = context.Reason
                    },
                    source: "gate",
                    correlationId: context.CorrelationId);
            }
            catch (Exception)
            {
            }
        }
    }
}
