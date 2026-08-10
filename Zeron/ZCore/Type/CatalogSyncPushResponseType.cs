// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.ZCore.Type
{
    /// <summary>
    /// CatalogSyncPushResponseType
    /// </summary>
    public class CatalogSyncPushResponseType
    {
        /// <summary>
        /// Success
        /// </summary>
        public bool Success
        {
            get;
            set;
        }

        /// <summary>
        /// Message
        /// </summary>
        public string? Message
        {
            get;
            set;
        }

        /// <summary>
        /// PushedCount
        /// </summary>
        public int PushedCount
        {
            get;
            set;
        }

        /// <summary>
        /// AgentKeys
        /// </summary>
        public List<string> AgentKeys
        {
            get;
            set;
        } = [];
    }
}
