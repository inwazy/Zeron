// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text.Json;
using Zeron.Server.Data;
using Zeron.Server.Data.Entities;
using Zeron.ZCore;
using Zeron.ZCore.Type;

namespace Zeron.Server.ZServers
{
    /// <summary>
    /// PackageDeployServer - central ManagedPackage install/uninstall dispatch.
    /// </summary>
    public class PackageDeployServer
    {
        // TaskDispatcherServer is used to create a new task.
        private readonly TaskDispatcherServer m_TaskDispatcher;

        // ZeronServerDbContext is used to get the events.
        private readonly ZeronServerDbContext m_DbContext;

        // Optional catalog for package-name validation.
        private readonly ManagedPackageCatalogServer? m_CatalogServer;

        /// <summary>
        /// PackageDeployServer
        /// </summary>
        /// <param name="taskDispatcher"></param>
        /// <param name="dbContext"></param>
        /// <param name="catalogServer"></param>
        /// <returns>Returns void.</returns>
        public PackageDeployServer(
            TaskDispatcherServer taskDispatcher,
            ZeronServerDbContext dbContext,
            ManagedPackageCatalogServer? catalogServer = null)
        {
            m_TaskDispatcher = taskDispatcher;
            m_DbContext = dbContext;
            m_CatalogServer = catalogServer;
        }

        /// <summary>
        /// DeployAsync
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns deploy response.</returns>
        public async Task<PackageDeployResponseType> DeployAsync(
            PackageDeployRequestType request,
            CancellationToken cancellationToken = default)
        {
            string? error = ValidateRequest(request, out string operation, out string packageName, out string command);

            if (error != null)
            {
                return new PackageDeployResponseType
                {
                    Success = false,
                    Message = error
                };
            }

            if (m_CatalogServer != null)
            {
                ManagedPackageInfoType? catalogPackage = await m_CatalogServer.GetPackageByNameAsync(
                    packageName,
                    cancellationToken);

                if (catalogPackage == null)
                {
                    return new PackageDeployResponseType
                    {
                        Success = false,
                        Message = $"Package '{packageName}' was not found in the Server catalog."
                    };
                }

                if (!catalogPackage.IsEnabled)
                {
                    return new PackageDeployResponseType
                    {
                        Success = false,
                        Message = $"Package '{packageName}' is disabled in the Server catalog."
                    };
                }
            }

            string taskName = string.IsNullOrWhiteSpace(request.Name)
                ? $"deploy-{operation}-{packageName}-{DateTime.UtcNow:yyyyMMddHHmmss}"
                : request.Name.Trim();

            TaskEntity task = await m_TaskDispatcher.CreateTaskAsync(new TaskCreateRequestType
            {
                Name = taskName,
                Description = request.Description
                    ?? $"ManagedPackage {operation} '{packageName}'",
                TargetApi = "ManagedPackage",
                Command = command,
                TargetType = request.TargetType ?? "all",
                AgentIds = request.AgentIds,
                HostnamePattern = request.HostnamePattern
            }, cancellationToken);

            ZNLogger.Common.Info(string.Format(CultureInfo.InvariantCulture,
                "PackageDeployServer created task '{0}' for {1} {2}.", task.Name, operation, packageName));

            return new PackageDeployResponseType
            {
                Success = true,
                Message = "Deploy task created. Assignment status becomes running when queued, then completed/failed when install finishes.",
                TaskId = task.Id,
                Command = command,
                Operation = operation,
                PackageName = packageName
            };
        }

        /// <summary>
        /// GetRecentDeploysAsync
        /// </summary>
        /// <param name="limit"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns recent ManagedPackage tasks.</returns>
        public async Task<List<TaskEntity>> GetRecentDeploysAsync(
            int limit = 20,
            CancellationToken cancellationToken = default)
        {
            int take = Math.Clamp(limit, 1, 100);

            return await m_DbContext.Tasks
                .Include(task => task.Assignments)
                .Where(task => task.TargetApi == "ManagedPackage")
                .OrderByDescending(task => task.CreatedAt)
                .Take(take)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// GetInstallEventsAsync
        /// </summary>
        /// <param name="packageName"></param>
        /// <param name="agentKey"></param>
        /// <param name="limit"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns install.* events.</returns>
        public async Task<List<EventEntity>> GetInstallEventsAsync(
            string? packageName = null,
            string? agentKey = null,
            int limit = 50,
            CancellationToken cancellationToken = default)
        {
            int take = Math.Clamp(limit, 1, 200);

            IQueryable<EventEntity> query = m_DbContext.Events
                .Include(evt => evt.Agent)
                .Where(evt => evt.Topic.StartsWith("install."));

            if (!string.IsNullOrWhiteSpace(agentKey))
            {
                query = query.Where(evt => evt.Agent != null && evt.Agent.AgentKey == agentKey);
            }

            List<EventEntity> events = await query
                .OrderByDescending(evt => evt.ReceivedAt)
                .Take(take * 3)
                .ToListAsync(cancellationToken);

            if (!string.IsNullOrWhiteSpace(packageName))
            {
                events = events
                    .Where(evt => PayloadContainsPackage(evt.Payload, packageName))
                    .Take(take)
                    .ToList();
            }
            else if (events.Count > take)
            {
                events = events.Take(take).ToList();
            }

            return events;
        }

        /// <summary>
        /// BuildCommand
        /// </summary>
        /// <param name="operation"></param>
        /// <param name="packageName"></param>
        /// <param name="extraArgs"></param>
        /// <returns>Returns ManagedPackage command string.</returns>
        public static string BuildCommand(
            string operation, 
            string packageName, 
            string? extraArgs)
        {
            string command = operation + " " + packageName;

            if (!string.IsNullOrWhiteSpace(extraArgs))
            {
                command += " " + extraArgs.Trim();
            }

            return command;
        }

        /// <summary>
        /// ValidateRequest
        /// </summary>
        /// <param name="request"></param>
        /// <param name="operation"></param>
        /// <param name="packageName"></param>
        /// <param name="command"></param>
        /// <returns>Returns error or null.</returns>
        private static string? ValidateRequest(
            PackageDeployRequestType request,
            out string operation,
            out string packageName,
            out string command)
        {
            operation = "";
            packageName = "";
            command = "";

            if (string.IsNullOrWhiteSpace(request.Operation))
            {
                return "Operation is required (install or uninstall).";
            }

            operation = request.Operation.Trim().ToLowerInvariant();

            if (operation is not "install" and not "uninstall")
            {
                return "Operation must be install or uninstall.";
            }

            if (string.IsNullOrWhiteSpace(request.PackageName))
            {
                return "Package name is required.";
            }

            packageName = request.PackageName.Trim();

            if (packageName.Contains(' '))
            {
                return "Package name cannot contain spaces.";
            }

            command = BuildCommand(operation, packageName, request.ExtraArgs);

            return null;
        }

        /// <summary>
        /// PayloadContainsPackage
        /// </summary>
        /// <param name="payload"></param>
        /// <param name="packageName"></param>
        /// <returns>Returns bool.</returns>
        private static bool PayloadContainsPackage(
            string? payload, 
            string packageName)
        {
            if (string.IsNullOrWhiteSpace(payload))
            {
                return false;
            }

            try
            {
                using JsonDocument document = JsonDocument.Parse(payload);

                if (document.RootElement.TryGetProperty("package", out JsonElement packageElement))
                {
                    return string.Equals(
                        packageElement.GetString(),
                        packageName,
                        StringComparison.OrdinalIgnoreCase);
                }
            }
            catch (JsonException)
            {
            }

            return payload.Contains(packageName, StringComparison.OrdinalIgnoreCase);
        }
    }
}
