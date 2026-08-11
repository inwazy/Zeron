// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Zeron.ZCore.Utils;
using Zeron.ZServers;

namespace Zeron.ZCore
{
    /// <summary>
    /// InstallEventPublisher - optional callback wired by the host to broadcast events.
    /// Automatically enriches JSON payloads with agentId and timestamp.
    /// Dual-writes to ZeronEventBus for in-process observers.
    /// </summary>
    public static class InstallEventPublisher
    {
        /// <summary>
        /// PublishHandler
        /// </summary>
        public static Action<string, string>? PublishHandler
        {
            get;
            set;
        }

        /// <summary>
        /// Publish
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="message"></param>
        /// <returns>Returns void.</returns>
        public static void Publish(
            string topic, 
            string message)
        {
            string enriched = EnrichMessage(message);

            try
            {
                ZeronEventBus.Current.Publish(topic, enriched, source: "agent");
            }
            catch (Exception)
            {
            }

            PublishHandler?.Invoke(topic, enriched);
        }

        /// <summary>
        /// PublishObject
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="payload"></param>
        /// <returns>Returns void.</returns>
        public static void PublishObject(
            string topic, 
            object payload)
        {
            Publish(topic, JsonSerializer.Serialize(payload));
        }

        /// <summary>
        /// EnrichMessage
        /// </summary>
        /// <param name="message"></param>
        /// <returns>Returns enriched JSON string.</returns>
        public static string EnrichMessage(
            string message)
        {
            try
            {
                JsonNode? node = JsonNode.Parse(message);

                if (node is JsonObject obj)
                {
                    if (AgentServer.AgentId != null)
                    {
                        obj["agentId"] = AgentServer.AgentId;
                    }

                    obj["timestamp"] = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);

                    return obj.ToJsonString();
                }
            }
            catch (JsonException)
            {
            }

            return JsonSerializer.Serialize(new
            {
                agentId = AgentServer.AgentId,
                timestamp = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
                payload = message
            });
        }
    }
}
