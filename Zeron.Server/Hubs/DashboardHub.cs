// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Zeron.Server.ZCore;

namespace Zeron.Server.Hubs
{
    /// <summary>
    /// DashboardHub
    /// </summary>
    [Authorize(Policy = ServerPolicies.ViewerOrAbove)]
    public class DashboardHub : Hub
    {
    }
}
