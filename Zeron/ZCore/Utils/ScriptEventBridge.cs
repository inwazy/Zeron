// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Zeron.ZCore.Type;

namespace Zeron.ZCore.Utils
{
    /// <summary>
    /// ScriptEventBridge - NDJSON observe bridge for a long-running script listener.
    /// Scripts may pause_self / resume_self only; gate/sync control is rejected.
    /// </summary>
    public sealed class ScriptEventBridge : IDisposable
    {
        // Pending events while pause_self is active.
        private readonly ConcurrentQueue<ZeronEventType> m_Queue = new();

        // Sync for process IO.
        private readonly object m_IoLock = new();

        // Bus subscription.
        private IDisposable? m_Subscription;

        // Listener process.
        private Process? m_Process;

        // Stdout reader.
        private StreamReader? m_StdoutReader;

        // Stdin writer.
        private StreamWriter? m_StdinWriter;

        // Reader cancellation.
        private CancellationTokenSource? m_ReaderCts;

        // Reader task.
        private Task? m_ReaderTask;

        // Restart loop cancellation.
        private CancellationTokenSource? m_LifetimeCts;

        // Restart loop task.
        private Task? m_LifetimeTask;

        // Pause self flag.
        private volatile bool m_Paused;

        // Disposed.
        private bool m_Disposed;

        /// <summary>
        /// Enabled
        /// </summary>
        public bool Enabled
        {
            get;
            private set;
        }

        /// <summary>
        /// ExecutablePath
        /// </summary>
        public string ExecutablePath
        {
            get;
            private set;
        } = "";

        /// <summary>
        /// Arguments
        /// </summary>
        public string Arguments
        {
            get;
            private set;
        } = "";

        /// <summary>
        /// RestartDelayMs
        /// </summary>
        public int RestartDelayMs
        {
            get;
            private set;
        } = 3000;

        /// <summary>
        /// IsPaused
        /// </summary>
        public bool IsPaused => m_Paused;

        /// <summary>
        /// QueuedCount - test helper.
        /// </summary>
        public int QueuedCount => m_Queue.Count;

        /// <summary>
        /// Configure
        /// </summary>
        /// <param name="enabled"></param>
        /// <param name="executablePath"></param>
        /// <param name="arguments"></param>
        /// <param name="restartDelayMs"></param>
        /// <returns>Returns void.</returns>
        public void Configure(
            bool enabled,
            string? executablePath,
            string? arguments,
            int restartDelayMs = 3000)
        {
            Enabled = enabled;
            ExecutablePath = executablePath?.Trim() ?? "";
            Arguments = arguments?.Trim() ?? "";
            RestartDelayMs = restartDelayMs > 0 ? restartDelayMs : 3000;
        }

        /// <summary>
        /// Start - subscribe to bus and optionally run listener process when enabled.
        /// </summary>
        /// <param name="launchListener">When false, only subscribe (tests / observe without process).</param>
        /// <returns>Returns void.</returns>
        public void Start(
            bool launchListener = true)
        {
            if (m_Disposed)
            {
                return;
            }

            m_Subscription ??= ZeronEventBus.Current.Subscribe("*", OnBusEvent);

            if (!launchListener || !Enabled || string.IsNullOrWhiteSpace(ExecutablePath))
            {
                return;
            }

            m_LifetimeCts = new CancellationTokenSource();
            m_LifetimeTask = Task.Run(() => RunSupervisorAsync(m_LifetimeCts.Token));
        }

        /// <summary>
        /// Stop
        /// </summary>
        /// <returns>Returns void.</returns>
        public void Stop()
        {
            try
            {
                m_LifetimeCts?.Cancel();
                StopProcess();
                m_Subscription?.Dispose();
                m_Subscription = null;
            }
            catch (Exception e)
            {
                ZNLogger.Common.Warn("ScriptEventBridge Stop: " + e.Message);
            }
        }

        /// <summary>
        /// Dispose
        /// </summary>
        /// <returns>Returns void.</returns>
        public void Dispose()
        {
            if (m_Disposed)
            {
                return;
            }

            m_Disposed = true;
            Stop();
            m_LifetimeCts?.Dispose();
            m_ReaderCts?.Dispose();
        }

        /// <summary>
        /// ProcessControlLine - parses listener stdout control JSON (testable).
        /// </summary>
        /// <param name="line"></param>
        /// <returns>Returns response line to write back (may be null).</returns>
        public string? ProcessControlLine(
            string? line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return null;
            }

            try
            {
                using JsonDocument document = JsonDocument.Parse(line);
                JsonElement root = document.RootElement;

                if (!root.TryGetProperty("type", out JsonElement typeElement))
                {
                    return null;
                }

                string? type = typeElement.GetString()?.Trim().ToLowerInvariant();

                switch (type)
                {
                    case "ack":
                        return null;

                    case "pause_self":
                        m_Paused = true;
                        return null;

                    case "resume_self":
                        m_Paused = false;
                        FlushQueue();
                        return null;

                    case "cancel":
                    case "pause_gate":
                    case "resume_gate":
                    case "pause_sync":
                    case "cancel_sync":
                        return JsonSerializer.Serialize(new
                        {
                            type = "error",
                            code = "not_allowed",
                            message = "Script listeners cannot control gates or sync."
                        });

                    default:
                        return JsonSerializer.Serialize(new
                        {
                            type = "error",
                            code = "unknown_type",
                            message = "Unsupported control type: " + type
                        });
                }
            }
            catch (JsonException)
            {
                return null;
            }
        }

        /// <summary>
        /// OnBusEvent
        /// </summary>
        /// <param name="zeronEvent"></param>
        /// <returns>Returns void.</returns>
        private void OnBusEvent(
            ZeronEventType zeronEvent)
        {
            if (!Enabled || string.IsNullOrWhiteSpace(ExecutablePath))
            {
                return;
            }

            if (m_Paused || m_Process == null || m_Process.HasExited)
            {
                m_Queue.Enqueue(zeronEvent);

                return;
            }

            WriteEvent(zeronEvent);
        }

        /// <summary>
        /// FlushQueue
        /// </summary>
        /// <returns>Returns void.</returns>
        private void FlushQueue()
        {
            if (m_Paused)
            {
                return;
            }

            lock (m_IoLock)
            {
                if (m_StdinWriter == null)
                {
                    return;
                }
            }

            while (!m_Paused && m_Queue.TryDequeue(out ZeronEventType? queued))
            {
                if (queued == null)
                {
                    continue;
                }

                if (!TryWriteEvent(queued))
                {
                    m_Queue.Enqueue(queued);
                    break;
                }
            }
        }

        /// <summary>
        /// WriteEvent
        /// </summary>
        /// <param name="zeronEvent"></param>
        /// <returns>Returns void.</returns>
        private void WriteEvent(
            ZeronEventType zeronEvent)
        {
            if (!TryWriteEvent(zeronEvent))
            {
                m_Queue.Enqueue(zeronEvent);
            }
        }

        /// <summary>
        /// TryWriteEvent
        /// </summary>
        /// <param name="zeronEvent"></param>
        /// <returns>Returns true when delivered to listener stdin.</returns>
        private bool TryWriteEvent(
            ZeronEventType zeronEvent)
        {
            string line = JsonSerializer.Serialize(new
            {
                type = "event",
                topic = zeronEvent.Topic,
                correlationId = zeronEvent.CorrelationId,
                source = zeronEvent.Source,
                timestamp = zeronEvent.TimestampUtc.ToString("o", CultureInfo.InvariantCulture),
                payload = TryParsePayload(zeronEvent.PayloadJson)
            });

            lock (m_IoLock)
            {
                try
                {
                    if (m_StdinWriter == null)
                    {
                        return false;
                    }

                    m_StdinWriter.WriteLine(line);
                    m_StdinWriter.Flush();

                    return true;
                }
                catch (Exception e)
                {
                    ZNLogger.Common.Warn(string.Format(CultureInfo.InvariantCulture,
                        "ScriptEventBridge write failed: {0}", e.Message));

                    return false;
                }
            }
        }

        /// <summary>
        /// TryParsePayload
        /// </summary>
        /// <param name="payloadJson"></param>
        /// <returns>Returns object.</returns>
        private static object? TryParsePayload(
            string? payloadJson)
        {
            if (string.IsNullOrWhiteSpace(payloadJson))
            {
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<JsonElement>(payloadJson);
            }
            catch (JsonException)
            {
                return payloadJson;
            }
        }

        /// <summary>
        /// RunSupervisorAsync
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns void.</returns>
        private async Task RunSupervisorAsync(
            CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    StartProcess();
                    FlushQueue();

                    if (m_ReaderTask != null)
                    {
                        await m_ReaderTask.ConfigureAwait(false);
                    }
                }
                catch (Exception e) when (!cancellationToken.IsCancellationRequested)
                {
                    ZNLogger.Common.Warn(string.Format(CultureInfo.InvariantCulture,
                        "ScriptEventBridge listener error: {0}", e.Message));
                }

                StopProcess();

                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                try
                {
                    await Task.Delay(RestartDelayMs, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// StartProcess
        /// </summary>
        /// <returns>Returns void.</returns>
        private void StartProcess()
        {
            StopProcess();

            ProcessStartInfo startInfo = new()
            {
                FileName = ExecutablePath,
                Arguments = Arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardInputEncoding = Encoding.UTF8
            };

            Process process = new() { StartInfo = startInfo, EnableRaisingEvents = true };

            if (!process.Start())
            {
                throw new InvalidOperationException("Failed to start script event listener: " + ExecutablePath);
            }

            m_Process = process;
            m_StdinWriter = process.StandardInput;
            m_StdoutReader = process.StandardOutput;
            m_ReaderCts = new CancellationTokenSource();
            m_ReaderTask = Task.Run(() => ReadStdoutLoop(m_ReaderCts.Token));

            ZNLogger.Common.Info(string.Format(CultureInfo.InvariantCulture,
                "ScriptEventBridge listener started: {0} {1}", ExecutablePath, Arguments));
        }

        /// <summary>
        /// StopProcess
        /// </summary>
        /// <returns>Returns void.</returns>
        private void StopProcess()
        {
            try
            {
                m_ReaderCts?.Cancel();
            }
            catch (Exception)
            {
            }

            lock (m_IoLock)
            {
                try
                {
                    m_StdinWriter?.Dispose();
                }
                catch (Exception)
                {
                }

                m_StdinWriter = null;
            }

            if (m_Process != null)
            {
                try
                {
                    if (!m_Process.HasExited)
                    {
                        m_Process.Kill(entireProcessTree: true);
                    }
                }
                catch (Exception)
                {
                }

                try
                {
                    m_Process.Dispose();
                }
                catch (Exception)
                {
                }

                m_Process = null;
            }

            try
            {
                m_StdoutReader?.Dispose();
            }
            catch (Exception)
            {
            }

            m_StdoutReader = null;
            m_ReaderCts?.Dispose();
            m_ReaderCts = null;
            m_ReaderTask = null;
        }

        /// <summary>
        /// ReadStdoutLoop
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns void.</returns>
        private void ReadStdoutLoop(
            CancellationToken cancellationToken)
        {
            StreamReader? reader = m_StdoutReader;

            if (reader == null)
            {
                return;
            }

            try
            {
                while (!cancellationToken.IsCancellationRequested && !reader.EndOfStream)
                {
                    string? line = reader.ReadLine();

                    if (line == null)
                    {
                        break;
                    }

                    string? response = ProcessControlLine(line);

                    if (response != null)
                    {
                        lock (m_IoLock)
                        {
                            m_StdinWriter?.WriteLine(response);
                            m_StdinWriter?.Flush();
                        }
                    }
                }
            }
            catch (Exception e) when (!cancellationToken.IsCancellationRequested)
            {
                ZNLogger.Common.Warn("ScriptEventBridge stdout reader: " + e.Message);
            }
        }
    }
}
