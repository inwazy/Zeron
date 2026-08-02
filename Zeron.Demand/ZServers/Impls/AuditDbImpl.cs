// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Microsoft.Data.Sqlite;
using System.Data;
using System.Globalization;
using Zeron.Demand.ZCore.Type;
using Zeron.ZCore;
using Zeron.ZInterfaces;

namespace Zeron.Demand.ZServers.Impls
{
    /// <summary>
    /// AuditDbImpl
    /// </summary>
    internal class AuditDbImpl : IImpl
    {
        // SQLite database connection.
        private static SqliteConnection? s_DbConnection;

        // Data source string.
        private static string? s_DataSource;

        /// <summary>
        /// Dispose
        /// </summary>
        /// <returns>Returns void.</returns>
        public void Dispose()
        {
            if (s_DbConnection != null)
            {
                if (s_DbConnection.State is ConnectionState.Open or ConnectionState.Broken)
                {
                    s_DbConnection.Close();
                    s_DbConnection.Dispose();
                }
            }
        }

        /// <summary>
        /// PrepareDatabase
        /// </summary>
        /// <param name="dataSource"></param>
        /// <returns>Returns void.</returns>
        public void PrepareDatabase(
            string? dataSource)
        {
            string dbPath = ResolveDatabasePath(dataSource);
            string? directory = Path.GetDirectoryName(dbPath);

            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            s_DataSource = "Data Source=" + dbPath;
            s_DbConnection = new SqliteConnection(s_DataSource);
            s_DbConnection.Open();
            EnsureSchema();
        }

        /// <summary>
        /// Insert
        /// </summary>
        /// <param name="agentId"></param>
        /// <param name="apiName"></param>
        /// <param name="command"></param>
        /// <param name="success"></param>
        /// <param name="message"></param>
        /// <param name="source"></param>
        /// <returns>Returns void.</returns>
        public static void Insert(
            string? agentId, 
            string? apiName, 
            string? command, 
            bool success, 
            string? message, 
            string? source)
        {
            if (s_DbConnection == null)
            {
                return;
            }

            try
            {
                using SqliteCommand cmd = s_DbConnection.CreateCommand();
                cmd.CommandText = """
                    INSERT INTO audit_log (agent_id, api_name, command, success, message, source, created_at)
                    VALUES (@agent_id, @api_name, @command, @success, @message, @source, @created_at);
                    """;
                cmd.Parameters.AddWithValue("@agent_id", agentId ?? "");
                cmd.Parameters.AddWithValue("@api_name", apiName ?? "");
                cmd.Parameters.AddWithValue("@command", command ?? "");
                cmd.Parameters.AddWithValue("@success", success ? 1 : 0);
                cmd.Parameters.AddWithValue("@message", message ?? "");
                cmd.Parameters.AddWithValue("@source", source ?? "rep");
                cmd.Parameters.AddWithValue("@created_at", DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture));
                cmd.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "AuditDbImpl Insert Error:{0}\n{1}", e.Message, e.StackTrace));
            }
        }

        /// <summary>
        /// QueryRecent
        /// </summary>
        /// <param name="limit"></param>
        /// <returns>Returns audit rows.</returns>
        public static List<AuditLogEntryType> QueryRecent(
            int limit)
        {
            List<AuditLogEntryType> results = [];

            if (s_DbConnection == null)
            {
                return results;
            }

            try
            {
                using SqliteCommand cmd = s_DbConnection.CreateCommand();
                cmd.CommandText = """
                    SELECT id, agent_id, api_name, command, success, message, source, created_at
                    FROM audit_log
                    ORDER BY id DESC
                    LIMIT @limit;
                    """;
                cmd.Parameters.AddWithValue("@limit", limit);

                using SqliteDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    results.Add(new AuditLogEntryType
                    {
                        Id = reader.GetInt64(0),
                        AgentId = reader.GetString(1),
                        ApiName = reader.GetString(2),
                        Command = reader.GetString(3),
                        Success = reader.GetInt32(4) == 1,
                        Message = reader.GetString(5),
                        Source = reader.GetString(6),
                        CreatedAt = reader.GetString(7)
                    });
                }
            }
            catch (Exception e)
            {
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "AuditDbImpl QueryRecent Error:{0}\n{1}", e.Message, e.StackTrace));
            }

            return results;
        }

        /// <summary>
        /// EnsureSchema
        /// </summary>
        /// <returns>Returns void.</returns>
        private static void EnsureSchema()
        {
            if (s_DbConnection == null)
            {
                return;
            }

            using SqliteCommand cmd = s_DbConnection.CreateCommand();
            cmd.CommandText = """
                CREATE TABLE IF NOT EXISTS audit_log (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    agent_id TEXT NOT NULL,
                    api_name TEXT NOT NULL,
                    command TEXT,
                    success INTEGER NOT NULL,
                    message TEXT,
                    source TEXT,
                    created_at TEXT NOT NULL
                );
                """;
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// ResolveDatabasePath
        /// </summary>
        /// <param name="dataSource"></param>
        /// <returns>Returns file path.</returns>
        private static string ResolveDatabasePath(
            string? dataSource)
        {
            string relativePath = string.IsNullOrWhiteSpace(dataSource) ? "Resource/audit.db" : dataSource;

            return Path.IsPathRooted(relativePath)
                ? relativePath
                : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath);
        }
    }
}
