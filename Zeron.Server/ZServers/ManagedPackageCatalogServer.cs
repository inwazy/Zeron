// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Microsoft.EntityFrameworkCore;
using System.Globalization;
using Zeron.Server.Data;
using Zeron.Server.Data.Entities;
using Zeron.ZCore;
using Zeron.ZCore.Type;

namespace Zeron.Server.ZServers
{
    /// <summary>
    /// ManagedPackageCatalogServer - central ManagedPackage catalog CRUD and agent sync.
    /// </summary>
    public class ManagedPackageCatalogServer
    {
        // Database context.
        private readonly ZeronServerDbContext m_DbContext;

        // Optional publisher for push catalog sync.
        private readonly CommandPublisherServer? m_CommandPublisher;

        /// <summary>
        /// ManagedPackageCatalogServer
        /// </summary>
        /// <param name="dbContext"></param>
        /// <param name="commandPublisher"></param>
        /// <returns>Returns void.</returns>
        public ManagedPackageCatalogServer(
            ZeronServerDbContext dbContext,
            CommandPublisherServer? commandPublisher = null)
        {
            m_DbContext = dbContext;
            m_CommandPublisher = commandPublisher;
        }

        /// <summary>
        /// GetPackagesAsync
        /// </summary>
        /// <param name="enabledOnly"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns package list.</returns>
        public async Task<List<ManagedPackageInfoType>> GetPackagesAsync(
            bool enabledOnly = false,
            CancellationToken cancellationToken = default)
        {
            IQueryable<ManagedPackageEntity> query = m_DbContext.ManagedPackages.AsQueryable();

            if (enabledOnly)
            {
                query = query.Where(package => package.IsEnabled);
            }

            List<ManagedPackageEntity> packages = await query
                .OrderBy(package => package.Name)
                .ToListAsync(cancellationToken);

            return packages.Select(ToInfo).ToList();
        }

        /// <summary>
        /// GetPackageAsync
        /// </summary>
        /// <param name="packageId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns package or null.</returns>
        public async Task<ManagedPackageInfoType?> GetPackageAsync(
            Guid packageId,
            CancellationToken cancellationToken = default)
        {
            ManagedPackageEntity? package = await m_DbContext.ManagedPackages
                .FirstOrDefaultAsync(item => item.Id == packageId, cancellationToken);

            return package == null ? null : ToInfo(package);
        }

        /// <summary>
        /// GetPackageByNameAsync
        /// </summary>
        /// <param name="name"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns package or null.</returns>
        public async Task<ManagedPackageInfoType?> GetPackageByNameAsync(
            string name,
            CancellationToken cancellationToken = default)
        {
            string normalized = NormalizeName(name);

            if (normalized.Length == 0)
            {
                return null;
            }

            ManagedPackageEntity? package = await m_DbContext.ManagedPackages
                .FirstOrDefaultAsync(item => item.Name == normalized, cancellationToken);

            return package == null ? null : ToInfo(package);
        }

        /// <summary>
        /// CreatePackageAsync
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns created package or error.</returns>
        public async Task<(ManagedPackageInfoType? Package, string? Error)> CreatePackageAsync(
            ManagedPackageUpsertRequestType request,
            CancellationToken cancellationToken = default)
        {
            string? error = ValidateRequest(request, requireName: true, out string name);

            if (error != null)
            {
                return (null, error);
            }

            bool exists = await m_DbContext.ManagedPackages
                .AnyAsync(package => package.Name == name, cancellationToken);

            if (exists)
            {
                return (null, "Package name already exists.");
            }

            DateTime now = DateTime.UtcNow;
            ManagedPackageEntity package = new()
            {
                Id = Guid.NewGuid(),
                Name = name,
                CreatedAt = now,
                UpdatedAt = now,
                IsEnabled = request.IsEnabled ?? true
            };

            ApplyFields(package, request);
            m_DbContext.ManagedPackages.Add(package);
            await m_DbContext.SaveChangesAsync(cancellationToken);
            await NotifyOnlineAgentsToSyncAsync(cancellationToken);

            ZNLogger.Common.Info(string.Format(CultureInfo.InvariantCulture,
                "ManagedPackageCatalogServer created package '{0}'.", package.Name));

            return (ToInfo(package), null);
        }

        /// <summary>
        /// UpdatePackageAsync
        /// </summary>
        /// <param name="packageId"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns updated package or error.</returns>
        public async Task<(ManagedPackageInfoType? Package, string? Error)> UpdatePackageAsync(
            Guid packageId,
            ManagedPackageUpsertRequestType request,
            CancellationToken cancellationToken = default)
        {
            ManagedPackageEntity? package = await m_DbContext.ManagedPackages
                .FirstOrDefaultAsync(item => item.Id == packageId, cancellationToken);

            if (package == null)
            {
                return (null, "Package not found.");
            }

            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                string? error = ValidateRequest(request, requireName: true, out string name);

                if (error != null)
                {
                    return (null, error);
                }

                bool nameTaken = await m_DbContext.ManagedPackages
                    .AnyAsync(item => item.Name == name && item.Id != packageId, cancellationToken);

                if (nameTaken)
                {
                    return (null, "Package name already exists.");
                }

                package.Name = name;
            }

            ApplyFields(package, request);

            if (request.IsEnabled.HasValue)
            {
                package.IsEnabled = request.IsEnabled.Value;
            }

            package.UpdatedAt = DateTime.UtcNow;
            await m_DbContext.SaveChangesAsync(cancellationToken);
            await NotifyOnlineAgentsToSyncAsync(cancellationToken);

            ZNLogger.Common.Info(string.Format(CultureInfo.InvariantCulture,
                "ManagedPackageCatalogServer updated package '{0}'.", package.Name));

            return (ToInfo(package), null);
        }

        /// <summary>
        /// DeletePackageAsync
        /// </summary>
        /// <param name="packageId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns error or null.</returns>
        public async Task<string?> DeletePackageAsync(
            Guid packageId,
            CancellationToken cancellationToken = default)
        {
            ManagedPackageEntity? package = await m_DbContext.ManagedPackages
                .FirstOrDefaultAsync(item => item.Id == packageId, cancellationToken);

            if (package == null)
            {
                return "Package not found.";
            }

            m_DbContext.ManagedPackages.Remove(package);
            await m_DbContext.SaveChangesAsync(cancellationToken);
            await NotifyOnlineAgentsToSyncAsync(cancellationToken);

            ZNLogger.Common.Info(string.Format(CultureInfo.InvariantCulture,
                "ManagedPackageCatalogServer deleted package '{0}'.", package.Name));

            return null;
        }

        /// <summary>
        /// GetCatalogSyncAsync - full catalog snapshot for Demand agents.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns sync payload.</returns>
        public async Task<ManagedPackageCatalogSyncResponseType> GetCatalogSyncAsync(
            CancellationToken cancellationToken = default)
        {
            List<ManagedPackageInfoType> packages = await GetPackagesAsync(
                enabledOnly: false,
                cancellationToken);

            return new ManagedPackageCatalogSyncResponseType
            {
                Success = true,
                GeneratedAt = DateTime.UtcNow,
                Packages = packages
            };
        }

        /// <summary>
        /// ValidateRequest
        /// </summary>
        /// <param name="request"></param>
        /// <param name="requireName"></param>
        /// <param name="name"></param>
        /// <returns>Returns error or null.</returns>
        private static string? ValidateRequest(
            ManagedPackageUpsertRequestType request,
            bool requireName,
            out string name)
        {
            name = NormalizeName(request.Name);

            if (requireName && name.Length == 0)
            {
                return "Package name is required.";
            }

            if (name.Contains(' '))
            {
                return "Package name cannot contain spaces.";
            }

            return null;
        }

        /// <summary>
        /// NormalizeName
        /// </summary>
        /// <param name="name"></param>
        /// <returns>Returns normalized name.</returns>
        private static string NormalizeName(
            string? name)
        {
            return string.IsNullOrWhiteSpace(name)
                ? ""
                : name.Trim().ToLowerInvariant();
        }

        /// <summary>
        /// ApplyFields
        /// </summary>
        /// <param name="package"></param>
        /// <param name="request"></param>
        /// <returns>Returns void.</returns>
        private static void ApplyFields(
            ManagedPackageEntity package,
            ManagedPackageUpsertRequestType request)
        {
            if (request.Urlx86 != null)
            {
                package.Urlx86 = request.Urlx86.Trim();
            }

            if (request.Urlx64 != null)
            {
                package.Urlx64 = request.Urlx64.Trim();
            }

            if (request.CmdInstallx86 != null)
            {
                package.CmdInstallx86 = request.CmdInstallx86.Trim();
            }

            if (request.CmdInstallx64 != null)
            {
                package.CmdInstallx64 = request.CmdInstallx64.Trim();
            }

            if (request.CmdUnInstallx86 != null)
            {
                package.CmdUnInstallx86 = request.CmdUnInstallx86.Trim();
            }

            if (request.CmdUnInstallx64 != null)
            {
                package.CmdUnInstallx64 = request.CmdUnInstallx64.Trim();
            }

            if (request.ScriptInstallBefore != null)
            {
                package.ScriptInstallBefore = request.ScriptInstallBefore;
            }

            if (request.ScriptInstallAfter != null)
            {
                package.ScriptInstallAfter = request.ScriptInstallAfter;
            }

            if (request.ScriptUnInstallBefore != null)
            {
                package.ScriptUnInstallBefore = request.ScriptUnInstallBefore;
            }

            if (request.ScriptUnInstallAfter != null)
            {
                package.ScriptUnInstallAfter = request.ScriptUnInstallAfter;
            }

            if (request.Sha256x86 != null)
            {
                package.Sha256x86 = NormalizeSha(request.Sha256x86);
            }

            if (request.Sha256x64 != null)
            {
                package.Sha256x64 = NormalizeSha(request.Sha256x64);
            }
        }

        /// <summary>
        /// NotifyOnlineAgentsToSyncAsync - push ManagedPackage sync to online agents.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns void.</returns>
        private async Task NotifyOnlineAgentsToSyncAsync(
            CancellationToken cancellationToken)
        {
            if (m_CommandPublisher == null)
            {
                return;
            }

            List<string> agentKeys = await m_DbContext.Agents
                .Where(agent => agent.Status == "online")
                .Select(agent => agent.AgentKey)
                .ToListAsync(cancellationToken);

            foreach (string agentKey in agentKeys)
            {
                m_CommandPublisher.PublishRemoteCommand(
                    agentKey,
                    Guid.Empty,
                    "ManagedPackage",
                    "sync");
            }

            if (agentKeys.Count > 0)
            {
                ZNLogger.Common.Info(string.Format(CultureInfo.InvariantCulture,
                    "ManagedPackageCatalogServer requested catalog sync on {0} online agent(s).",
                    agentKeys.Count));
            }
        }

        /// <summary>
        /// NormalizeSha
        /// </summary>
        /// <param name="sha"></param>
        /// <returns>Returns normalized sha or empty.</returns>
        private static string NormalizeSha(
            string? sha)
        {
            return string.IsNullOrWhiteSpace(sha)
                ? ""
                : sha.Trim().ToLowerInvariant();
        }

        /// <summary>
        /// ToInfo
        /// </summary>
        /// <param name="package"></param>
        /// <returns>Returns ManagedPackageInfoType.</returns>
        private static ManagedPackageInfoType ToInfo(
            ManagedPackageEntity package)
        {
            return new ManagedPackageInfoType
            {
                Id = package.Id.ToString(),
                Name = package.Name,
                Urlx86 = package.Urlx86,
                Urlx64 = package.Urlx64,
                CmdInstallx86 = package.CmdInstallx86,
                CmdInstallx64 = package.CmdInstallx64,
                CmdUnInstallx86 = package.CmdUnInstallx86,
                CmdUnInstallx64 = package.CmdUnInstallx64,
                ScriptInstallBefore = package.ScriptInstallBefore,
                ScriptInstallAfter = package.ScriptInstallAfter,
                ScriptUnInstallBefore = package.ScriptUnInstallBefore,
                ScriptUnInstallAfter = package.ScriptUnInstallAfter,
                Sha256x86 = package.Sha256x86,
                Sha256x64 = package.Sha256x64,
                IsEnabled = package.IsEnabled,
                UpdatedAt = package.UpdatedAt
            };
        }
    }
}
