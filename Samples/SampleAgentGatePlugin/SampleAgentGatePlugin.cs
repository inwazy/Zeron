// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using System.Globalization;
using Zeron.ZCore;
using Zeron.ZCore.Type;
using Zeron.ZInterfaces;

namespace SampleAgentGatePlugin
{
    /// <summary>
    /// SampleAgentGatePlugin - observe install.* and intercept gate.install.
    /// Drop SampleAgentGatePlugin.dll into Demand script_plugins_dir (do not name it Zeron.*.dll).
    /// </summary>
    public sealed class SampleAgentGatePlugin : IZeronAgentPlugin, IGateHandler
    {
        // Optional fixed options (tests); otherwise loaded per Handle().
        private readonly SampleAgentGateOptions? m_FixedOptions;

        // Event bus subscription.
        private IDisposable? m_InstallSubscription;

        // Cancelled-topic subscription.
        private IDisposable? m_CancelledSubscription;

        // Gate controller from Initialize.
        private IGateController? m_Gate;

        // Cancel auto-resume when plugin stops.
        private CancellationTokenSource? m_Lifetime;

        /// <summary>
        /// SampleAgentGatePlugin
        /// </summary>
        public SampleAgentGatePlugin()
        {
        }

        /// <summary>
        /// SampleAgentGatePlugin - test constructor.
        /// </summary>
        /// <param name="options"></param>
        public SampleAgentGatePlugin(
            SampleAgentGateOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            m_FixedOptions = options;
        }

        /// <summary>
        /// Id
        /// </summary>
        public string Id => "sample-agent-gate";

        /// <summary>
        /// Initialize
        /// </summary>
        /// <param name="bus"></param>
        /// <param name="gate"></param>
        /// <returns>Returns void.</returns>
        public void Initialize(
            IZeronEventBus bus,
            IGateController gate)
        {
            ArgumentNullException.ThrowIfNull(bus);
            ArgumentNullException.ThrowIfNull(gate);

            Stop();
            m_Gate = gate;
            m_Lifetime = new CancellationTokenSource();
            m_InstallSubscription = bus.Subscribe("install.*", OnBusEvent);
            m_CancelledSubscription = bus.Subscribe(ZeronEventTopics.GateCancelled, OnBusEvent);
            gate.Register(this);

            SampleAgentGateOptions options = GetOptions();
            ZNLogger.Common.Info(string.Format(CultureInfo.InvariantCulture,
                "SampleAgentGatePlugin ready. mode={0} delayMs={1} package={2}",
                options.Mode,
                options.DelayMs,
                options.PackageFilter ?? "*"));
        }

        /// <summary>
        /// Stop
        /// </summary>
        /// <returns>Returns void.</returns>
        public void Stop()
        {
            try
            {
                m_Lifetime?.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }

            m_Lifetime?.Dispose();
            m_Lifetime = null;

            m_InstallSubscription?.Dispose();
            m_InstallSubscription = null;
            m_CancelledSubscription?.Dispose();
            m_CancelledSubscription = null;

            if (m_Gate != null)
            {
                m_Gate.Unregister(this);
                m_Gate = null;
            }
        }

        /// <summary>
        /// Handle
        /// </summary>
        /// <param name="context"></param>
        /// <returns>Returns void.</returns>
        public void Handle(
            GateContextType context)
        {
            ArgumentNullException.ThrowIfNull(context);

            if (!string.Equals(context.Topic, ZeronEventTopics.GateInstall, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            SampleAgentGateOptions options = GetOptions();

            if (!options.MatchesPackage(context.PayloadJson))
            {
                return;
            }

            if (options.Mode == SampleAgentGateOptions.ModeCancel)
            {
                context.Decision = GateDecisionType.Cancel;
                context.Reason = "SampleAgentGatePlugin cancel";
                ZNLogger.Common.Warn(string.Format(CultureInfo.InvariantCulture,
                    "SampleAgentGatePlugin cancelling install correlationId={0}", context.CorrelationId));

                return;
            }

            if (options.Mode != SampleAgentGateOptions.ModePauseResume)
            {
                return;
            }

            context.Decision = GateDecisionType.Pause;
            context.Reason = "SampleAgentGatePlugin pause-resume";
            ZNLogger.Common.Info(string.Format(CultureInfo.InvariantCulture,
                "SampleAgentGatePlugin pausing install correlationId={0} delayMs={1}",
                context.CorrelationId,
                options.DelayMs));

            ScheduleAutoResume(context.CorrelationId, options.DelayMs);
        }

        /// <summary>
        /// GetOptions
        /// </summary>
        /// <returns>Returns SampleAgentGateOptions.</returns>
        private SampleAgentGateOptions GetOptions()
        {
            return m_FixedOptions ?? SampleAgentGateOptions.Load();
        }

        /// <summary>
        /// OnBusEvent
        /// </summary>
        /// <param name="zeronEvent"></param>
        /// <returns>Returns void.</returns>
        private static void OnBusEvent(
            ZeronEventType zeronEvent)
        {
            ZNLogger.Common.Info(string.Format(CultureInfo.InvariantCulture,
                "SampleAgentGatePlugin observed {0} correlationId={1} payload={2}",
                zeronEvent.Topic,
                zeronEvent.CorrelationId,
                zeronEvent.PayloadJson));
        }

        /// <summary>
        /// ScheduleAutoResume
        /// </summary>
        /// <param name="correlationId"></param>
        /// <param name="delayMs"></param>
        /// <returns>Returns void.</returns>
        private void ScheduleAutoResume(
            string? correlationId,
            int delayMs)
        {
            if (string.IsNullOrWhiteSpace(correlationId) || m_Gate == null)
            {
                return;
            }

            IGateController gate = m_Gate;
            string id = correlationId.Trim();
            CancellationToken token = m_Lifetime?.Token ?? CancellationToken.None;

            _ = Task.Run(async () =>
            {
                try
                {
                    if (delayMs > 0)
                    {
                        await Task.Delay(delayMs, token).ConfigureAwait(false);
                    }

                    DateTime deadline = DateTime.UtcNow.AddSeconds(15);

                    while (!token.IsCancellationRequested && DateTime.UtcNow < deadline)
                    {
                        if (gate.Resume(id, "SampleAgentGatePlugin auto-resume"))
                        {
                            ZNLogger.Common.Info(string.Format(CultureInfo.InvariantCulture,
                                "SampleAgentGatePlugin resumed install correlationId={0}", id));

                            return;
                        }

                        await Task.Delay(50, token).ConfigureAwait(false);
                    }

                    if (!token.IsCancellationRequested)
                    {
                        gate.Cancel(id, "SampleAgentGatePlugin auto-resume timed out");
                    }
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception e)
                {
                    ZNLogger.Common.Warn("SampleAgentGatePlugin auto-resume: " + e.Message);
                }
            }, token);
        }
    }
}
