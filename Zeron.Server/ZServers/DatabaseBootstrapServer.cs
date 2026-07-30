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
            ");
        }
    }
}
