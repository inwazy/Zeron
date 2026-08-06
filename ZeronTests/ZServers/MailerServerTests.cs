// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using System.Collections.Specialized;
using Zeron.ZServers;

namespace Zeron.ZServers.Tests
{
    [TestClass()]
    public class MailerServerTests
    {
        private MailerServer? m_Mailer;

        [TestCleanup]
        public void Cleanup()
        {
            m_Mailer?.Stop();
            m_Mailer = null;
        }

        [TestMethod()]
        public void LoadConfigParsesSmtpSettingsTest()
        {
            m_Mailer = new MailerServer();
            NameValueCollection config = new()
            {
                ["mail_enabled"] = "true",
                ["mail_host"] = "smtp.example.com",
                ["mail_port"] = "465",
                ["mail_user_login"] = "user",
                ["mail_user_password"] = "pass",
                ["mail_sender_name"] = "Zeron Ops",
                ["mail_sender_address"] = "ops@example.com",
                ["mail_recipients_administrator"] = "a@example.com, b@example.com",
                ["mail_enable_ssl"] = "false"
            };

            m_Mailer.LoadConfig(config);

            Assert.IsTrue(MailerServer.Enabled);
            Assert.AreEqual("smtp.example.com", MailerServer.Host);
            Assert.AreEqual(465, MailerServer.Port);
            Assert.AreEqual("user", MailerServer.UserLogin);
            Assert.AreEqual("pass", MailerServer.UserPassword);
            Assert.AreEqual("Zeron Ops", MailerServer.SenderName);
            Assert.AreEqual("ops@example.com", MailerServer.SenderAddress);
            Assert.AreEqual("a@example.com, b@example.com", MailerServer.RecipientsAdministrator);
            Assert.IsFalse(MailerServer.EnableSsl);
        }

        [TestMethod()]
        public void InitializeDisabledLeavesMailerUnconfiguredTest()
        {
            m_Mailer = new MailerServer();
            m_Mailer.LoadConfig(new NameValueCollection
            {
                ["mail_enabled"] = "false",
                ["mail_host"] = "smtp.example.com",
                ["mail_sender_address"] = "ops@example.com",
                ["mail_recipients_administrator"] = "a@example.com"
            });
            m_Mailer.Initialize();

            Assert.IsFalse(MailerServer.IsConfigured);
            Assert.IsFalse(MailerServer.QueueMail("subject", "<p>body</p>"));
        }

        [TestMethod()]
        public void InitializeWithoutRecipientsIsNotConfiguredTest()
        {
            m_Mailer = new MailerServer();
            m_Mailer.LoadConfig(new NameValueCollection
            {
                ["mail_enabled"] = "true",
                ["mail_host"] = "smtp.example.com",
                ["mail_sender_address"] = "ops@example.com",
                ["mail_recipients_administrator"] = ""
            });
            m_Mailer.Initialize();

            Assert.IsFalse(MailerServer.IsConfigured);
            Assert.IsFalse(MailerServer.SendMailNow("subject", "<p>body</p>"));
        }

        [TestMethod()]
        public void InitializeWithValidSmtpMarksConfiguredTest()
        {
            m_Mailer = new MailerServer();
            m_Mailer.LoadConfig(new NameValueCollection
            {
                ["mail_enabled"] = "true",
                ["mail_host"] = "smtp.example.com",
                ["mail_port"] = "587",
                ["mail_sender_name"] = "Zeron",
                ["mail_sender_address"] = "ops@example.com",
                ["mail_recipients_administrator"] = "admin@example.com, ops@example.com",
                ["mail_enable_ssl"] = "true"
            });
            m_Mailer.Initialize();

            Assert.IsTrue(MailerServer.IsConfigured);
            Assert.IsFalse(MailerServer.QueueMail("", "<p>body</p>"));
            Assert.IsFalse(MailerServer.QueueMail("subject", ""));
        }
    }
}
