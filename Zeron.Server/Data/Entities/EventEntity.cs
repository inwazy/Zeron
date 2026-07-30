// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.Server.Data.Entities
{
    /// <summary>
    /// EventEntity
    /// </summary>
    public class EventEntity
    {
        /// <summary>
        /// Id
        /// </summary>
        public long Id
        {
            get;
            set;
        }

        /// <summary>
        /// AgentId
        /// </summary>
        public Guid AgentId
        {
            get;
            set;
        }

        /// <summary>
        /// Topic
        /// </summary>
        public string Topic
        {
            get;
            set;
        } = "";

        /// <summary>
        /// Payload
        /// </summary>
        public string Payload
        {
            get;
            set;
        } = "";

        /// <summary>
        /// ReceivedAt
        /// </summary>
        public DateTime ReceivedAt
        {
            get;
            set;
        }

        /// <summary>
        /// Agent
        /// </summary>  
        public AgentEntity? Agent
        {
            get;
            set;
        }
    }
}
