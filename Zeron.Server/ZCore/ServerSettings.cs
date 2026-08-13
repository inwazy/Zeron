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
        /// CurveEnabled
        /// </summary>
        public bool CurveEnabled
        {
            get;
            set;
        }

        /// <summary>
        /// CurveSecretKeyPath
        /// </summary>
        public string CurveSecretKeyPath
        {
            get;
            set;
        } = "Data/curve-server.secret";

        /// <summary>
        /// CurvePublicKeyPath
        /// </summary>
        public string CurvePublicKeyPath
        {
            get;
            set;
        } = "Data/curve-server.public";

        /// <summary>
        /// AgentHmacRequired
        /// </summary>
        public bool AgentHmacRequired
        {
            get;
            set;
        }

        /// <summary>
        /// AgentHmacSkewSeconds
        /// </summary>
        public int AgentHmacSkewSeconds
        {
            get;
            set;
        } = 300;

        /// <summary>
        /// RequireHttpsAgents
        /// </summary>
        public bool RequireHttpsAgents
        {
            get;
            set;
        }

        /// <summary>
        /// HeartbeatTimeoutSeconds
        /// </summary>
        public int HeartbeatTimeoutSeconds
        {
            get;
            set;
        } = 90;

        /// <summary>
        /// CatalogSyncStaleMinutes - online agents without sync within this window are stale.
        /// </summary>
        public int CatalogSyncStaleMinutes
        {
            get;
            set;
        } = 15;

        /// <summary>
        /// PublishAgentHeartbeatEvents - emit agent.heartbeat on the in-process event bus (noisy).
        /// </summary>
        public bool PublishAgentHeartbeatEvents
        {
            get;
            set;
        }

        /// <summary>
        /// GatePauseTimeoutMs - Server dispatch gate pause timeout (short by default).
        /// </summary>
        public int GatePauseTimeoutMs
        {
            get;
            set;
        } = 2000;

        /// <summary>
        /// DispatchIntervalMs
        /// </summary>
        public int DispatchIntervalMs
        {
            get;
            set;
        } = 5000;

        /// <summary>
        /// ScheduleIntervalMs
        /// </summary>
        public int ScheduleIntervalMs
        {
            get;
            set;
        } = 15000;

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
        /// InstallResultNotifyEnabled - create DeviceOwner dashboard tips for self-service install results.
        /// </summary>
        public bool InstallResultNotifyEnabled
        {
            get;
            set;
        } = true;

        /// <summary>
        /// InstallResultEmailEnabled - email bound users (with Email) on self-service install results.
        /// </summary>
        public bool InstallResultEmailEnabled
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

        /// <summary>
        /// EncryptionSaltKey - AES salt for agent API key obfuscation (NetMQ payloads).
        /// </summary>
        public string? EncryptionSaltKey
        {
            get;
            set;
        }

        /// <summary>
        /// EncryptionIvKey - AES IV source for agent API key obfuscation.
        /// </summary>
        public string? EncryptionIvKey
        {
            get;
            set;
        }

        /// <summary>
        /// WindowsServiceName - SCM service name when hosted as a Windows Service.
        /// </summary>
        public string WindowsServiceName
        {
            get;
            set;
        } = "Zeron.Server";
    }
}
