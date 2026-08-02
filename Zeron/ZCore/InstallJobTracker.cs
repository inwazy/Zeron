// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using System.Collections.Concurrent;
using System.Globalization;
using Zeron.ZCore.Type;

namespace Zeron.ZCore
{
    /// <summary>
    /// InstallJobTracker - tracks install/uninstall queue state.
    /// </summary>
    public static class InstallJobTracker
    {
        // Lock object.
        private static readonly object s_Lock = new();

        // Is running.
        private static bool s_IsRunning;

        // Current package.
        private static string? s_CurrentPackage;

        // Current operation.
        private static string? s_CurrentOperation;

        // Last package.
        private static string? s_LastPackage;

        // Last operation.
        private static string? s_LastOperation;

        // Last success.
        private static bool? s_LastSuccess;

        // Last exit code.
        private static int? s_LastExitCode;

        // Last completed at.
        private static DateTime? s_LastCompletedAt;

        /// <summary>
        /// QueueCountProvider
        /// </summary>
        public static Func<int>? QueueCountProvider
        {
            get;
            set;
        }

        /// <summary>
        /// MarkRunning
        /// </summary>
        /// <param name="packageName"></param>
        /// <param name="operation"></param>
        /// <returns>Returns void.</returns>
        public static void MarkRunning(
            string? packageName, 
            string? operation)
        {
            lock (s_Lock)
            {
                s_IsRunning = true;
                s_CurrentPackage = packageName;
                s_CurrentOperation = operation;
            }
        }

        /// <summary>
        /// MarkCompleted
        /// </summary>
        /// <param name="packageName"></param>
        /// <param name="operation"></param>
        /// <param name="success"></param>
        /// <param name="exitCode"></param>
        /// <returns>Returns void.</returns>
        public static void MarkCompleted(
            string? packageName, 
            string? operation, 
            bool success, 
            int exitCode)
        {
            lock (s_Lock)
            {
                s_IsRunning = false;
                s_CurrentPackage = null;
                s_CurrentOperation = null;
                s_LastPackage = packageName;
                s_LastOperation = operation;
                s_LastSuccess = success;
                s_LastExitCode = exitCode;
                s_LastCompletedAt = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// GetStatus
        /// </summary>
        /// <returns>Returns InstallJobStatus.</returns>
        public static InstallJobStatus GetStatus()
        {
            lock (s_Lock)
            {
                return new InstallJobStatus
                {
                    QueueCount = QueueCountProvider?.Invoke() ?? 0,
                    IsRunning = s_IsRunning,
                    CurrentPackage = s_CurrentPackage,
                    CurrentOperation = s_CurrentOperation,
                    LastPackage = s_LastPackage,
                    LastOperation = s_LastOperation,
                    LastSuccess = s_LastSuccess,
                    LastExitCode = s_LastExitCode,
                    LastCompletedAt = s_LastCompletedAt?.ToString("o", CultureInfo.InvariantCulture)
                };
            }
        }
    }
}
