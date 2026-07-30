// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.Server.ZCore
{
    /// <summary>
    /// ServerSettings
    /// </summary>
    public class ServerSettings
    {
        /// <summary>
        /// SectionName
        /// </summary>
        public const string SectionName = "Zeron";

        /// <summary>
        /// DatabasePath
        /// </summary>
        public string DatabasePath
        {
            get;
            set;
        } = "Data/zeron-server.db";

        /// <summary>
        /// CommandPubAddr
        /// </summary>
        public string CommandPubAddr
        {
            get;
            set;
        } = "tcp://*:6000";

        /// <summary>
        /// AgentApiKey
        /// </summary>
        public string AgentApiKey
        {
            get;
            set;
        } = "zeron.testkey";

        /// <summary>
        /// HeartbeatTimeoutSeconds
        /// </summary>
        public int HeartbeatTimeoutSeconds
        {
            get;
            set;
        } = 90;

        /// <summary>
        /// DispatchIntervalMs
        /// </summary>
        public int DispatchIntervalMs
        {
            get;
            set;
        } = 5000;
    }
}
