// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Microsoft.Data.Sqlite;
using System.Data;
using System.Globalization;
using Zeron.Demand.ZCore.Type;
using Zeron.ZCore;
using Zeron.ZInterfaces;
using Zeron.ZServers;

namespace Zeron.Demand.ZServers.Impls
{
    /// <summary>
    /// ManagedPackageImpl
    /// </summary>
    internal class ManagedPackageImpl : IImpl
    {
        // SQLite Connect instance
        private static SqliteConnection? m_DbConnection;

        // Database table name
        private static string? m_DbTableName = "managed_packages";

        // Database source
        private static string? m_DataSource;

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
        public void PrepareDatabase(string? dataSource)
        {
            if (File.Exists(dataSource))
            {
                m_DataSource = "Data Source=" + dataSource;
                m_DbConnection = new SqliteConnection(m_DataSource);

                if (m_DbConnection != null
                    && m_DbConnection.State == ConnectionState.Closed)
                {
                    m_DbConnection.Open();
                }
            }
        }

        /// <summary>
        /// GetSingleByName
        /// </summary>
        /// <param name="colName"></param>
        /// <returns>Returns ManagedPackageRepoType.</returns>
        public static ManagedPackageRepoType GetSingleByName(string? colName)
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
                        cmd.Parameters.AddWithValue("@col_name", colName.ToLower());
                        cmd.Prepare();

                        try
                        {
                            using (SqliteDataReader reader = cmd.ExecuteReader())
                            {
                                reader.Read();
                                result.Name = (reader["name"] != DBNull.Value) ? (string?)reader["name"] : string.Empty;
                                result.Urlx86 = (reader["url_x86"] != DBNull.Value) ? (string?)reader["url_x86"] : string.Empty;
                                result.Urlx64 = (reader["url_x64"] != DBNull.Value) ? (string?)reader["url_x64"] : string.Empty;
                                result.CmdInstallx86 = (reader["cmd_install_x86"] != DBNull.Value) ? (string?)reader["cmd_install_x86"] : string.Empty;
                                result.CmdInstallx64 = (reader["cmd_install_x64"] != DBNull.Value) ? (string?)reader["cmd_install_x64"] : string.Empty;
                                result.CmdUnInstallx86 = (reader["cmd_uninstall_x86"] != DBNull.Value) ? (string?)reader["cmd_uninstall_x86"] : string.Empty;
                                result.CmdUnInstallx64 = (reader["cmd_uninstall_x64"] != DBNull.Value) ? (string?)reader["cmd_uninstall_x64"] : string.Empty;
                                result.ScriptInstallBefore = (reader["script_install_before"] != DBNull.Value) ? (string?)reader["script_install_before"] : string.Empty;
                                result.ScriptInstallAfter = (reader["script_install_after"] != DBNull.Value) ? (string?)reader["script_install_after"] : string.Empty;
                                result.ScriptUnInstallBefore = (reader["script_uninstall_before"] != DBNull.Value) ? (string?)reader["script_uninstall_before"] : string.Empty;
                                result.ScriptUnInstallAfter = (reader["script_uninstall_after"] != DBNull.Value) ? (string?)reader["script_uninstall_after"] : string.Empty;
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
    }
}
