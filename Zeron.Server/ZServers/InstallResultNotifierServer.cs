// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text.Json;
using Zeron.Server.Data;
using Zeron.Server.Data.Entities;
using Zeron.Server.ZCore;
using Zeron.ZCore;
using Zeron.ZCore.Type;
using Zeron.ZCore.Utils;

namespace Zeron.Server.ZServers
{
    /// <summary>
    /// InstallResultNotifierServer - notify DeviceOwners of self-service install results.
    /// </summary>
    public class InstallResultNotifierServer
    {
        // Database context.
        private readonly ZeronServerDbContext m_DbContext;

        // Notifications.
        private readonly UserNotificationServer m_NotificationServer;

        // Settings.
        private readonly ServerSettings m_Settings;

        /// <summary>
        /// InstallResultNotifierServer
        /// </summary>
        /// <param name="dbContext"></param>
        /// <param name="notificationServer"></param>
        /// <param name="settings"></param>
        /// <returns>Returns void.</returns>
        public InstallResultNotifierServer(
            ZeronServerDbContext dbContext,
            UserNotificationServer notificationServer,
            ServerSettings settings)
        {
            m_DbContext = dbContext;
            m_NotificationServer = notificationServer;
            m_Settings = settings;
        }

        /// <summary>
        /// NotifyFromInstallEventAsync
        /// </summary>
        /// <param name="report"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns notified user count.</returns>
        public async Task<int> NotifyFromInstallEventAsync(
            AgentEventReportType report,
            CancellationToken cancellationToken = default)
        {
            if (!m_Settings.InstallResultNotifyEnabled
                && !m_Settings.InstallResultEmailEnabled)
            {
                return 0;
            }

            bool isCompleted = string.Equals(report.Topic, "install.completed", StringComparison.OrdinalIgnoreCase);
            bool isFailed = string.Equals(report.Topic, "install.failed", StringComparison.OrdinalIgnoreCase);

            if (!isCompleted && !isFailed)
            {
                return 0;
            }

            if (string.IsNullOrWhiteSpace(report.AgentId)
                || !TryReadPayload(report.Payload, out string? assignmentId, out bool? success, out int? exitCode, out string? package))
            {
                return 0;
            }

            if (!Guid.TryParse(assignmentId, out Guid assignmentGuid))
            {
                return 0;
            }

            TaskAssignmentEntity? assignment = await m_DbContext.TaskAssignments
                .AsNoTracking()
                .Include(item => item.Task)
                .FirstOrDefaultAsync(item => item.Id == assignmentGuid, cancellationToken);

            if (assignment?.Task == null
                || !assignment.Task.Name.StartsWith("self-", StringComparison.OrdinalIgnoreCase))
            {
                return 0;
            }

            bool finalSuccess = isCompleted && success != false;
            string packageName = string.IsNullOrWhiteSpace(package) ? "unknown" : package.Trim();
            string agentKey = report.AgentId.Trim();
            string operation = assignment.Task.Command.StartsWith("uninstall", StringComparison.OrdinalIgnoreCase)
                ? "uninstall"
                : "install";

            List<UserEntity> recipients = await m_DbContext.UserAgentBindings
                .AsNoTracking()
                .Where(binding => binding.AgentKey == agentKey)
                .Join(
                    m_DbContext.Users.AsNoTracking().Where(user => user.IsActive),
                    binding => binding.UserId,
                    user => user.Id,
                    (_, user) => user)
                .Distinct()
                .ToListAsync(cancellationToken);

            if (recipients.Count == 0)
            {
                return 0;
            }

            string title = finalSuccess
                ? string.Format(CultureInfo.InvariantCulture, "{0} succeeded: {1}", operation, packageName)
                : string.Format(CultureInfo.InvariantCulture, "{0} failed: {1}", operation, packageName);

            string message = string.Format(CultureInfo.InvariantCulture,
                "{0} of '{1}' on agent '{2}' {3} (exitCode={4}).",
                char.ToUpperInvariant(operation[0]) + operation[1..],
                packageName,
                agentKey,
                finalSuccess ? "completed successfully" : "failed",
                exitCode?.ToString(CultureInfo.InvariantCulture) ?? "n/a");

            int notified = 0;

            foreach (UserEntity user in recipients)
            {
                if (m_Settings.InstallResultNotifyEnabled)
                {
                    await m_NotificationServer.CreateAsync(
                        user.Id,
                        UserNotificationServer.KindInstallResult,
                        title,
                        message,
                        agentKey,
                        packageName,
                        finalSuccess,
                        cancellationToken);
                }

                if (m_Settings.InstallResultEmailEnabled
                    && !string.IsNullOrWhiteSpace(user.Email))
                {
                    await TrySendEmailAsync(user.Email!, title, message);
                }

                notified++;
            }

            if (notified > 0)
            {
                ZNLogger.Common.Info(string.Format(CultureInfo.InvariantCulture,
                    "InstallResultNotifierServer notified {0} user(s) for {1} on {2}.",
                    notified,
                    packageName,
                    agentKey));
            }

            return notified;
        }

        /// <summary>
        /// TrySendEmailAsync
        /// </summary>
        private async Task TrySendEmailAsync(
            string toAddress,
            string title,
            string message)
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

            if (!SmtpMailServer.HasConnection(options))
            {
                return;
            }

            (bool ok, Exception? error) = await SmtpMailServer.TrySendAsync(
                options,
                toAddress,
                "[Zeron] " + title,
                message,
                isBodyHtml: false);

            if (ok)
            {
                ZNLogger.Common.Info(string.Format(CultureInfo.InvariantCulture,
                    "InstallResultNotifierServer emailed '{0}'.", toAddress));

                return;
            }

            ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture,
                "InstallResultNotifierServer email Error:{0}\n{1}",
                error?.Message,
                error?.StackTrace));
        }

        /// <summary>
        /// TryReadPayload
        /// </summary>
        private static bool TryReadPayload(
            string? payload,
            out string? assignmentId,
            out bool? success,
            out int? exitCode,
            out string? package)
        {
            assignmentId = null;
            success = null;
            exitCode = null;
            package = null;

            if (string.IsNullOrWhiteSpace(payload))
            {
                return false;
            }

            try
            {
                using JsonDocument document = JsonDocument.Parse(payload);
                JsonElement root = document.RootElement;

                if (root.TryGetProperty("assignmentId", out JsonElement assignmentElement)
                    && assignmentElement.ValueKind == JsonValueKind.String)
                {
                    assignmentId = assignmentElement.GetString();
                }

                if (root.TryGetProperty("package", out JsonElement packageElement)
                    && packageElement.ValueKind == JsonValueKind.String)
                {
                    package = packageElement.GetString();
                }

                if (root.TryGetProperty("success", out JsonElement successElement)
                    && (successElement.ValueKind == JsonValueKind.True || successElement.ValueKind == JsonValueKind.False))
                {
                    success = successElement.GetBoolean();
                }

                if (root.TryGetProperty("exitCode", out JsonElement exitElement)
                    && exitElement.TryGetInt32(out int code))
                {
                    exitCode = code;
                }

                return !string.IsNullOrWhiteSpace(assignmentId);
            }
            catch (JsonException)
            {
                return false;
            }
        }
    }
}
