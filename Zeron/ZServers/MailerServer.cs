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
    /// MailerServer
    /// </summary>
    public class MailerServer : ConfigurationTable, IServer
    {
        // SMTP client handle.
        private static SmtpClient? m_SmtpClient;

        // SMTP mail sender address handle.
        private static MailAddress? m_MailSender;

        // SMTP mail message handle.
        private static MailMessage? m_MailMessage;

        // Email mail recipients administrator event.
        private static readonly Dictionary<string, string> m_MailRecipientsAdministrator = new();

        // Email queue message.
        private static readonly ConcurrentQueue<Tuple<string, string>> m_MailQueueMessages = new();

        // Email queue enable running trigger.
        private static bool m_MailEnableRunning = true;

        // Email queue send signal.
        private static readonly Semaphore m_MailSendSignal = new(0, 20000);

        // Email send per milliseconds.
        private static readonly int m_DelayTimeToSend = 10;

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
        }

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
        /// LoadConfig
        /// </summary>
        /// <param name="aConfig"></param>
        /// <returns>Returns void.</returns>
        public override void LoadConfig(NameValueCollection aConfig)
        {
            if (aConfig == null)
            {
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "MailerServer Config Empty"));

                return;
            }

            try
            {
                Host = aConfig["mail_host"];
                Port = int.Parse(aConfig["mail_port"] ?? "", CultureInfo.InvariantCulture);
                UserLogin = aConfig["mail_user_login"];
                UserPassword = aConfig["mail_user_password"];
                SenderName = aConfig["mail_sender_name"];
                SenderAddress = aConfig["mail_sender_address"];
                RecipientsAdministrator = aConfig["mail_recipients_administrator"];
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
            try
            {
                m_SmtpClient = new SmtpClient
                {
                    Host = Host ?? "",
                    Port = Port,
                    Credentials = new NetworkCredential(UserLogin, UserPassword),
                    EnableSsl = true
                };

                m_MailSender = new MailAddress(SenderAddress ?? "", SenderName);
                m_MailMessage = new MailMessage
                {
                    SubjectEncoding = System.Text.Encoding.UTF8,
                    BodyEncoding = System.Text.Encoding.UTF8,
                    Sender = m_MailSender,
                    From = m_MailSender,
                    IsBodyHtml = true
                };
            }
            catch (ArgumentNullException e)
            {
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "MailerServer ArgumentNullException:{0}\n{1}", e.Message, e.StackTrace));
            }
            catch (ArgumentException e)
            {
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "MailerServer ArgumentException:{0}\n{1}", e.Message, e.StackTrace));
            }
            catch (FormatException e)
            {
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "MailerServer FormatException:{0}\n{1}", e.Message, e.StackTrace));
            }

            try
            {
                Thread threadQueue = new(QueuesProc)
                {
                    IsBackground = true,
                    Name = "MailerServer"
                };

                threadQueue.Start();
            }
            catch (ArgumentNullException e)
            {
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "MailerServer ArgumentNullException:{0}\n{1}", e.Message, e.StackTrace));
            }
        }

        /// <summary>
        /// Stop
        /// </summary>
        /// <returns>Returns void.</returns>
        public void Stop()
        {
            if (m_MailEnableRunning)
            {

            }

            m_MailEnableRunning = false;

            try
            {
                if (m_MailMessage != null)
                {
                    m_MailMessage.Dispose();

                }

                if (m_SmtpClient != null)
                {
                    m_SmtpClient.Dispose();
                }
            }
            catch (Exception e)
            {
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "MailerServer Error:{0}\n{1}", e.Message, e.StackTrace));
            }

            ServerIntegrate.FinishSingleStop();
        }

        /// <summary>
        /// QueuesProc
        /// </summary>
        /// <param name="aArg"></param>
        /// <returns>Returns void.</returns>
        private static void QueuesProc(object? aArg)
        {
            string emailSubject;
            string emailMessage;

            while (m_MailEnableRunning)
            {
                try
                {
                    m_MailSendSignal.WaitOne();
                    m_MailQueueMessages.TryDequeue(out Tuple<string, string>? item);

                    if (item == null)
                    {
                        continue;
                    }

                    emailSubject = item.Item1;
                    emailMessage = item.Item2;

                    if (string.IsNullOrEmpty(emailSubject)
                        || string.IsNullOrEmpty(emailMessage))
                    {
                        continue;
                    }

                    // TODO

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
    }
}
