// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Microsoft.Data.Sqlite;
using System.Data;
using System.Globalization;
using Zeron.Demand.ZCore;
using Zeron.Demand.ZCore.Type;
using Zeron.ZCore;
using Zeron.ZCore.Type;
using Zeron.ZInterfaces;
using Zeron.ZServers;

namespace Zeron.Demand.ZServers.Impls
{
    /// <summary>
    /// ManagedPackageDbImpl
    /// </summary>
    internal class ManagedPackageDbImpl : IImpl
    {
        // SQLite Connect instance
        private static SqliteConnection? m_DbConnection;

        // Database table name
        private static string? m_DbTableName = "managed_packages";

        // Database source
        private static string? m_DataSource;

        // Sync lock.
        private static readonly object s_SyncRoot = new();

        /// <summary>
        /// Dispose
        /// </summary>
        /// <returns>Returns void.</returns>
        public void Dispose()
        {
            if (m_DbConnection != null)
            {
                if (m_DbConnection.State == ConnectionState.Open
                    || m_DbConnection.State == ConnectionState.Broken)
                {
                    m_DbConnection.Close();
                    m_DbConnection.DisposeAsync();
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
            if (string.IsNullOrWhiteSpace(dataSource))
            {
                return;
            }

            try
            {
                string? directory = Path.GetDirectoryName(dataSource);

                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                m_DataSource = "Data Source=" + dataSource;
                m_DbConnection = new SqliteConnection(m_DataSource);
                m_DbConnection.Open();
                EnsureSchema();
            }
            catch (Exception e)
            {
                if (DeployServer.AppDebug)
                {
                    ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture,
                        "ManagedPackageDbImpl PrepareDatabase Error:{0}\n{1}", e.Message, e.StackTrace));
                }
            }
        }

        /// <summary>
        /// GetSingleByName
        /// </summary>
        /// <param name="colName"></param>
        /// <returns>Returns ManagedPackageRepoType.</returns>
        public static ManagedPackageRepoType GetSingleByName(
            string? colName)
        {
            ManagedPackageRepoType result = new();

            if (colName == null || colName.Length == 0)
            {
                return result;
            }

            try
            {
                if (m_DbConnection != null)
                {
                    using (SqliteCommand? cmd = m_DbConnection.CreateCommand())
                    {
                        cmd.CommandType = CommandType.Text;
                        cmd.CommandText = "SELECT * FROM `" + m_DbTableName + "` WHERE `name` = @col_name AND `status` = 1 LIMIT 1;";
                        cmd.Parameters.AddWithValue("@col_name", colName.ToLowerInvariant());
                        cmd.Prepare();

                        try
                        {
                            using (SqliteDataReader reader = cmd.ExecuteReader())
                            {
                                if (!reader.Read())
                                {
                                    return result;
                                }

                                MapReaderToRepo(reader, result);
                            }
                        }
                        catch (SqliteException e)
                        {
                            if (DeployServer.AppDebug)
                            {
                                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "ManagedPackageImpl GetSingleByName SqliteException Error:{0}\n{1}", e.Message, e.StackTrace));
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                if (DeployServer.AppDebug)
                {
                    ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "ManagedPackageImpl GetSingleByName Error:{0}\n{1}", e.Message, e.StackTrace));
                }
            }

            return result;
        }

        /// <summary>
        /// ListPackages
        /// </summary>
        /// <returns>Returns local catalog rows.</returns>
        public static List<ManagedPackageLocalInfoType> ListPackages()
        {
            List<ManagedPackageLocalInfoType> result = [];

            if (m_DbConnection == null)
            {
                return result;
            }

            lock (s_SyncRoot)
            {
                using SqliteCommand cmd = m_DbConnection.CreateCommand();
                cmd.CommandText = "SELECT `name`, `source`, `status` FROM `" + m_DbTableName + "` ORDER BY `name`;";

                using SqliteDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    result.Add(new ManagedPackageLocalInfoType
                    {
                        Name = ReadString(reader, "name"),
                        Source = ReadString(reader, "source"),
                        Enabled = ReadInt(reader, "status") == 1
                    });
                }
            }

            return result;
        }

        /// <summary>
        /// MarkLocalOverride - protect package from Server sync overwrites.
        /// </summary>
        /// <param name="packageName"></param>
        /// <returns>Returns true when updated.</returns>
        public static bool MarkLocalOverride(
            string? packageName)
        {
            string name = NormalizeName(packageName);

            if (name.Length == 0 || m_DbConnection == null)
            {
                return false;
            }

            lock (s_SyncRoot)
            {
                using SqliteCommand cmd = m_DbConnection.CreateCommand();
                cmd.CommandText = "UPDATE `" + m_DbTableName + "` SET `source` = @source WHERE `name` = @name;";
                cmd.Parameters.AddWithValue("@source", ManagedPackageSource.Local);
                cmd.Parameters.AddWithValue("@name", name);

                return cmd.ExecuteNonQuery() > 0;
            }
        }

        /// <summary>
        /// ClearLocalOverride - delete local-override row so Server sync can recreate it.
        /// </summary>
        /// <param name="packageName"></param>
        /// <returns>Returns true when deleted.</returns>
        public static bool ClearLocalOverride(
            string? packageName)
        {
            string name = NormalizeName(packageName);

            if (name.Length == 0 || m_DbConnection == null)
            {
                return false;
            }

            lock (s_SyncRoot)
            {
                using SqliteCommand cmd = m_DbConnection.CreateCommand();
                cmd.CommandText = "DELETE FROM `" + m_DbTableName + "` WHERE `name` = @name AND `source` = @source;";
                cmd.Parameters.AddWithValue("@name", name);
                cmd.Parameters.AddWithValue("@source", ManagedPackageSource.Local);

                return cmd.ExecuteNonQuery() > 0;
            }
        }

        /// <summary>
        /// ApplyServerCatalog - upsert server rows; never overwrite local overrides.
        /// </summary>
        /// <param name="packages"></param>
        /// <returns>Returns applied count.</returns>
        public static int ApplyServerCatalog(
            IEnumerable<ManagedPackageInfoType> packages)
        {
            if (m_DbConnection == null)
            {
                return 0;
            }

            int applied = 0;
            HashSet<string> serverNames = new(StringComparer.OrdinalIgnoreCase);

            lock (s_SyncRoot)
            {
                foreach (ManagedPackageInfoType package in packages)
                {
                    string name = NormalizeName(package.Name);

                    if (name.Length == 0)
                    {
                        continue;
                    }

                    serverNames.Add(name);
                    string? source = GetSourceByName(name);

                    if (string.Equals(source, ManagedPackageSource.Local, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    UpsertServerPackage(package, name);
                    applied++;
                }

                DisableMissingServerPackages(serverNames);
            }

            return applied;
        }

        /// <summary>
        /// EnsureSchema
        /// </summary>
        /// <returns>Returns void.</returns>
        private static void EnsureSchema()
        {
            if (m_DbConnection == null)
            {
                return;
            }

            using (SqliteCommand create = m_DbConnection.CreateCommand())
            {
                create.CommandText =
                    "CREATE TABLE IF NOT EXISTS `" + m_DbTableName + "` (" +
                    "`name` TEXT PRIMARY KEY NOT NULL, " +
                    "`url_x86` TEXT, " +
                    "`url_x64` TEXT, " +
                    "`cmd_install_x86` TEXT, " +
                    "`cmd_install_x64` TEXT, " +
                    "`cmd_uninstall_x86` TEXT, " +
                    "`cmd_uninstall_x64` TEXT, " +
                    "`script_install_before` TEXT, " +
                    "`script_install_after` TEXT, " +
                    "`script_uninstall_before` TEXT, " +
                    "`script_uninstall_after` TEXT, " +
                    "`script_engine` TEXT, " +
                    "`sha256_x86` TEXT, " +
                    "`sha256_x64` TEXT, " +
                    "`status` INTEGER NOT NULL DEFAULT 1, " +
                    "`source` TEXT NOT NULL DEFAULT 'local'" +
                    ");";
                create.ExecuteNonQuery();
            }

            EnsureColumn("source", "TEXT NOT NULL DEFAULT 'local'");
            EnsureColumn("sha256_x86", "TEXT");
            EnsureColumn("sha256_x64", "TEXT");
            EnsureColumn("script_engine", "TEXT");
        }

        /// <summary>
        /// EnsureColumn
        /// </summary>
        /// <param name="columnName"></param>
        /// <param name="columnDef"></param>
        /// <returns>Returns void.</returns>
        private static void EnsureColumn(
            string columnName,
            string columnDef)
        {
            if (m_DbConnection == null || ColumnExists(columnName))
            {
                return;
            }

            using SqliteCommand alter = m_DbConnection.CreateCommand();
            alter.CommandText = "ALTER TABLE `" + m_DbTableName + "` ADD COLUMN `" + columnName + "` " + columnDef + ";";
            alter.ExecuteNonQuery();
        }

        /// <summary>
        /// ColumnExists
        /// </summary>
        /// <param name="columnName"></param>
        /// <returns>Returns bool.</returns>
        private static bool ColumnExists(
            string columnName)
        {
            if (m_DbConnection == null)
            {
                return false;
            }

            using SqliteCommand cmd = m_DbConnection.CreateCommand();
            cmd.CommandText = "PRAGMA table_info(`" + m_DbTableName + "`);";

            using SqliteDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                string? name = reader["name"]?.ToString();

                if (string.Equals(name, columnName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// GetSourceByName
        /// </summary>
        /// <param name="name"></param>
        /// <returns>Returns source or null when missing.</returns>
        private static string? GetSourceByName(
            string name)
        {
            if (m_DbConnection == null)
            {
                return null;
            }

            using SqliteCommand cmd = m_DbConnection.CreateCommand();
            cmd.CommandText = "SELECT `source` FROM `" + m_DbTableName + "` WHERE `name` = @name LIMIT 1;";
            cmd.Parameters.AddWithValue("@name", name);

            object? value = cmd.ExecuteScalar();

            return value == null || value == DBNull.Value ? null : Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// UpsertServerPackage
        /// </summary>
        /// <param name="package"></param>
        /// <param name="name"></param>
        /// <returns>Returns void.</returns>
        private static void UpsertServerPackage(
            ManagedPackageInfoType package,
            string name)
        {
            if (m_DbConnection == null)
            {
                return;
            }

            using SqliteCommand cmd = m_DbConnection.CreateCommand();
            cmd.CommandText =
                "INSERT INTO `" + m_DbTableName + "` (" +
                "`name`, `url_x86`, `url_x64`, `cmd_install_x86`, `cmd_install_x64`, " +
                "`cmd_uninstall_x86`, `cmd_uninstall_x64`, `script_install_before`, `script_install_after`, " +
                "`script_uninstall_before`, `script_uninstall_after`, `script_engine`, `sha256_x86`, `sha256_x64`, `status`, `source`) " +
                "VALUES (" +
                "@name, @url_x86, @url_x64, @cmd_install_x86, @cmd_install_x64, " +
                "@cmd_uninstall_x86, @cmd_uninstall_x64, @script_install_before, @script_install_after, " +
                "@script_uninstall_before, @script_uninstall_after, @script_engine, @sha256_x86, @sha256_x64, @status, @source) " +
                "ON CONFLICT(`name`) DO UPDATE SET " +
                "`url_x86`=excluded.`url_x86`, " +
                "`url_x64`=excluded.`url_x64`, " +
                "`cmd_install_x86`=excluded.`cmd_install_x86`, " +
                "`cmd_install_x64`=excluded.`cmd_install_x64`, " +
                "`cmd_uninstall_x86`=excluded.`cmd_uninstall_x86`, " +
                "`cmd_uninstall_x64`=excluded.`cmd_uninstall_x64`, " +
                "`script_install_before`=excluded.`script_install_before`, " +
                "`script_install_after`=excluded.`script_install_after`, " +
                "`script_uninstall_before`=excluded.`script_uninstall_before`, " +
                "`script_uninstall_after`=excluded.`script_uninstall_after`, " +
                "`script_engine`=excluded.`script_engine`, " +
                "`sha256_x86`=excluded.`sha256_x86`, " +
                "`sha256_x64`=excluded.`sha256_x64`, " +
                "`status`=excluded.`status`, " +
                "`source`=excluded.`source` " +
                "WHERE `" + m_DbTableName + "`.`source` != 'local';";

            cmd.Parameters.AddWithValue("@name", name);
            cmd.Parameters.AddWithValue("@url_x86", package.Urlx86 ?? "");
            cmd.Parameters.AddWithValue("@url_x64", package.Urlx64 ?? "");
            cmd.Parameters.AddWithValue("@cmd_install_x86", package.CmdInstallx86 ?? "");
            cmd.Parameters.AddWithValue("@cmd_install_x64", package.CmdInstallx64 ?? "");
            cmd.Parameters.AddWithValue("@cmd_uninstall_x86", package.CmdUnInstallx86 ?? "");
            cmd.Parameters.AddWithValue("@cmd_uninstall_x64", package.CmdUnInstallx64 ?? "");
            cmd.Parameters.AddWithValue("@script_install_before", package.ScriptInstallBefore ?? "");
            cmd.Parameters.AddWithValue("@script_install_after", package.ScriptInstallAfter ?? "");
            cmd.Parameters.AddWithValue("@script_uninstall_before", package.ScriptUnInstallBefore ?? "");
            cmd.Parameters.AddWithValue("@script_uninstall_after", package.ScriptUnInstallAfter ?? "");
            cmd.Parameters.AddWithValue(
                "@script_engine",
                string.IsNullOrWhiteSpace(package.ScriptEngine) ? "powershell" : package.ScriptEngine.Trim().ToLowerInvariant());
            cmd.Parameters.AddWithValue("@sha256_x86", NormalizeSha(package.Sha256x86));
            cmd.Parameters.AddWithValue("@sha256_x64", NormalizeSha(package.Sha256x64));
            cmd.Parameters.AddWithValue("@status", package.IsEnabled ? 1 : 0);
            cmd.Parameters.AddWithValue("@source", ManagedPackageSource.Server);
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// DisableMissingServerPackages
        /// </summary>
        /// <param name="serverNames"></param>
        /// <returns>Returns void.</returns>
        private static void DisableMissingServerPackages(
            HashSet<string> serverNames)
        {
            if (m_DbConnection == null)
            {
                return;
            }

            List<string> toDisable = [];

            using (SqliteCommand cmd = m_DbConnection.CreateCommand())
            {
                cmd.CommandText = "SELECT `name` FROM `" + m_DbTableName + "` WHERE `source` = @source;";
                cmd.Parameters.AddWithValue("@source", ManagedPackageSource.Server);

                using SqliteDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    string name = ReadString(reader, "name");

                    if (name.Length > 0 && !serverNames.Contains(name))
                    {
                        toDisable.Add(name);
                    }
                }
            }

            foreach (string name in toDisable)
            {
                using SqliteCommand update = m_DbConnection.CreateCommand();
                update.CommandText = "UPDATE `" + m_DbTableName + "` SET `status` = 0 WHERE `name` = @name AND `source` = @source;";
                update.Parameters.AddWithValue("@name", name);
                update.Parameters.AddWithValue("@source", ManagedPackageSource.Server);
                update.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// MapReaderToRepo
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="result"></param>
        /// <returns>Returns void.</returns>
        private static void MapReaderToRepo(
            SqliteDataReader reader,
            ManagedPackageRepoType result)
        {
            result.Name = ReadString(reader, "name");
            result.Urlx86 = ReadString(reader, "url_x86");
            result.Urlx64 = ReadString(reader, "url_x64");
            result.CmdInstallx86 = ReadString(reader, "cmd_install_x86");
            result.CmdInstallx64 = ReadString(reader, "cmd_install_x64");
            result.CmdUnInstallx86 = ReadString(reader, "cmd_uninstall_x86");
            result.CmdUnInstallx64 = ReadString(reader, "cmd_uninstall_x64");
            result.ScriptInstallBefore = ReadString(reader, "script_install_before");
            result.ScriptInstallAfter = ReadString(reader, "script_install_after");
            result.ScriptUnInstallBefore = ReadString(reader, "script_uninstall_before");
            result.ScriptUnInstallAfter = ReadString(reader, "script_uninstall_after");
            result.ScriptEngine = HasColumn(reader, "script_engine") ? ReadString(reader, "script_engine") : "";
            result.Sha256x86 = HasColumn(reader, "sha256_x86") ? ReadString(reader, "sha256_x86") : "";
            result.Sha256x64 = HasColumn(reader, "sha256_x64") ? ReadString(reader, "sha256_x64") : "";
            result.Source = HasColumn(reader, "source") ? ReadString(reader, "source") : "";
        }

        /// <summary>
        /// HasColumn
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="column"></param>
        /// <returns>Returns bool.</returns>
        private static bool HasColumn(
            SqliteDataReader reader,
            string column)
        {
            for (int i = 0; i < reader.FieldCount; i++)
            {
                if (string.Equals(reader.GetName(i), column, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
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
        /// NormalizeSha
        /// </summary>
        /// <param name="sha"></param>
        /// <returns>Returns lowercase hex or empty.</returns>
        private static string NormalizeSha(
            string? sha)
        {
            return string.IsNullOrWhiteSpace(sha)
                ? ""
                : sha.Trim().ToLowerInvariant();
        }

        /// <summary>
        /// ReadString
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="column"></param>
        /// <returns>Returns string.</returns>
        private static string ReadString(
            SqliteDataReader reader,
            string column)
        {
            return reader[column] != DBNull.Value
                ? Convert.ToString(reader[column], CultureInfo.InvariantCulture) ?? ""
                : "";
        }

        /// <summary>
        /// ReadInt
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="column"></param>
        /// <returns>Returns int.</returns>
        private static int ReadInt(
            SqliteDataReader reader,
            string column)
        {
            return reader[column] != DBNull.Value
                ? Convert.ToInt32(reader[column], CultureInfo.InvariantCulture)
                : 0;
        }
    }
}
