using System;
using System.Data.SQLite;
using System.IO;

namespace RemoteITToolkit.Infrastructure.Database
{
    public class SqliteDatabaseInitializer
    {
        private readonly string _connectionString;

        public SqliteDatabaseInitializer(string connectionString)
        {
            _connectionString = connectionString;
        }

        public void Initialize()
        {
            var builder = new SQLiteConnectionStringBuilder(_connectionString);
            string dbPath = builder.DataSource;

            if (!File.Exists(dbPath))
            {
                SQLiteConnection.CreateFile(dbPath);
            }

            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                        CREATE TABLE IF NOT EXISTS Settings (
                            Id TEXT PRIMARY KEY,
                            [Key] TEXT UNIQUE NOT NULL,
                            Value TEXT,
                            CreatedAt DATETIME,
                            UpdatedAt DATETIME
                        );
                        CREATE TABLE IF NOT EXISTS ApplicationLogs (
                            Id TEXT PRIMARY KEY,
                            LogLevel TEXT,
                            Message TEXT,
                            StackTrace TEXT,
                            CreatedAt DATETIME,
                            UpdatedAt DATETIME
                        );
                        CREATE TABLE IF NOT EXISTS RecentScans (
                            Id TEXT PRIMARY KEY,
                            ScanDate DATETIME,
                            DeviceName TEXT,
                            Summary TEXT,
                            CreatedAt DATETIME,
                            UpdatedAt DATETIME
                        );
                        CREATE TABLE IF NOT EXISTS ReportRecords (
                            Id TEXT PRIMARY KEY,
                            FilePath TEXT,
                            ReportType TEXT,
                            OperatorName TEXT,
                            CreatedAt DATETIME,
                            UpdatedAt DATETIME
                        );
                    ";
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}