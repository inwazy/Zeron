// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Microsoft.EntityFrameworkCore;
using System.Globalization;
using Zeron.Server.Data;
using Zeron.Server.Data.Entities;
using Zeron.Server.ZCore;
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

        // Optional audit log.
        private readonly AuditLogServer? m_AuditLogServer;

        /// <summary>
        /// ManagedPackageCatalogServer
        /// </summary>
        /// <param name="dbContext"></param>
        /// <param name="commandPublisher"></param>
        /// <param name="auditLogServer"></param>
        /// <returns>Returns void.</returns>
        public ManagedPackageCatalogServer(
            ZeronServerDbContext dbContext,
            CommandPublisherServer? commandPublisher = null,
            AuditLogServer? auditLogServer = null)
        {
            m_DbContext = dbContext;
            m_CommandPublisher = commandPublisher;
            m_AuditLogServer = auditLogServer;
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
            CancellationToken cancellationToken = default,
            AuditActorType? actor = null)
        {
            string? error = ValidateRequest(request, requireName: true, out string name);

            if (error != null)
            {
                await WriteCatalogAuditAsync(AuditActions.CatalogCreate, false, error, name, actor, cancellationToken);
                return (null, error);
            }

            bool exists = await m_DbContext.ManagedPackages
                .AnyAsync(package => package.Name == name, cancellationToken);

            if (exists)
            {
                string existsError = "Package name already exists.";
                await WriteCatalogAuditAsync(AuditActions.CatalogCreate, false, existsError, name, actor, cancellationToken);
                return (null, existsError);
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
            
            package.ScriptEngine = NormalizeScriptEngine(package.ScriptEngine);
            m_DbContext.ManagedPackages.Add(package);
            m_DbContext.ManagedPackageVersions.Add(BuildVersionEntity(
                package,
                versionNumber: 1,
                changeKind: "create",
                actor,
                restoredFromVersion: null));
            await m_DbContext.SaveChangesAsync(cancellationToken);
            await NotifyOnlineAgentsToSyncAsync(cancellationToken);

            ZNLogger.Common.Info(string.Format(CultureInfo.InvariantCulture,
                "ManagedPackageCatalogServer created package '{0}'.", package.Name));

            await WriteCatalogAuditAsync(
                AuditActions.CatalogCreate,
                true,
                $"Created catalog package '{package.Name}'.",
                package.Name,
                actor,
                cancellationToken,
                new { package.Id, package.IsEnabled });

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
            CancellationToken cancellationToken = default,
            AuditActorType? actor = null)
        {
            ManagedPackageEntity? package = await m_DbContext.ManagedPackages
                .FirstOrDefaultAsync(item => item.Id == packageId, cancellationToken);

            if (package == null)
            {
                await WriteCatalogAuditAsync(AuditActions.CatalogUpdate, false, "Package not found.", null, actor, cancellationToken);
                return (null, "Package not found.");
            }

            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                string? error = ValidateRequest(request, requireName: true, out string name);

                if (error != null)
                {
                    await WriteCatalogAuditAsync(AuditActions.CatalogUpdate, false, error, package.Name, actor, cancellationToken);
                    return (null, error);
                }

                bool nameTaken = await m_DbContext.ManagedPackages
                    .AnyAsync(item => item.Name == name && item.Id != packageId, cancellationToken);

                if (nameTaken)
                {
                    string taken = "Package name already exists.";
                    await WriteCatalogAuditAsync(AuditActions.CatalogUpdate, false, taken, name, actor, cancellationToken);
                    return (null, taken);
                }

                package.Name = name;
            }

            ApplyFields(package, request);

            if (request.IsEnabled.HasValue)
            {
                package.IsEnabled = request.IsEnabled.Value;
            }

            package.ScriptEngine = NormalizeScriptEngine(package.ScriptEngine);
            package.UpdatedAt = DateTime.UtcNow;
            int nextVersion = await GetNextVersionNumberAsync(packageId, cancellationToken);
            m_DbContext.ManagedPackageVersions.Add(BuildVersionEntity(
                package,
                nextVersion,
                "update",
                actor,
                restoredFromVersion: null));
            await m_DbContext.SaveChangesAsync(cancellationToken);
            await NotifyOnlineAgentsToSyncAsync(cancellationToken);

            ZNLogger.Common.Info(string.Format(CultureInfo.InvariantCulture,
                "ManagedPackageCatalogServer updated package '{0}'.", package.Name));

            await WriteCatalogAuditAsync(
                AuditActions.CatalogUpdate,
                true,
                $"Updated catalog package '{package.Name}'.",
                package.Name,
                actor,
                cancellationToken,
                new { package.Id, package.IsEnabled });

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
            CancellationToken cancellationToken = default,
            AuditActorType? actor = null)
        {
            ManagedPackageEntity? package = await m_DbContext.ManagedPackages
                .FirstOrDefaultAsync(item => item.Id == packageId, cancellationToken);

            if (package == null)
            {
                await WriteCatalogAuditAsync(AuditActions.CatalogDelete, false, "Package not found.", null, actor, cancellationToken);
                return "Package not found.";
            }

            string packageName = package.Name;
            m_DbContext.ManagedPackages.Remove(package);
            await m_DbContext.SaveChangesAsync(cancellationToken);
            await NotifyOnlineAgentsToSyncAsync(cancellationToken);

            ZNLogger.Common.Info(string.Format(CultureInfo.InvariantCulture,
                "ManagedPackageCatalogServer deleted package '{0}'.", packageName));

            await WriteCatalogAuditAsync(
                AuditActions.CatalogDelete,
                true,
                $"Deleted catalog package '{packageName}'.",
                packageName,
                actor,
                cancellationToken,
                new { packageId });

            return null;
        }

        /// <summary>
        /// GetPackageVersionsAsync
        /// </summary>
        /// <param name="packageId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns version list (newest first).</returns>
        public async Task<List<ManagedPackageVersionInfoType>> GetPackageVersionsAsync(
            Guid packageId,
            CancellationToken cancellationToken = default)
        {
            bool exists = await m_DbContext.ManagedPackages
                .AnyAsync(item => item.Id == packageId, cancellationToken);

            if (!exists)
            {
                return [];
            }

            List<ManagedPackageVersionEntity> versions = await m_DbContext.ManagedPackageVersions
                .AsNoTracking()
                .Where(item => item.PackageId == packageId)
                .OrderByDescending(item => item.VersionNumber)
                .ToListAsync(cancellationToken);

            return versions.Select(ToVersionInfo).ToList();
        }

        /// <summary>
        /// GetPackageVersionAsync
        /// </summary>
        /// <param name="packageId"></param>
        /// <param name="versionNumber"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns version or null.</returns>
        public async Task<ManagedPackageVersionInfoType?> GetPackageVersionAsync(
            Guid packageId,
            int versionNumber,
            CancellationToken cancellationToken = default)
        {
            ManagedPackageVersionEntity? version = await m_DbContext.ManagedPackageVersions
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    item => item.PackageId == packageId && item.VersionNumber == versionNumber,
                    cancellationToken);

            return version == null ? null : ToVersionInfo(version);
        }

        /// <summary>
        /// RollbackPackageAsync - restore a historical snapshot and push catalog sync.
        /// </summary>
        /// <param name="packageId"></param>
        /// <param name="versionNumber"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="actor"></param>
        /// <returns>Returns restored package or error.</returns>
        public async Task<(ManagedPackageInfoType? Package, string? Error)> RollbackPackageAsync(
            Guid packageId,
            int versionNumber,
            CancellationToken cancellationToken = default,
            AuditActorType? actor = null)
        {
            ManagedPackageEntity? package = await m_DbContext.ManagedPackages
                .FirstOrDefaultAsync(item => item.Id == packageId, cancellationToken);

            if (package == null)
            {
                await WriteCatalogAuditAsync(
                    AuditActions.CatalogRollback,
                    false,
                    "Package not found.",
                    null,
                    actor,
                    cancellationToken);
                return (null, "Package not found.");
            }

            ManagedPackageVersionEntity? source = await m_DbContext.ManagedPackageVersions
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    item => item.PackageId == packageId && item.VersionNumber == versionNumber,
                    cancellationToken);

            if (source == null)
            {
                await WriteCatalogAuditAsync(
                    AuditActions.CatalogRollback,
                    false,
                    "Version not found.",
                    package.Name,
                    actor,
                    cancellationToken,
                    new { packageId, versionNumber });
                return (null, "Version not found.");
            }

            string restoredName = NormalizeName(source.Name);

            if (restoredName.Length == 0 || restoredName.Contains(' '))
            {
                string invalid = "Stored version name is invalid.";
                await WriteCatalogAuditAsync(
                    AuditActions.CatalogRollback,
                    false,
                    invalid,
                    package.Name,
                    actor,
                    cancellationToken);
                return (null, invalid);
            }

            bool nameTaken = await m_DbContext.ManagedPackages
                .AnyAsync(item => item.Name == restoredName && item.Id != packageId, cancellationToken);

            if (nameTaken)
            {
                string taken = "Package name already exists.";
                await WriteCatalogAuditAsync(
                    AuditActions.CatalogRollback,
                    false,
                    taken,
                    restoredName,
                    actor,
                    cancellationToken,
                    new { packageId, versionNumber });
                return (null, taken);
            }

            ApplySnapshot(package, source);
            package.UpdatedAt = DateTime.UtcNow;

            int nextVersion = await GetNextVersionNumberAsync(packageId, cancellationToken);
            m_DbContext.ManagedPackageVersions.Add(BuildVersionEntity(
                package,
                nextVersion,
                "rollback",
                actor,
                restoredFromVersion: versionNumber));

            await m_DbContext.SaveChangesAsync(cancellationToken);
            await NotifyOnlineAgentsToSyncAsync(cancellationToken);

            ZNLogger.Common.Info(string.Format(CultureInfo.InvariantCulture,
                "ManagedPackageCatalogServer rolled package '{0}' back to version {1} (new version {2}).",
                package.Name,
                versionNumber,
                nextVersion));

            await WriteCatalogAuditAsync(
                AuditActions.CatalogRollback,
                true,
                $"Rolled catalog package '{package.Name}' back to version {versionNumber}.",
                package.Name,
                actor,
                cancellationToken,
                new { package.Id, versionNumber, newVersion = nextVersion, package.IsEnabled });

            return (ToInfo(package), null);
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

            if (request.ScriptEngine != null)
            {
                package.ScriptEngine = NormalizeScriptEngine(request.ScriptEngine);
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
        /// WriteCatalogAuditAsync
        /// </summary>
        private async Task WriteCatalogAuditAsync(
            string action,
            bool success,
            string summary,
            string? packageName,
            AuditActorType? actor,
            CancellationToken cancellationToken,
            object? details = null)
        {
            if (m_AuditLogServer == null || actor == null)
            {
                return;
            }

            await m_AuditLogServer.WriteAsync(
                action,
                success,
                summary,
                actor,
                targetType: "package",
                targetKey: packageName,
                details: details,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// NotifyOnlineAgentsToSyncAsync - push ManagedPackage sync to online agents.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns void.</returns>
        private async Task NotifyOnlineAgentsToSyncAsync(
            CancellationToken cancellationToken)
        {
            await RequestCatalogSyncAsync(agentKeys: null, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// RequestCatalogSyncAsync - push ManagedPackage sync to selected or all online agents.
        /// </summary>
        /// <param name="agentKeys"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns pushed agent keys.</returns>
        public async Task<List<string>> RequestCatalogSyncAsync(
            IEnumerable<string>? agentKeys = null,
            CancellationToken cancellationToken = default)
        {
            if (m_CommandPublisher == null)
            {
                return [];
            }

            List<string> targets;

            if (agentKeys == null)
            {
                targets = await m_DbContext.Agents
                    .Where(agent => agent.Status == "online")
                    .Select(agent => agent.AgentKey)
                    .ToListAsync(cancellationToken);
            }
            else
            {
                HashSet<string> requested = new(
                    agentKeys.Where(key => !string.IsNullOrWhiteSpace(key)).Select(key => key.Trim()),
                    StringComparer.OrdinalIgnoreCase);

                if (requested.Count == 0)
                {
                    return [];
                }

                targets = await m_DbContext.Agents
                    .Where(agent => agent.Status == "online" && requested.Contains(agent.AgentKey))
                    .Select(agent => agent.AgentKey)
                    .ToListAsync(cancellationToken);
            }

            foreach (string agentKey in targets)
            {
                m_CommandPublisher.PublishRemoteCommand(
                    agentKey,
                    Guid.Empty,
                    "ManagedPackage",
                    "sync");
            }

            if (targets.Count > 0)
            {
                ZNLogger.Common.Info(string.Format(CultureInfo.InvariantCulture,
                    "ManagedPackageCatalogServer requested catalog sync on {0} online agent(s).",
                    targets.Count));
            }

            return targets;
        }

        /// <summary>
        /// GetNextVersionNumberAsync
        /// </summary>
        private async Task<int> GetNextVersionNumberAsync(
            Guid packageId,
            CancellationToken cancellationToken)
        {
            int? max = await m_DbContext.ManagedPackageVersions
                .Where(item => item.PackageId == packageId)
                .MaxAsync(item => (int?)item.VersionNumber, cancellationToken);

            return (max ?? 0) + 1;
        }

        /// <summary>
        /// BuildVersionEntity
        /// </summary>
        private static ManagedPackageVersionEntity BuildVersionEntity(
            ManagedPackageEntity package,
            int versionNumber,
            string changeKind,
            AuditActorType? actor,
            int? restoredFromVersion)
        {
            return new ManagedPackageVersionEntity
            {
                Id = Guid.NewGuid(),
                PackageId = package.Id,
                VersionNumber = versionNumber,
                CreatedAt = DateTime.UtcNow,
                ChangeKind = changeKind,
                ActorUsername = actor?.Username,
                RestoredFromVersion = restoredFromVersion,
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
                ScriptEngine = NormalizeScriptEngine(package.ScriptEngine),
                Sha256x86 = package.Sha256x86,
                Sha256x64 = package.Sha256x64,
                IsEnabled = package.IsEnabled
            };
        }

        /// <summary>
        /// ApplySnapshot - full replace of live package fields from a version row.
        /// </summary>
        private static void ApplySnapshot(
            ManagedPackageEntity package,
            ManagedPackageVersionEntity source)
        {
            package.Name = NormalizeName(source.Name);
            package.Urlx86 = source.Urlx86;
            package.Urlx64 = source.Urlx64;
            package.CmdInstallx86 = source.CmdInstallx86;
            package.CmdInstallx64 = source.CmdInstallx64;
            package.CmdUnInstallx86 = source.CmdUnInstallx86;
            package.CmdUnInstallx64 = source.CmdUnInstallx64;
            package.ScriptInstallBefore = source.ScriptInstallBefore;
            package.ScriptInstallAfter = source.ScriptInstallAfter;
            package.ScriptUnInstallBefore = source.ScriptUnInstallBefore;
            package.ScriptUnInstallAfter = source.ScriptUnInstallAfter;
            package.ScriptEngine = NormalizeScriptEngine(source.ScriptEngine);
            package.Sha256x86 = source.Sha256x86;
            package.Sha256x64 = source.Sha256x64;
            package.IsEnabled = source.IsEnabled;
        }

        /// <summary>
        /// NormalizeScriptEngine
        /// </summary>
        /// <param name="engine"></param>
        /// <returns>Returns normalized engine id.</returns>
        internal static string NormalizeScriptEngine(
            string? engine)
        {
            return string.IsNullOrWhiteSpace(engine)
                ? "powershell"
                : engine.Trim().ToLowerInvariant();
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
                ScriptEngine = NormalizeScriptEngine(package.ScriptEngine),
                Sha256x86 = package.Sha256x86,
                Sha256x64 = package.Sha256x64,
                IsEnabled = package.IsEnabled,
                UpdatedAt = package.UpdatedAt
            };
        }

        /// <summary>
        /// ToVersionInfo
        /// </summary>
        private static ManagedPackageVersionInfoType ToVersionInfo(
            ManagedPackageVersionEntity version)
        {
            return new ManagedPackageVersionInfoType
            {
                Id = version.Id.ToString(),
                PackageId = version.PackageId.ToString(),
                VersionNumber = version.VersionNumber,
                CreatedAt = version.CreatedAt,
                ChangeKind = version.ChangeKind,
                ActorUsername = version.ActorUsername,
                RestoredFromVersion = version.RestoredFromVersion,
                Name = version.Name,
                Urlx86 = version.Urlx86,
                Urlx64 = version.Urlx64,
                CmdInstallx86 = version.CmdInstallx86,
                CmdInstallx64 = version.CmdInstallx64,
                CmdUnInstallx86 = version.CmdUnInstallx86,
                CmdUnInstallx64 = version.CmdUnInstallx64,
                ScriptInstallBefore = version.ScriptInstallBefore,
                ScriptInstallAfter = version.ScriptInstallAfter,
                ScriptUnInstallBefore = version.ScriptUnInstallBefore,
                ScriptUnInstallAfter = version.ScriptUnInstallAfter,
                ScriptEngine = NormalizeScriptEngine(version.ScriptEngine),
                Sha256x86 = version.Sha256x86,
                Sha256x64 = version.Sha256x64,
                IsEnabled = version.IsEnabled
            };
        }
    }
}
