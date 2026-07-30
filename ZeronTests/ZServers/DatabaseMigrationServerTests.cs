// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zeron.Server.Data;
using Zeron.Server.ZServers;

namespace Zeron.Server.ZServers.Tests
{
    [TestClass()]
    public class DatabaseMigrationServerTests
    {
        /// <summary>
        /// Legacy EnsureCreated database is baselined before MigrateAsync.
        /// </summary>
        [TestMethod()]
        public async Task MigrateAsyncBaselinesLegacyDatabaseTest()
        {
            string dbPath = Path.Combine(Path.GetTempPath(), "zeron-legacy-" + Guid.NewGuid().ToString("N") + ".db");

            try
            {
                await CreateLegacyDatabaseAsync(dbPath);

                DbContextOptions<ZeronServerDbContext> options = new DbContextOptionsBuilder<ZeronServerDbContext>()
                    .UseSqlite("Data Source=" + dbPath)
                    .Options;

                await using (ZeronServerDbContext dbContext = new(options))
                {
                    await DatabaseMigrationServer.MigrateAsync(dbContext);

                    IEnumerable<string> applied = await dbContext.Database.GetAppliedMigrationsAsync();

                    Assert.IsTrue(applied.Contains("20260730132711_InitialCreate"));
                }

                SqliteConnection.ClearAllPools();
            }
            finally
            {
                SqliteConnection.ClearAllPools();

                if (File.Exists(dbPath))
                {
                    try
                    {
                        File.Delete(dbPath);
                    }
                    catch (IOException)
                    {
                    }
                }
            }
        }

        private static async Task CreateLegacyDatabaseAsync(string dbPath)
        {
            await using SqliteConnection connection = new("Data Source=" + dbPath);
            await connection.OpenAsync();

            await using SqliteCommand command = connection.CreateCommand();
            command.CommandText = """
                CREATE TABLE "Agents" (
                    "Id" TEXT NOT NULL CONSTRAINT "PK_Agents" PRIMARY KEY,
                    "AgentKey" TEXT NOT NULL,
                    "MachineName" TEXT NULL,
                    "IpAddress" TEXT NULL,
                    "Version" TEXT NULL,
                    "Status" TEXT NOT NULL,
                    "RegisteredAt" TEXT NOT NULL,
                    "LastSeenAt" TEXT NOT NULL,
                    "LastHeartbeatAt" TEXT NOT NULL
                );
                """;

            await command.ExecuteNonQueryAsync();
        }
    }
}
