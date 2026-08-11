// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.Globalization;
using System.Net.Mail;
using Zeron.ZCore;
using Zeron.ZCore.Container;
using Zeron.ZCore.Foundation;
using Zeron.ZCore.Type;
using Zeron.ZCore.Utils;
using Zeron.ZInterfaces;

namespace Zeron.ZServers
{
    /// <summary>
    /// MailerServer - queued SMTP sender (instance state + Current facade).
    /// </summary>
    public class MailerServer : ConfigurationTable, IServer
    {
        // Active runtime instance.
        public static MailerServer? Current
        {
            get;
            private set;
        }

        // Email send per milliseconds.
        private const int DelayTimeToSend = 10;

        // SMTP client handle.
        private SmtpClient? m_SmtpClient;

        // SMTP mail sender address handle.
        private MailAddress? m_MailSender;

        // Email queue message.
        private readonly ConcurrentQueue<Tuple<string, string>> m_MailQueueMessages = new();

        // Email queue enable running trigger.
        private bool m_MailEnableRunning = true;

        // Email queue send signal.
        private readonly Semaphore m_MailSendSignal = new(0, 20000);

        // Cached administrator recipients.
        private readonly List<MailAddress> m_AdministratorRecipients = [];

        // Whether SMTP is ready.
        private bool m_IsConfigured;

        // Queue thread (started once per instance).
        private Thread? m_QueueThread;

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
        public static bool IsConfigured => Current?.m_IsConfigured == true;

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
            Current = this;
            m_MailEnableRunning = true;
            m_IsConfigured = false;
            m_AdministratorRecipients.Clear();

            try
            {
                m_SmtpClient?.Dispose();
            }
            catch (Exception e)
            {
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture,
                    "MailerServer Dispose Error:{0}\n{1}", e.Message, e.StackTrace));
            }

            m_SmtpClient = null;
            m_MailSender = null;

            SmtpMailOptionsType options = BuildOptions();

            if (!Enabled || !SmtpMailServer.HasConnection(options))
            {
                ZNLogger.Common.Info("MailerServer disabled or incomplete SMTP configuration.");
                EnsureQueueThread();

                return;
            }

            try
            {
                m_SmtpClient = SmtpMailServer.CreateClient(options);
                m_MailSender = SmtpMailServer.CreateFromAddress(options);
                m_AdministratorRecipients.AddRange(SmtpMailServer.ParseRecipients(
                    RecipientsAdministrator,
                    part => ZNLogger.Common.Warn(string.Format(CultureInfo.InvariantCulture,
                        "MailerServer skipping invalid recipient '{0}'", part))));
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

            EnsureQueueThread();
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
            return Current?.QueueMailCore(subject, bodyHtml) ?? false;
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
            return Current?.SendQueuedMessage(subject, bodyHtml) ?? false;
        }

        /// <summary>
        /// Stop
        /// </summary>
        /// <returns>Returns void.</returns>
        public void Stop()
        {
            m_MailEnableRunning = false;
            m_IsConfigured = false;

            try
            {
                m_MailSendSignal.Release();
            }
            catch (SemaphoreFullException)
            {
            }
            catch (ObjectDisposedException)
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

            if (ReferenceEquals(Current, this))
            {
                Current = null;
            }

            ServerIntegrate.FinishSingleStop();
        }

        /// <summary>
        /// QueueMailCore
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="bodyHtml"></param>
        /// <returns>Returns bool.</returns>
        private bool QueueMailCore(
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
            catch (ObjectDisposedException)
            {
            }

            return true;
        }

        /// <summary>
        /// BuildOptions
        /// </summary>
        /// <returns>Returns SmtpMailOptions.</returns>
        private static SmtpMailOptionsType BuildOptions()
        {
            return new SmtpMailOptionsType
            {
                Host = Host,
                Port = Port,
                EnableSsl = EnableSsl,
                UserName = UserLogin,
                Password = UserPassword,
                FromAddress = SenderAddress,
                FromDisplayName = SenderName
            };
        }

        /// <summary>
        /// EnsureQueueThread - start queue worker once per instance.
        /// </summary>
        /// <returns>Returns void.</returns>
        private void EnsureQueueThread()
        {
            if (m_QueueThread != null && m_QueueThread.IsAlive)
            {
                return;
            }

            try
            {
                m_QueueThread = new Thread(QueuesProc)
                {
                    IsBackground = true,
                    Name = "MailerServer"
                };
                m_QueueThread.Start();
            }
            catch (Exception e)
            {
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture,
                    "MailerServer StartQueueThread Error:{0}\n{1}", e.Message, e.StackTrace));
            }
        }

        /// <summary>
        /// QueuesProc
        /// </summary>
        /// <param name="aArg"></param>
        /// <returns>Returns void.</returns>
        private void QueuesProc(
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
                    Thread.Sleep(DelayTimeToSend);
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
        private bool SendQueuedMessage(
            string subject,
            string bodyHtml)
        {
            if (!m_IsConfigured || m_SmtpClient == null || m_MailSender == null)
            {
                return false;
            }

            bool sent = SmtpMailServer.TrySend(
                m_SmtpClient,
                m_MailSender,
                m_AdministratorRecipients,
                subject,
                bodyHtml,
                isBodyHtml: true,
                out Exception? error);

            if (sent)
            {
                ZNLogger.Common.Info(string.Format(CultureInfo.InvariantCulture,
                    "MailerServer sent '{0}' to {1} recipient(s).",
                    subject,
                    m_AdministratorRecipients.Count));

                return true;
            }

            ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture,
                "MailerServer Send Error:{0}\n{1}",
                error?.Message,
                error?.StackTrace));

            return false;
        }
    }
}
