// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Data;
using System.Globalization;
using Zeron.Server.Data;
using Zeron.ZCore;

namespace Zeron.Server.ZServers
{
    /// <summary>
    /// DatabaseMigrationServer
    /// </summary>
    public static class DatabaseMigrationServer
    {
        // TODO: Update to the latest migration ID.
        private const string InitialMigrationId = "20260730132711_InitialCreate";

        /// <summary>
        /// MigrateAsync
        /// </summary>
        /// <param name="dbContext"></param>
        /// <returns>Returns void.</returns>
        public static async Task MigrateAsync(ZeronServerDbContext dbContext)
        {
            if (await ShouldBaselineLegacyDatabaseAsync(dbContext))
            {
                await BaselineLegacyDatabaseAsync(dbContext);

                ZNLogger.Common.Info(string.Format(CultureInfo.InvariantCulture,
                    "DatabaseMigrationServer baselined legacy database at '{0}'.",
                    dbContext.Database.GetConnectionString()));
            }

            await dbContext.Database.MigrateAsync();
        }

        /// <summary>
        /// ShouldBaselineLegacyDatabaseAsync
        /// </summary>
        /// <param name="dbContext"></param>
        /// <returns>Returns bool.</returns>
        internal static async Task<bool> ShouldBaselineLegacyDatabaseAsync(ZeronServerDbContext dbContext)
        {
            if (!await dbContext.Database.CanConnectAsync())
            {
                return false;
            }

            IEnumerable<string> appliedMigrations = await dbContext.Database.GetAppliedMigrationsAsync();

            if (appliedMigrations.Any())
            {
                return false;
            }

            return await TableExistsAsync(dbContext, "Agents");
        }

        /// <summary>
        /// BaselineLegacyDatabaseAsync
        /// </summary>
        /// <param name="dbContext"></param>
        /// <returns>Returns void.</returns>
        internal static async Task BaselineLegacyDatabaseAsync(ZeronServerDbContext dbContext)
        {
            await dbContext.Database.ExecuteSqlRawAsync("""
                CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
                    "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
                    "ProductVersion" TEXT NOT NULL
                );
                """);

            string productVersion = ProductInfo.GetVersion();

            await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT OR IGNORE INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
                VALUES ({InitialMigrationId}, {productVersion});
                """);
        }

        /// <summary>
        /// TableExistsAsync
        /// </summary>
        /// <param name="dbContext"></param>
        /// <param name="tableName"></param>
        /// <returns>Returns bool.</returns>
        internal static async Task<bool> TableExistsAsync(ZeronServerDbContext dbContext, string tableName)
        {
            System.Data.Common.DbConnection connection = dbContext.Database.GetDbConnection();
            bool wasClosed = connection.State == ConnectionState.Closed;

            if (wasClosed)
            {
                await connection.OpenAsync();
            }

            try
            {
                await using System.Data.Common.DbCommand command = connection.CreateCommand();
                command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = $tableName";

                System.Data.Common.DbParameter parameter = command.CreateParameter();
                parameter.ParameterName = "$tableName";
                parameter.Value = tableName;
                command.Parameters.Add(parameter);

                object? result = await command.ExecuteScalarAsync();

                return Convert.ToInt32(result, CultureInfo.InvariantCulture) > 0;
            }
            finally
            {
                if (wasClosed && connection.State == ConnectionState.Open)
                {
                    await connection.CloseAsync();
                }
            }
        }
    }
}
