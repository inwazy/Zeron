// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Zeron.Server.Components.Shared;
using Zeron.Server.Data.Entities;

namespace Zeron.Server.ZInterfaces
{
    /// <summary>
    /// IAlertRowActionContribution — optional alert row actions from editions.
    /// </summary>
    public interface IAlertRowActionContribution
    {
        /// <summary>
        /// GetActions
        /// </summary>
        /// <param name="alert">Alert row.</param>
        /// <returns>Returns row actions for the alert.</returns>
        IReadOnlyList<AlertRowAction> GetActions(
            AlertEntity alert);
    }
}
