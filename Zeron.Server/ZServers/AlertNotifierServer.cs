// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using System.Globalization;
using System.Net;
using System.Net.Mail;
using Zeron.Server.Data.Entities;
using Zeron.Server.ZCore;
using Zeron.Server.ZInterfaces;
using Zeron.ZCore;

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
            if (string.IsNullOrWhiteSpace(m_Settings.SmtpHost)
                || string.IsNullOrWhiteSpace(m_Settings.AlertEmailTo))
            {
                return;
            }

            try
            {
                using SmtpClient client = new(m_Settings.SmtpHost, m_Settings.SmtpPort)
                {
                    EnableSsl = m_Settings.SmtpEnableSsl
                };

                if (!string.IsNullOrWhiteSpace(m_Settings.SmtpUser))
                {
                    client.Credentials = new NetworkCredential(m_Settings.SmtpUser, m_Settings.SmtpPassword);
                }

                string fromAddress = m_Settings.SmtpFrom ?? "zeron@localhost";
                using MailMessage message = new(fromAddress, m_Settings.AlertEmailTo)
                {
                    Subject = "[Zeron Alert] " + alert.Title,
                    Body = alert.Message,
                    IsBodyHtml = false
                };

                await client.SendMailAsync(message);
                alert.NotifiedAt = DateTime.UtcNow;

                ZNLogger.Common.Info(string.Format(CultureInfo.InvariantCulture,
                    "AlertNotifierServer emailed alert {0} to {1}", alert.Id, m_Settings.AlertEmailTo));
            }
            catch (Exception e)
            {
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture,
                    "AlertNotifierServer TrySendEmailAsync Error:{0}\n{1}", e.Message, e.StackTrace));
            }
        }
    }
}
