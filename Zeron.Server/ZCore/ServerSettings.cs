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

        /// <summary>
        /// JwtSecret
        /// </summary>
        public string JwtSecret
        {
            get;
            set;
        } = "zeron-dev-secret-change-in-production";

        /// <summary>
        /// JwtIssuer
        /// </summary>
        public string JwtIssuer
        {
            get;
            set;
        } = "Zeron.Server";

        /// <summary>
        /// JwtExpireMinutes
        /// </summary>
        public int JwtExpireMinutes
        {
            get;
            set;
        } = 480;

        /// <summary>
        /// DefaultAdminUsername
        /// </summary>
        public string DefaultAdminUsername
        {
            get;
            set;
        } = "admin";

        /// <summary>
        /// DefaultAdminPassword
        /// </summary>
        public string DefaultAdminPassword
        {
            get;
            set;
        } = "admin";

        /// <summary>
        /// AlertEmailEnabled
        /// </summary>
        public bool AlertEmailEnabled
        {
            get;
            set;
        }

        /// <summary>
        /// AlertEmailTo
        /// </summary>
        public string? AlertEmailTo
        {
            get;
            set;
        }

        /// <summary>
        /// SmtpHost
        /// </summary>
        public string? SmtpHost
        {
            get;
            set;
        }

        /// <summary>
        /// SmtpPort
        /// </summary>
        public int SmtpPort
        {
            get;
            set;
        } = 587;

        /// <summary>
        /// SmtpUser
        /// </summary>
        public string? SmtpUser
        {
            get;
            set;
        }

        /// <summary>
        /// SmtpPassword
        /// </summary>
        public string? SmtpPassword
        {
            get;
            set;
        }

        /// <summary>
        /// SmtpFrom
        /// </summary>
        public string? SmtpFrom
        {
            get;
            set;
        }

        /// <summary>
        /// SmtpEnableSsl
        /// </summary>
        public bool SmtpEnableSsl
        {
            get;
            set;
        } = true;
    }
}
