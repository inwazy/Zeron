// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Zeron.Server.Components.Shared;

namespace Zeron.Server.ZInterfaces
{
    /// <summary>
    /// INavMenuContribution — optional nav links from editions (e.g. ZeronOpsPro).
    /// </summary>
    public interface INavMenuContribution
    {
        /// <summary>
        /// GetItems
        /// </summary>
        /// <returns>Returns nav items to append for staff roles.</returns>
        IReadOnlyList<NavMenuItem> GetItems();
    }
}
