// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.ZCore.Type
{
    /// <summary>
    /// ZeronEventType - in-process event bus payload.
    /// </summary>
    public sealed class ZeronEventType
    {
        /// <summary>
        /// Topic - stable topic string (e.g. package.catalog.sync).
        /// </summary>
        public string Topic
        {
            get;
            set;
        } = "";

        /// <summary>
        /// Source - publisher label (server, agent, script-host, …).
        /// </summary>
        public string? Source
        {
            get;
            set;
        }

        /// <summary>
        /// PayloadJson
        /// </summary>
        public string? PayloadJson
        {
            get;
            set;
        }

        /// <summary>
        /// CorrelationId
        /// </summary>
        public string? CorrelationId
        {
            get;
            set;
        }

        /// <summary>
        /// TimestampUtc
        /// </summary>
        public DateTime TimestampUtc
        {
            get;
            set;
        } = DateTime.UtcNow;
    }
}
