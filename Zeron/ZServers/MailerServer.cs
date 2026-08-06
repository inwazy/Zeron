// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.Globalization;
using System.Net;
using System.Net.Mail;
using Zeron.ZCore;
using Zeron.ZCore.Container;
using Zeron.ZCore.Foundation;
using Zeron.ZInterfaces;

namespace Zeron.ZServers
{
    /// <summary>
    /// MailerServer - queued SMTP sender for agent-side notifications.
    /// </summary>
    public class MailerServer : ConfigurationTable, IServer
    {
        // SMTP client handle.
        private static SmtpClient? m_SmtpClient;

        // SMTP mail sender address handle.
        private static MailAddress? m_MailSender;

        // Email queue message.
        private static readonly ConcurrentQueue<Tuple<string, string>> m_MailQueueMessages = new();

        // Email queue enable running trigger.
        private static bool m_MailEnableRunning = true;

        // Email queue send signal.
        private static readonly Semaphore m_MailSendSignal = new(0, 20000);

        // Email send per milliseconds.
        private static readonly int m_DelayTimeToSend = 10;

        // Cached administrator recipients.
        private static readonly List<MailAddress> m_AdministratorRecipients = [];

        // Whether SMTP is ready.
        private static bool m_IsConfigured;

        /// <summary>
        /// Host
        /// </summary>
        public static string? Host
        {
            get;
            set;
        }

        /// <summary>
        /// Port
        /// </summary>
        public static int Port
        {
            get;
            set;
        } = 587;

        /// <summary>
        /// UserLogin
        /// </summary>
        public static string? UserLogin
        {
            get;
            set;
        }

        /// <summary>
        /// UserPassword
        /// </summary>
        public static string? UserPassword
        {
            get;
            set;
        }

        /// <summary>
        /// SenderName
        /// </summary>
        public static string? SenderName
        {
            get;
            set;
        }

        /// <summary>
        /// SenderAddress
        /// </summary>
        public static string? SenderAddress
        {
            get;
            set;
        }

        /// <summary>
        /// RecipientsAdministrator
        /// </summary>
        public static string? RecipientsAdministrator
        {
            get;
            set;
        }

        /// <summary>
        /// EnableSsl
        /// </summary>
        public static bool EnableSsl
        {
            get;
            set;
        } = true;

        /// <summary>
        /// Enabled
        /// </summary>
        public static bool Enabled
        {
            get;
            set;
        }

        /// <summary>
        /// IsConfigured
        /// </summary>
        public static bool IsConfigured => m_IsConfigured;

        /// <summary>
        /// LoadConfig
        /// </summary>
        /// <param name="aConfig"></param>
        /// <returns>Returns void.</returns>
        public override void LoadConfig(
            NameValueCollection aConfig)
        {
            if (aConfig == null)
            {
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "MailerServer Config Empty"));

                return;
            }

            try
            {
                Enabled = bool.Parse(aConfig["mail_enabled"] ?? "false");
                Host = aConfig["mail_host"];
                Port = int.TryParse(aConfig["mail_port"], NumberStyles.Integer, CultureInfo.InvariantCulture, out int port)
                    ? port
                    : 587;
                UserLogin = aConfig["mail_user_login"];
                UserPassword = aConfig["mail_user_password"];
                SenderName = aConfig["mail_sender_name"];
                SenderAddress = aConfig["mail_sender_address"];
                RecipientsAdministrator = aConfig["mail_recipients_administrator"];
                EnableSsl = bool.Parse(aConfig["mail_enable_ssl"] ?? "true");
            }
            catch (Exception e)
            {
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "MailerServer Config Error:{0}\n{1}", e.Message, e.StackTrace));
            }
        }

        /// <summary>
        /// Initialize
        /// </summary>
        /// <returns>Returns void.</returns>
        public void Initialize()
        {
            m_MailEnableRunning = true;
            m_IsConfigured = false;
            m_AdministratorRecipients.Clear();

            if (!Enabled || string.IsNullOrWhiteSpace(Host) || string.IsNullOrWhiteSpace(SenderAddress))
            {
                ZNLogger.Common.Info("MailerServer disabled or incomplete SMTP configuration.");
                StartQueueThread();

                return;
            }

            try
            {
                m_SmtpClient = new SmtpClient
                {
                    Host = Host,
                    Port = Port,
                    EnableSsl = EnableSsl,
                    DeliveryMethod = SmtpDeliveryMethod.Network
                };

                if (!string.IsNullOrWhiteSpace(UserLogin))
                {
                    m_SmtpClient.Credentials = new NetworkCredential(UserLogin, UserPassword);
                }

                m_MailSender = new MailAddress(SenderAddress, SenderName ?? "Zeron");
                ParseAdministratorRecipients(RecipientsAdministrator);
                m_IsConfigured = m_AdministratorRecipients.Count > 0;

                if (!m_IsConfigured)
                {
                    ZNLogger.Common.Warn("MailerServer has SMTP host but no valid administrator recipients.");
                }
                else
                {
                    ZNLogger.Common.Info(string.Format(CultureInfo.InvariantCulture,
                        "MailerServer ready. Host={0}:{1}, Recipients={2}",
                        Host,
                        Port,
                        m_AdministratorRecipients.Count));
                }
            }
            catch (Exception e)
            {
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture,
                    "MailerServer Initialize Error:{0}\n{1}", e.Message, e.StackTrace));
                m_IsConfigured = false;
            }

            StartQueueThread();
        }

        /// <summary>
        /// QueueMail - enqueue an email to administrator recipients.
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="bodyHtml"></param>
        /// <returns>Returns bool.</returns>
        public static bool QueueMail(
            string? subject,
            string? bodyHtml)
        {
            if (!m_IsConfigured
                || string.IsNullOrWhiteSpace(subject)
                || string.IsNullOrWhiteSpace(bodyHtml))
            {
                return false;
            }

            m_MailQueueMessages.Enqueue(Tuple.Create(subject.Trim(), bodyHtml));

            try
            {
                m_MailSendSignal.Release();
            }
            catch (SemaphoreFullException)
            {
            }

            return true;
        }

        /// <summary>
        /// SendMailNow - send immediately (used by tests / sync paths).
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="bodyHtml"></param>
        /// <returns>Returns bool.</returns>
        public static bool SendMailNow(
            string subject,
            string bodyHtml)
        {
            return SendQueuedMessage(subject, bodyHtml);
        }

        /// <summary>
        /// Stop
        /// </summary>
        /// <returns>Returns void.</returns>
        public void Stop()
        {
            m_MailEnableRunning = false;

            try
            {
                m_MailSendSignal.Release();
            }
            catch (SemaphoreFullException)
            {
            }

            try
            {
                m_SmtpClient?.Dispose();
                m_SmtpClient = null;
            }
            catch (Exception e)
            {
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "MailerServer Error:{0}\n{1}", e.Message, e.StackTrace));
            }

            ServerIntegrate.FinishSingleStop();
        }

        /// <summary>
        /// StartQueueThread
        /// </summary>
        /// <returns>Returns void.</returns>
        private static void StartQueueThread()
        {
            try
            {
                Thread threadQueue = new(QueuesProc)
                {
                    IsBackground = true,
                    Name = "MailerServer"
                };

                threadQueue.Start();
            }
            catch (Exception e)
            {
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture,
                    "MailerServer StartQueueThread Error:{0}\n{1}", e.Message, e.StackTrace));
            }
        }

        /// <summary>
        /// ParseAdministratorRecipients
        /// </summary>
        /// <param name="recipients"></param>
        /// <returns>Returns void.</returns>
        private static void ParseAdministratorRecipients(
            string? recipients)
        {
            m_AdministratorRecipients.Clear();

            if (string.IsNullOrWhiteSpace(recipients))
            {
                return;
            }

            foreach (string part in recipients.Split([';', '|', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                try
                {
                    m_AdministratorRecipients.Add(new MailAddress(part));
                }
                catch (FormatException)
                {
                    ZNLogger.Common.Warn(string.Format(CultureInfo.InvariantCulture,
                        "MailerServer skipping invalid recipient '{0}'", part));
                }
            }
        }

        /// <summary>
        /// QueuesProc
        /// </summary>
        /// <param name="aArg"></param>
        /// <returns>Returns void.</returns>
        private static void QueuesProc(
            object? aArg)
        {
            while (m_MailEnableRunning)
            {
                try
                {
                    m_MailSendSignal.WaitOne();

                    if (!m_MailEnableRunning)
                    {
                        break;
                    }

                    if (!m_MailQueueMessages.TryDequeue(out Tuple<string, string>? item) || item == null)
                    {
                        continue;
                    }

                    string emailSubject = item.Item1;
                    string emailMessage = item.Item2;

                    if (string.IsNullOrEmpty(emailSubject) || string.IsNullOrEmpty(emailMessage))
                    {
                        continue;
                    }

                    SendQueuedMessage(emailSubject, emailMessage);
                    Thread.Sleep(m_DelayTimeToSend);
                }
                catch (ObjectDisposedException e)
                {
                    ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "MailerServer QueuesProc ObjectDisposedException:{0}\n{1}", e.Message, e.StackTrace));
                }
                catch (AbandonedMutexException e)
                {
                    ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "MailerServer QueuesProc AbandonedMutexException:{0}\n{1}", e.Message, e.StackTrace));
                }
                catch (InvalidOperationException e)
                {
                    ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "MailerServer QueuesProc InvalidOperationException:{0}\n{1}", e.Message, e.StackTrace));
                }
            }
        }

        /// <summary>
        /// SendQueuedMessage
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="bodyHtml"></param>
        /// <returns>Returns bool.</returns>
        private static bool SendQueuedMessage(
            string subject,
            string bodyHtml)
        {
            if (!m_IsConfigured || m_SmtpClient == null || m_MailSender == null)
            {
                return false;
            }

            try
            {
                using MailMessage message = new()
                {
                    SubjectEncoding = System.Text.Encoding.UTF8,
                    BodyEncoding = System.Text.Encoding.UTF8,
                    Sender = m_MailSender,
                    From = m_MailSender,
                    IsBodyHtml = true,
                    Subject = subject,
                    Body = bodyHtml
                };

                foreach (MailAddress recipient in m_AdministratorRecipients)
                {
                    message.To.Add(recipient);
                }

                m_SmtpClient.Send(message);

                ZNLogger.Common.Info(string.Format(CultureInfo.InvariantCulture,
                    "MailerServer sent '{0}' to {1} recipient(s).",
                    subject,
                    m_AdministratorRecipients.Count));

                return true;
            }
            catch (Exception e)
            {
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture,
                    "MailerServer Send Error:{0}\n{1}", e.Message, e.StackTrace));

                return false;
            }
        }
    }
}
