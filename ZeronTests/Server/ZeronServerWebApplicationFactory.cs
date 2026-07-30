// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Zeron.Server.Tests
{
    /// <summary>
    /// ZeronServerWebApplicationFactory
    /// </summary>
    public class ZeronServerWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly string m_DatabasePath = Path.Combine(
            Path.GetTempPath(),
            "zeron-e2e-" + Guid.NewGuid().ToString("N") + ".db");

        /// <summary>
        /// ConfigureWebHost
        /// </summary>
        /// <param name="builder"></param>
        /// <returns>Returns void.</returns>
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, configBuilder) =>
            {
                configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Zeron:DatabasePath"] = m_DatabasePath,
                    ["Zeron:AgentApiKey"] = "zeron.testkey",
                    ["Zeron:DefaultAdminUsername"] = "admin",
                    ["Zeron:DefaultAdminPassword"] = "admin123",
                    ["Zeron:JwtSecret"] = "zeron-e2e-test-secret-min-32-chars",
                    ["Zeron:JwtIssuer"] = "Zeron.Server",
                    ["Zeron:HeartbeatTimeoutSeconds"] = "90"
                });
            });
        }
    }
}
