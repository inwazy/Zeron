// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

namespace Zeron.Server.ZServers
{
    /// <summary>
    /// DashboardHubClient - creates authenticated SignalR connections for dashboard pages.
    /// </summary>
    public static class DashboardHubClient
    {
        /// <summary>
        /// Create
        /// </summary>
        /// <param name="navigation"></param>
        /// <param name="httpContextAccessor"></param>
        /// <returns>Returns HubConnection.</returns>
        public static HubConnection Create(NavigationManager navigation, IHttpContextAccessor httpContextAccessor)
        {
            return new HubConnectionBuilder()
                .WithUrl(navigation.ToAbsoluteUri("/hubs/dashboard"), options =>
                {
                    string? cookieHeader = httpContextAccessor.HttpContext?.Request.Headers.Cookie;

                    if (!string.IsNullOrEmpty(cookieHeader))
                    {
                        options.Headers.Add("Cookie", cookieHeader);
                    }
                })
                .WithAutomaticReconnect()
                .Build();
        }

        /// <summary>
        /// TryStartAsync
        /// </summary>
        /// <param name="connection"></param>
        /// <returns>Returns bool.</returns>
        public static async Task<bool> TryStartAsync(HubConnection? connection)
        {
            if (connection == null)
            {
                return false;
            }

            try
            {
                await connection.StartAsync();

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
