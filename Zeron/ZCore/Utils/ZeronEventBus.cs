// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using Zeron.ZCore.Type;
using Zeron.ZInterfaces;

namespace Zeron.ZCore.Utils
{
    /// <summary>
    /// ZeronEventBus - thread-safe in-process event bus.
    /// </summary>
    public sealed class ZeronEventBus : IZeronEventBus
    {
        // Process-wide bus instance.
        private static readonly ZeronEventBus s_Instance = new();

        // Subscriptions.
        private readonly ConcurrentDictionary<Guid, Subscription> m_Subscriptions = new();

        /// <summary>
        /// Current
        /// </summary>
        public static IZeronEventBus Current => s_Instance;

        /// <summary>
        /// Subscribe
        /// </summary>
        /// <param name="topicFilter"></param>
        /// <param name="handler"></param>
        /// <returns>Returns disposable subscription.</returns>
        public IDisposable Subscribe(
            string? topicFilter,
            Action<ZeronEvent> handler)
        {
            ArgumentNullException.ThrowIfNull(handler);

            Guid id = Guid.NewGuid();
            Subscription subscription = new(id, NormalizeFilter(topicFilter), handler, this);
            m_Subscriptions[id] = subscription;

            return subscription;
        }

        /// <summary>
        /// Publish
        /// </summary>
        /// <param name="zeronEvent"></param>
        /// <returns>Returns void.</returns>
        public void Publish(
            ZeronEvent zeronEvent)
        {
            ArgumentNullException.ThrowIfNull(zeronEvent);

            if (string.IsNullOrWhiteSpace(zeronEvent.Topic))
            {
                return;
            }

            zeronEvent.Topic = zeronEvent.Topic.Trim();

            if (zeronEvent.TimestampUtc == default)
            {
                zeronEvent.TimestampUtc = DateTime.UtcNow;
            }

            foreach (Subscription subscription in m_Subscriptions.Values)
            {
                if (!Matches(subscription.TopicFilter, zeronEvent.Topic))
                {
                    continue;
                }

                try
                {
                    subscription.Handler(zeronEvent);
                }
                catch (Exception e)
                {
                    ZNLogger.Common.Warn(string.Format(CultureInfo.InvariantCulture,
                        "ZeronEventBus handler error topic={0}: {1}", zeronEvent.Topic, e.Message));
                }
            }
        }

        /// <summary>
        /// Publish
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="payloadJson"></param>
        /// <param name="source"></param>
        /// <param name="correlationId"></param>
        /// <returns>Returns void.</returns>
        public void Publish(
            string topic,
            string? payloadJson = null,
            string? source = null,
            string? correlationId = null)
        {
            Publish(new ZeronEvent
            {
                Topic = topic,
                PayloadJson = payloadJson,
                Source = source,
                CorrelationId = correlationId,
                TimestampUtc = DateTime.UtcNow
            });
        }

        /// <summary>
        /// PublishObject
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="payload"></param>
        /// <param name="source"></param>
        /// <param name="correlationId"></param>
        /// <returns>Returns void.</returns>
        public static void PublishObject(
            string topic,
            object payload,
            string? source = null,
            string? correlationId = null)
        {
            Current.Publish(
                topic,
                JsonSerializer.Serialize(payload),
                source,
                correlationId);
        }

        /// <summary>
        /// Clear
        /// </summary>
        /// <returns>Returns void.</returns>
        public void Clear()
        {
            m_Subscriptions.Clear();
        }

        /// <summary>
        /// Unsubscribe
        /// </summary>
        /// <param name="id"></param>
        /// <returns>Returns void.</returns>
        private void Unsubscribe(
            Guid id)
        {
            m_Subscriptions.TryRemove(id, out _);
        }

        /// <summary>
        /// NormalizeFilter
        /// </summary>
        private static string NormalizeFilter(
            string? topicFilter)
        {
            if (string.IsNullOrWhiteSpace(topicFilter) || topicFilter.Trim() == "*")
            {
                return "*";
            }

            return topicFilter.Trim();
        }

        /// <summary>
        /// Matches
        /// </summary>
        public static bool Matches(
            string filter,
            string topic)
        {
            if (filter == "*" || string.IsNullOrWhiteSpace(filter))
            {
                return true;
            }

            if (filter.EndsWith(".*", StringComparison.Ordinal))
            {
                string prefix = filter[..^1]; // keep trailing '.'

                return topic.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
            }

            return string.Equals(filter, topic, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Subscription
        /// </summary>
        private sealed class Subscription : IDisposable
        {
            // Bus instance.
            private readonly ZeronEventBus m_Bus;

            // Subscription ID.
            private readonly Guid m_Id;

            // Disposed flag.
            private bool m_Disposed;

            /// <summary>
            /// Subscription
            /// </summary>
            /// <param name="id"></param>
            /// <param name="topicFilter"></param>
            /// <param name="handler"></param>
            /// <param name="bus"></param>
            /// <returns>Returns void.</returns>
            public Subscription(
                Guid id,
                string topicFilter,
                Action<ZeronEvent> handler,
                ZeronEventBus bus)
            {
                m_Id = id;
                TopicFilter = topicFilter;
                Handler = handler;
                m_Bus = bus;
            }

            /// <summary>
            /// TopicFilter
            /// </summary>
            public string TopicFilter
            {
                get;
            }

            /// <summary>
            /// Handler
            /// </summary>
            public Action<ZeronEvent> Handler
            {
                get;
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
                m_Bus.Unsubscribe(m_Id);
            }
        }
    }
}
