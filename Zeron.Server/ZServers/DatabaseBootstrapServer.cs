// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Microsoft.EntityFrameworkCore;
using Zeron.Server.Data;

namespace Zeron.Server.ZServers
{
    /// <summary>
    /// DatabaseBootstrapServer
    /// </summary>
    public static class DatabaseBootstrapServer
    {
        /// <summary>
        /// EnsureSchemaAsync
        /// </summary>
        /// <param name="dbContext"></param>
        /// <returns>Returns void.</returns>
        public static async Task EnsureSchemaAsync(ZeronServerDbContext dbContext)
        {
            dbContext.Database.EnsureCreated();

            await dbContext.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS Users (
                    Id TEXT NOT NULL CONSTRAINT PK_Users PRIMARY KEY,
                    Username TEXT NOT NULL,
                    PasswordHash TEXT NOT NULL,
                    Role TEXT NOT NULL,
                    IsActive INTEGER NOT NULL,
                    CreatedAt TEXT NOT NULL
                );
                CREATE UNIQUE INDEX IF NOT EXISTS IX_Users_Username ON Users (Username);

                CREATE TABLE IF NOT EXISTS Alerts (
                    Id TEXT NOT NULL CONSTRAINT PK_Alerts PRIMARY KEY,
                    RuleType TEXT NOT NULL,
                    AgentKey TEXT NULL,
                    AgentId TEXT NULL,
                    Title TEXT NOT NULL,
                    Message TEXT NOT NULL,
                    Severity TEXT NOT NULL,
                    Status TEXT NOT NULL,
                    CreatedAt TEXT NOT NULL,
                    ResolvedAt TEXT NULL,
                    NotifiedAt TEXT NULL
                );
                CREATE INDEX IF NOT EXISTS IX_Alerts_Status ON Alerts (Status);
                CREATE INDEX IF NOT EXISTS IX_Alerts_RuleType ON Alerts (RuleType);
                CREATE INDEX IF NOT EXISTS IX_Alerts_AgentKey ON Alerts (AgentKey);
                CREATE INDEX IF NOT EXISTS IX_Alerts_CreatedAt ON Alerts (CreatedAt);
            ");
        }
    }
}
