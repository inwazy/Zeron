// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Zeron.ZCore.Type;

namespace Zeron.ZInterfaces
{
    /// <summary>
    /// IZeronEventBus - in-process pub/sub for Server and Agent.
    /// </summary>
    public interface IZeronEventBus
    {
        /// <summary>
        /// Subscribe - topicFilter null/"*" = all; suffix ".*" = prefix match.
        /// </summary>
        /// <param name="topicFilter"></param>
        /// <param name="handler"></param>
        /// <returns>Returns disposable subscription.</returns>
        IDisposable Subscribe(
            string? topicFilter,
            Action<ZeronEvent> handler);

        /// <summary>
        /// Publish
        /// </summary>
        /// <param name="zeronEvent"></param>
        /// <returns>Returns void.</returns>
        void Publish(
            ZeronEvent zeronEvent);

        /// <summary>
        /// Publish - convenience overload.
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="payloadJson"></param>
        /// <param name="source"></param>
        /// <param name="correlationId"></param>
        /// <returns>Returns void.</returns>
        void Publish(
            string topic,
            string? payloadJson = null,
            string? source = null,
            string? correlationId = null);

        /// <summary>
        /// Clear - test helper.
        /// </summary>
        /// <returns>Returns void.</returns>
        void Clear();
    }
}
