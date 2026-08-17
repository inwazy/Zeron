// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Zeron.Server.Data.Entities;
using Zeron.ZCore.Type;

namespace Zeron.Server.ZInterfaces
{
    /// <summary>
    /// IDashboardNotifier
    /// </summary>
    public interface IDashboardNotifier
    {
        /// <summary>
        /// NotifyEventAsync
        /// </summary>
        /// <param name="eventEntity"></param>
        /// <returns>Returns void.</returns>
        Task NotifyEventAsync(
            EventEntity eventEntity);

        /// <summary>
        /// NotifyAgentStatusAsync
        /// </summary>
        /// <param name="agent"></param>
        /// <returns>Returns void.</returns>
        Task NotifyAgentStatusAsync(
            AgentEntity agent);

        /// <summary>
        /// NotifyAlertAsync
        /// </summary>
        /// <param name="alert"></param>
        /// <returns>Returns void.</returns>
        Task NotifyAlertAsync(
            AlertEntity alert);

        /// <summary>
        /// NotifyInstallResultAsync - push a DeviceOwner install-result tip to that user.
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="notification"></param>
        /// <returns>Returns void.</returns>
        Task NotifyInstallResultAsync(
            Guid userId,
            UserNotificationInfoType notification);
    }
}
