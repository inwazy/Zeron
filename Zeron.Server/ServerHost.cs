// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NLog;
using NLog.Extensions.Logging;
using System.Text;
using System.Text.Json.Serialization;
using Zeron.Server.Background;
using Zeron.Server.Components;
using Zeron.Server.Data;
using Zeron.Server.Endpoints;
using Zeron.Server.Hubs;
using Zeron.Server.Middleware;
using Zeron.Server.ZCore;
using Zeron.Server.ZInterfaces;
using Zeron.Server.ZServers;

namespace Zeron.Server
{
    /// <summary>
    /// ServerHost
    /// </summary>
    public static class ServerHost
    {
        /// <summary>
        /// BuildApplication
        /// </summary>
        /// <param name="args"></param>
        /// <returns>Returns WebApplication.</returns>
        public static WebApplication BuildApplication(
            string[] args)
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

            LogManager.Setup().LoadConfigurationFromFile("NLog.config");
            builder.Logging.ClearProviders();
            builder.Logging.AddNLog();

            ServerSettings serverSettings = builder.Configuration
                .GetSection(ServerSettings.SectionName)
                .Get<ServerSettings>() ?? new ServerSettings();

            string dbPath = ResolveDatabasePath(serverSettings.DatabasePath);

            builder.Services.AddSingleton(serverSettings);
            builder.Services.ConfigureHttpJsonOptions(options =>
            {
                options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            });
            builder.Services.AddDbContext<ZeronServerDbContext>(options => options.UseSqlite("Data Source=" + dbPath));
            builder.Services.AddSingleton<JwtTokenServer>();
            builder.Services.AddScoped<AuthServer>();
            builder.Services.AddScoped<UserManagerServer>();
            builder.Services.AddScoped<AgentManagerServer>();
            builder.Services.AddScoped<AgentDiagnosticServer>();
            builder.Services.AddScoped<TaskDispatcherServer>();
            builder.Services.AddScoped<EventIngestorServer>();
            builder.Services.AddScoped<AlertNotifierServer>();
            builder.Services.AddScoped<AlertRuleServer>();
            builder.Services.AddScoped<DashboardSummaryServer>();
            builder.Services.AddSingleton<CommandPublisherServer>();
            builder.Services.AddSingleton<IDashboardNotifier, DashboardNotifierServer>();
            builder.Services.AddHttpContextAccessor();

            if (!builder.Environment.IsEnvironment("Testing"))
            {
                builder.Services.AddHostedService<HeartbeatMonitorWorker>();
                builder.Services.AddHostedService<TaskDispatchWorker>();
            }

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddCookie(options =>
            {
                options.LoginPath = "/login";
                options.LogoutPath = "/account/logout";
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = serverSettings.JwtIssuer,
                    ValidAudience = serverSettings.JwtIssuer,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(serverSettings.JwtSecret))
                };
            });

            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy(ServerPolicies.ViewerOrAbove, policy =>
                    policy.RequireRole(ServerRoles.Admin, ServerRoles.Operator, ServerRoles.Viewer));
                options.AddPolicy(ServerPolicies.OperatorOrAbove, policy =>
                    policy.RequireRole(ServerRoles.Admin, ServerRoles.Operator));
                options.AddPolicy(ServerPolicies.AdminOnly, policy =>
                    policy.RequireRole(ServerRoles.Admin));
            });

            builder.Services.AddCascadingAuthenticationState();
            var razorComponents = builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            if (builder.Environment.IsDevelopment())
            {
                razorComponents.AddCircuitOptions(options => options.DetailedErrors = true);
            }

            builder.Services.AddSignalR();

            WebApplication app = builder.Build();

            using (IServiceScope scope = app.Services.CreateScope())
            {
                ZeronServerDbContext dbContext = scope.ServiceProvider.GetRequiredService<ZeronServerDbContext>();
                DatabaseMigrationServer.MigrateAsync(dbContext).GetAwaiter().GetResult();

                AuthServer authServer = scope.ServiceProvider.GetRequiredService<AuthServer>();
                authServer.SeedDefaultUserAsync().GetAwaiter().GetResult();
            }

            if (!app.Environment.IsEnvironment("Testing"))
            {
                CommandPublisherServer commandPublisher = app.Services.GetRequiredService<CommandPublisherServer>();
                commandPublisher.Start();
                app.Lifetime.ApplicationStopping.Register(() => commandPublisher.Dispose());
            }

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
            }

            app.UseStaticFiles();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseMiddleware<MustChangePasswordMiddleware>();
            app.UseAntiforgery();

            app.MapGet("/api", () => Results.Ok(new
            {
                name = "Zeron.Server",
                version = typeof(ServerHost).Assembly.GetName().Version?.ToString()
            })).AllowAnonymous();

            app.MapHealthEndpoints();
            app.MapAuthEndpoints();
            app.MapDashboardEndpoints();
            app.MapAgentEndpoints();
            app.MapTaskEndpoints();
            app.MapEventEndpoints();
            app.MapAlertEndpoints();
            app.MapUserEndpoints();
            app.MapHub<DashboardHub>("/hubs/dashboard");

            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            return app;
        }

        /// <summary>
        /// ResolveDatabasePath
        /// </summary>
        /// <param name="databasePath"></param>
        /// <returns>Returns absolute database path.</returns>
        public static string ResolveDatabasePath(
            string databasePath)
        {
            string dbPath = Path.IsPathRooted(databasePath)
                ? databasePath
                : Path.Combine(AppContext.BaseDirectory, databasePath);

            string? dbDirectory = Path.GetDirectoryName(dbPath);

            if (!string.IsNullOrEmpty(dbDirectory))
            {
                Directory.CreateDirectory(dbDirectory);
            }

            return dbPath;
        }
    }
}
