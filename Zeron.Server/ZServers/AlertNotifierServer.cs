// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using System.Globalization;
using Zeron.Server.Data.Entities;
using Zeron.Server.ZCore;
using Zeron.Server.ZInterfaces;
using Zeron.ZCore;
using Zeron.ZCore.Type;
using Zeron.ZCore.Utils;

namespace Zeron.Server.ZServers
{
    /// <summary>
    /// AlertNotifierServer - dashboard and email notifications for alerts.
    /// </summary>
    public class AlertNotifierServer
    {
        // Settings
        private readonly ServerSettings m_Settings;

        // Dashboard Notifier
        private readonly IDashboardNotifier? m_DashboardNotifier;

        /// <summary>
        /// AlertNotifierServer
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="dashboardNotifier"></param>
        /// <returns>Returns void.</returns>
        public AlertNotifierServer(
            ServerSettings settings, 
            IDashboardNotifier? dashboardNotifier = null)
        {
            m_Settings = settings;
            m_DashboardNotifier = dashboardNotifier;
        }

        /// <summary>
        /// NotifyAsync
        /// </summary>
        /// <param name="alert"></param>
        /// <returns>Returns void.</returns>
        public async Task NotifyAsync(
            AlertEntity alert)
        {
            if (m_DashboardNotifier != null)
            {
                await m_DashboardNotifier.NotifyAlertAsync(alert);
            }

            if (m_Settings.AlertEmailEnabled)
            {
                await TrySendEmailAsync(alert);
            }
        }

        /// <summary>
        /// TrySendEmailAsync
        /// </summary>
        /// <param name="alert"></param>
        /// <returns>Returns void.</returns>
        public async Task TrySendEmailAsync(
            AlertEntity alert)
        {
            SmtpMailOptions options = new()
            {
                Host = m_Settings.SmtpHost,
                Port = m_Settings.SmtpPort,
                EnableSsl = m_Settings.SmtpEnableSsl,
                UserName = m_Settings.SmtpUser,
                Password = m_Settings.SmtpPassword,
                FromAddress = string.IsNullOrWhiteSpace(m_Settings.SmtpFrom)
                    ? "zeron@localhost"
                    : m_Settings.SmtpFrom,
                FromDisplayName = "Zeron"
            };

            if (!SmtpMailServer.HasConnection(options)
                || string.IsNullOrWhiteSpace(m_Settings.AlertEmailTo))
            {
                return;
            }

            (bool ok, Exception? error) = await SmtpMailServer.TrySendAsync(
                options,
                m_Settings.AlertEmailTo,
                "[Zeron Alert] " + alert.Title,
                alert.Message,
                isBodyHtml: false);

            if (ok)
            {
                alert.NotifiedAt = DateTime.UtcNow;
                ZNLogger.Common.Info(string.Format(CultureInfo.InvariantCulture,
                    "AlertNotifierServer emailed alert {0} to {1}", alert.Id, m_Settings.AlertEmailTo));

                return;
            }

            ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture,
                "AlertNotifierServer TrySendEmailAsync Error:{0}\n{1}",
                error?.Message,
                error?.StackTrace));
        }
    }
}
