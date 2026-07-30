// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.ZCore
{
    /// <summary>
    /// InstallEventPublisher - optional callback wired by the host to broadcast install events.
    /// </summary>
    public static class InstallEventPublisher
    {
        /// <summary>
        /// PublishHandler
        /// </summary>
        public static Action<string, string>? PublishHandler
        {
            get;
            set;
        }

        /// <summary>
        /// Publish
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="message"></param>
        /// <returns>Returns void.</returns>
        public static void Publish(string topic, string message)
        {
            PublishHandler?.Invoke(topic, message);
        }
    }
}
