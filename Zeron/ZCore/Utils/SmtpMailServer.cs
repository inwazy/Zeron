// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using System.Net;
using System.Net.Mail;
using System.Text;
using Zeron.ZCore.Type;

namespace Zeron.ZCore.Utils
{
    /// <summary>
    /// SmtpMailServer - shared SMTP client / message helpers for agent and server.
    /// </summary>
    public static class SmtpMailServer
    {
        // Recipient separators shared by Mailer and AlertNotifier.
        private static readonly char[] s_RecipientSeparators = [',', '|', ';', ' '];

        /// <summary>
        /// HasConnection - host and from address are present.
        /// </summary>
        /// <param name="options"></param>
        /// <returns>Returns bool.</returns>
        public static bool HasConnection(
            SmtpMailOptions? options)
        {
            return options != null
                && !string.IsNullOrWhiteSpace(options.Host)
                && !string.IsNullOrWhiteSpace(options.FromAddress);
        }

        /// <summary>
        /// CreateClient
        /// </summary>
        /// <param name="options"></param>
        /// <returns>Returns SmtpClient.</returns>
        public static SmtpClient CreateClient(
            SmtpMailOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);

            if (string.IsNullOrWhiteSpace(options.Host))
            {
                throw new ArgumentException("SMTP host is required.", nameof(options));
            }

            SmtpClient client = new()
            {
                Host = options.Host.Trim(),
                Port = options.Port > 0 ? options.Port : 587,
                EnableSsl = options.EnableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            if (!string.IsNullOrWhiteSpace(options.UserName))
            {
                client.Credentials = new NetworkCredential(options.UserName, options.Password);
            }

            return client;
        }

        /// <summary>
        /// CreateFromAddress
        /// </summary>
        /// <param name="options"></param>
        /// <returns>Returns MailAddress.</returns>
        public static MailAddress CreateFromAddress(
            SmtpMailOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);

            if (string.IsNullOrWhiteSpace(options.FromAddress))
            {
                throw new ArgumentException("SMTP from address is required.", nameof(options));
            }

            return new MailAddress(
                options.FromAddress.Trim(),
                string.IsNullOrWhiteSpace(options.FromDisplayName) ? "Zeron" : options.FromDisplayName.Trim());
        }

        /// <summary>
        /// ParseRecipients - splits on comma, pipe, semicolon, or whitespace.
        /// </summary>
        /// <param name="recipients"></param>
        /// <param name="onInvalid"></param>
        /// <returns>Returns recipient list.</returns>
        public static List<MailAddress> ParseRecipients(
            string? recipients,
            Action<string>? onInvalid = null)
        {
            List<MailAddress> result = [];

            if (string.IsNullOrWhiteSpace(recipients))
            {
                return result;
            }

            foreach (string part in recipients.Split(
                s_RecipientSeparators,
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                try
                {
                    result.Add(new MailAddress(part));
                }
                catch (FormatException)
                {
                    onInvalid?.Invoke(part);
                }
            }

            return result;
        }

        /// <summary>
        /// CreateMessage
        /// </summary>
        /// <param name="from"></param>
        /// <param name="recipients"></param>
        /// <param name="subject"></param>
        /// <param name="body"></param>
        /// <param name="isBodyHtml"></param>
        /// <returns>Returns MailMessage.</returns>
        public static MailMessage CreateMessage(
            MailAddress from,
            IEnumerable<MailAddress> recipients,
            string subject,
            string body,
            bool isBodyHtml)
        {
            ArgumentNullException.ThrowIfNull(from);
            ArgumentNullException.ThrowIfNull(recipients);

            MailMessage message = new()
            {
                SubjectEncoding = Encoding.UTF8,
                BodyEncoding = Encoding.UTF8,
                Sender = from,
                From = from,
                IsBodyHtml = isBodyHtml,
                Subject = subject ?? "",
                Body = body ?? ""
            };

            foreach (MailAddress recipient in recipients)
            {
                message.To.Add(recipient);
            }

            if (message.To.Count == 0)
            {
                message.Dispose();
                throw new ArgumentException("At least one recipient is required.", nameof(recipients));
            }

            return message;
        }

        /// <summary>
        /// TrySend - send with an existing SmtpClient (queued / long-lived).
        /// </summary>
        /// <param name="client"></param>
        /// <param name="from"></param>
        /// <param name="recipients"></param>
        /// <param name="subject"></param>
        /// <param name="body"></param>
        /// <param name="isBodyHtml"></param>
        /// <param name="error"></param>
        /// <returns>Returns bool.</returns>
        public static bool TrySend(
            SmtpClient client,
            MailAddress from,
            IEnumerable<MailAddress> recipients,
            string subject,
            string body,
            bool isBodyHtml,
            out Exception? error)
        {
            error = null;

            if (client == null || from == null || recipients == null
                || string.IsNullOrWhiteSpace(subject)
                || string.IsNullOrWhiteSpace(body))
            {
                error = new ArgumentException("SMTP client, from, recipients, subject, and body are required.");

                return false;
            }

            try
            {
                using MailMessage message = CreateMessage(from, recipients, subject.Trim(), body, isBodyHtml);
                client.Send(message);

                return true;
            }
            catch (Exception e)
            {
                error = e;

                return false;
            }
        }

        /// <summary>
        /// TrySendAsync - one-shot send that creates and disposes the SmtpClient.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="recipients"></param>
        /// <param name="subject"></param>
        /// <param name="body"></param>
        /// <param name="isBodyHtml"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns (ok, error).</returns>
        public static async Task<(bool Ok, Exception? Error)> TrySendAsync(
            SmtpMailOptions options,
            IEnumerable<MailAddress> recipients,
            string subject,
            string body,
            bool isBodyHtml,
            CancellationToken cancellationToken = default)
        {
            if (!HasConnection(options)
                || recipients == null
                || string.IsNullOrWhiteSpace(subject)
                || string.IsNullOrWhiteSpace(body))
            {
                return (false, new ArgumentException("SMTP options, recipients, subject, and body are required."));
            }

            try
            {
                using SmtpClient client = CreateClient(options);
                MailAddress from = CreateFromAddress(options);
                using MailMessage message = CreateMessage(from, recipients, subject.Trim(), body, isBodyHtml);

                await client.SendMailAsync(message, cancellationToken).ConfigureAwait(false);

                return (true, null);
            }
            catch (Exception e)
            {
                return (false, e);
            }
        }

        /// <summary>
        /// TrySendAsync - parse recipient string then one-shot send.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="recipients"></param>
        /// <param name="subject"></param>
        /// <param name="body"></param>
        /// <param name="isBodyHtml"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns (ok, error).</returns>
        public static Task<(bool Ok, Exception? Error)> TrySendAsync(
            SmtpMailOptions options,
            string? recipients,
            string subject,
            string body,
            bool isBodyHtml,
            CancellationToken cancellationToken = default)
        {
            List<MailAddress> parsed = ParseRecipients(recipients);

            if (parsed.Count == 0)
            {
                return Task.FromResult<(bool, Exception?)>((false, new ArgumentException("No valid SMTP recipients.")));
            }

            return TrySendAsync(options, parsed, subject, body, isBodyHtml, cancellationToken);
        }
    }
}
