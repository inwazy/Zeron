// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.Server.Components.Shared
{
    /// <summary>
    /// UiFormatServer - shared Dashboard formatting helpers.
    /// </summary>
    public static class UiFormatServer
    {
        /// <summary>
        /// FormatTime
        /// </summary>
        /// <param name="value"></param>
        /// <returns>Returns relative or local time string.</returns>
        public static string FormatTime(
            DateTime value)
        {
            DateTime utc = value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
            };

            TimeSpan ago = DateTime.UtcNow - utc;

            if (ago.TotalSeconds < 60)
            {
                return $"{Math.Max(0, (int)ago.TotalSeconds)}s ago";
            }

            if (ago.TotalMinutes < 60)
            {
                return $"{(int)ago.TotalMinutes}m ago";
            }

            if (ago.TotalHours < 24)
            {
                return $"{(int)ago.TotalHours}h ago";
            }

            return utc.ToLocalTime().ToString("g");
        }

        /// <summary>
        /// FormatHeartbeat
        /// </summary>
        /// <param name="lastHeartbeatAt"></param>
        /// <returns>Returns heartbeat display string.</returns>
        public static string FormatHeartbeat(
            DateTime? lastHeartbeatAt)
        {
            if (lastHeartbeatAt == null || lastHeartbeatAt == default(DateTime))
            {
                return "Never";
            }

            return FormatTime(lastHeartbeatAt.Value);
        }

        /// <summary>
        /// Truncate
        /// </summary>
        /// <param name="text"></param>
        /// <param name="maxLength"></param>
        /// <returns>Returns truncated text.</returns>
        public static string Truncate(
            string? text,
            int maxLength)
        {
            if (string.IsNullOrEmpty(text))
            {
                return "";
            }

            return text.Length <= maxLength ? text : text[..maxLength] + "...";
        }
    }
}
