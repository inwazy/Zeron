// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using System.Net.Mail;
using Zeron.ZCore.Type;

namespace Zeron.ZCore.Utils.Tests
{
    [TestClass()]
    public class SmtpMailServerTests
    {
        [TestMethod()]
        public void HasConnectionRequiresHostAndFromTest()
        {
            Assert.IsFalse(SmtpMailServer.HasConnection(null));
            Assert.IsFalse(SmtpMailServer.HasConnection(new SmtpMailOptions
            {
                Host = "smtp.example.com"
            }));
            Assert.IsTrue(SmtpMailServer.HasConnection(new SmtpMailOptions
            {
                Host = "smtp.example.com",
                FromAddress = "ops@example.com"
            }));
        }

        [TestMethod()]
        public void ParseRecipientsSplitsAndSkipsInvalidTest()
        {
            List<string> invalid = [];
            List<MailAddress> recipients = SmtpMailServer.ParseRecipients(
                "a@example.com, b@example.com|not-an-email; c@example.com",
                part => invalid.Add(part));

            Assert.AreEqual(3, recipients.Count);
            Assert.AreEqual("a@example.com", recipients[0].Address);
            Assert.AreEqual("b@example.com", recipients[1].Address);
            Assert.AreEqual("c@example.com", recipients[2].Address);
            Assert.AreEqual(1, invalid.Count);
            Assert.AreEqual("not-an-email", invalid[0]);
        }

        [TestMethod()]
        public void CreateClientAppliesOptionsTest()
        {
            using SmtpClient client = SmtpMailServer.CreateClient(new SmtpMailOptions
            {
                Host = "smtp.example.com",
                Port = 465,
                EnableSsl = false,
                UserName = "user",
                Password = "pass",
                FromAddress = "ops@example.com"
            });

            Assert.AreEqual("smtp.example.com", client.Host);
            Assert.AreEqual(465, client.Port);
            Assert.IsFalse(client.EnableSsl);
            Assert.IsNotNull(client.Credentials);
        }

        [TestMethod()]
        public void CreateFromAddressUsesDisplayNameTest()
        {
            MailAddress from = SmtpMailServer.CreateFromAddress(new SmtpMailOptions
            {
                FromAddress = "ops@example.com",
                FromDisplayName = "Zeron Ops"
            });

            Assert.AreEqual("ops@example.com", from.Address);
            Assert.AreEqual("Zeron Ops", from.DisplayName);
        }

        [TestMethod()]
        public async Task TrySendAsyncRejectsMissingRecipientsTest()
        {
            (bool ok, Exception? error) = await SmtpMailServer.TrySendAsync(
                new SmtpMailOptions
                {
                    Host = "smtp.example.com",
                    FromAddress = "ops@example.com"
                },
                recipients: "",
                subject: "subject",
                body: "body",
                isBodyHtml: false);

            Assert.IsFalse(ok);
            Assert.IsNotNull(error);
        }
    }
}
