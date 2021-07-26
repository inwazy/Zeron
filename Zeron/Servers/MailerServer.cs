// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using NLog.Internal;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Mail;
using System.Threading;
using Zeron.Core;
using Zeron.Core.Base;
using Zeron.Core.Container;
using Zeron.Interfaces;

namespace Zeron.Servers
{
    /// <summary>
    /// MailerServer
    /// </summary>
    public class MailerServer : ConfigurationTable, IServer
    {
        // SMTP client handle.
        private static SmtpClient m_SmtpClient;

        // SMTP mail sender address handle.
        private static MailAddress m_MailSender;

        // SMTP mail message handle.
        private static MailMessage m_MailMessage;

        // Email mail recipients administrator event.
        private static readonly Dictionary<string, string> m_MailRecipientsAdministrator = new Dictionary<string, string>();

        // Email queue message.
        private static readonly ConcurrentQueue<Tuple<string, string>> m_MailQueueMessages = new ConcurrentQueue<Tuple<string, string>>();

        // Email queue enable running trigger.
        private static bool m_MailEnableRunning = true;

        // Email queue send signal.
        private static readonly Semaphore m_MailSendSignal = new Semaphore(0, 20000);

        // Email send per milliseconds
        private static readonly int m_DelayTimeToSend = 10;

        /// <summary>
        /// Host
        /// </summary>
        public static string Host
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
        public static string UserLogin
        {
            get;
            set;
        }

        /// <summary>
        /// UserPassword
        /// </summary>
        public static string UserPassword
        {
            get;
            set;
        }

        /// <summary>
        /// SenderName
        /// </summary>
        public static string SenderName
        {
            get;
            set;
        }

        /// <summary>
        /// SenderAddress
        /// </summary>
        public static string SenderAddress
        {
            get;
            set;
        }

        /// <summary>
        /// RecipientsAdministrator
        /// </summary>
        public static string RecipientsAdministrator
        {
            get;
            set;
        }

        /// <summary>
        /// LoadConfig
        /// </summary>
        /// <param name="aConfig"></param>
        /// <returns>Returns void.</returns>
        public override void LoadConfig(ConfigurationManager aConfig)
        {
            if (aConfig == null)
            {
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "MailerServer Config Empty"));

                return;
            }

            try
            {
                Host = aConfig.AppSettings["mail_host"];
                Port = int.Parse(aConfig.AppSettings["mail_port"], CultureInfo.InvariantCulture);
                UserLogin = aConfig.AppSettings["mail_user_login"];
                UserPassword = aConfig.AppSettings["mail_user_password"];
                SenderName = aConfig.AppSettings["mail_sender_name"];
                SenderAddress = aConfig.AppSettings["mail_sender_address"];
                RecipientsAdministrator = aConfig.AppSettings["mail_recipients_administrator"];
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
                    Host = Host,
                    Port = Port,
                    Credentials = new NetworkCredential(UserLogin, UserPassword),
                    EnableSsl = true
                };

                m_MailSender = new MailAddress(SenderAddress, SenderName);
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
                Thread threadQueue = new Thread(QueuesProc)
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
                m_MailMessage.Dispose();
                m_SmtpClient.Dispose();
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
        private static void QueuesProc(object aArg)
        {
            string emailSubject;
            string emailMessage;

            while (m_MailEnableRunning)
            {
                try
                {
                    m_MailSendSignal.WaitOne();
                    m_MailQueueMessages.TryDequeue(out Tuple<string, string> item);

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



                    Thread.Sleep(m_DelayTimeToSend);
                }
                catch (ObjectDisposedException e)
                {
                    ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "MailerServer ObjectDisposedException:{0}\n{1}", e.Message, e.StackTrace));
                }
                catch (AbandonedMutexException e)
                {
                    ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "MailerServer AbandonedMutexException:{0}\n{1}", e.Message, e.StackTrace));
                }
                catch (InvalidOperationException e)
                {
                    ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "MailerServer InvalidOperationException:{0}\n{1}", e.Message, e.StackTrace));
                }
            }
        }
    }
}
