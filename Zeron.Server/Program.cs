// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Microsoft.EntityFrameworkCore;
using NLog;
using NLog.Extensions.Logging;
using Zeron.Server.Background;
using Zeron.Server.Data;
using Zeron.Server.Endpoints;
using Zeron.Server.ZCore;
using Zeron.Server.ZServers;

namespace Zeron.Server
{
    /// <summary>
    /// Program
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// Main
        /// </summary>
        /// <param name="args"></param>
        /// <returns>Returns void.</returns>
        public static void Main(string[] args)
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

            LogManager.Setup().LoadConfigurationFromFile("NLog.config");
            builder.Logging.ClearProviders();
            builder.Logging.AddNLog();

            ServerSettings serverSettings = builder.Configuration
                .GetSection(ServerSettings.SectionName)
                .Get<ServerSettings>() ?? new ServerSettings();

            string dbPath = Path.IsPathRooted(serverSettings.DatabasePath)
                ? serverSettings.DatabasePath
                : Path.Combine(AppContext.BaseDirectory, serverSettings.DatabasePath);

            string? dbDirectory = Path.GetDirectoryName(dbPath);

            if (!string.IsNullOrEmpty(dbDirectory))
            {
                Directory.CreateDirectory(dbDirectory);
            }

            builder.Services.AddSingleton(serverSettings);
            builder.Services.AddDbContext<ZeronServerDbContext>(options => options.UseSqlite("Data Source=" + dbPath));
            builder.Services.AddScoped<AgentManagerServer>();
            builder.Services.AddScoped<TaskDispatcherServer>();
            builder.Services.AddScoped<EventIngestorServer>();
            builder.Services.AddSingleton<CommandPublisherServer>();
            builder.Services.AddHostedService<HeartbeatMonitorWorker>();
            builder.Services.AddHostedService<TaskDispatchWorker>();

            WebApplication app = builder.Build();

            using (IServiceScope scope = app.Services.CreateScope())
            {
                ZeronServerDbContext dbContext = scope.ServiceProvider.GetRequiredService<ZeronServerDbContext>();
                dbContext.Database.EnsureCreated();
            }

            CommandPublisherServer commandPublisher = app.Services.GetRequiredService<CommandPublisherServer>();
            commandPublisher.Start();

            app.MapGet("/", () => Results.Ok(new
            {
                name = "Zeron.Server",
                version = typeof(Program).Assembly.GetName().Version?.ToString()
            }));

            app.MapAgentEndpoints();
            app.MapTaskEndpoints();
            app.MapEventEndpoints();

            app.Lifetime.ApplicationStopping.Register(() => commandPublisher.Dispose());

            app.Run();
        }
    }
}
