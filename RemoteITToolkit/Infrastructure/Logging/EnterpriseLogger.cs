using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using RemoteITToolkit.Core.DTOs;
using RemoteITToolkit.Core.Interfaces;

namespace RemoteITToolkit.Infrastructure.Logging
{
    public class EnterpriseLogger : IExtendedLogger
    {
        private readonly string _logDirectory;
        private readonly string _connectionString;
        private readonly object _fileLock = new object();

        public EnterpriseLogger(string connectionString)
        {
            _connectionString = connectionString;
            _logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            if (!Directory.Exists(_logDirectory)) Directory.CreateDirectory(_logDirectory);
        }

        private void WriteLog(string category, string level, string message, string op = "System")
        {
            // Write to Text File
            lock (_fileLock)
            {
                try
                {
                    string filename = $"{category.ToLower().Replace(" ", "_")}.log";
                    string path = Path.Combine(_logDirectory, filename);
                    string logLine = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] [User: {op}] {message}{Environment.NewLine}";
                    File.AppendAllText(path, logLine);
                }
                catch { }
            }

            // Write to SQLite Database
            try
            {
                using (var conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand("INSERT INTO EnterpriseLogs (Id, Category, LogLevel, Message, Operator, CreatedAt) VALUES (@Id, @Cat, @Lvl, @Msg, @Op, @Date)", conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", Guid.NewGuid().ToString());
                        cmd.Parameters.AddWithValue("@Cat", category);
                        cmd.Parameters.AddWithValue("@Lvl", level);
                        cmd.Parameters.AddWithValue("@Msg", message);
                        cmd.Parameters.AddWithValue("@Op", op);
                        cmd.Parameters.AddWithValue("@Date", DateTime.Now);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch { }
        }

        public void LogInfo(string message) => WriteLog("Application", "INFO", message);
        public void LogWarning(string message) => WriteLog("Application", "WARNING", message);
        public void LogError(string message, Exception ex) => WriteLog("Error", "CRITICAL", $"{message} | Ex: {ex.Message}");

        public void LogSecurity(string message, string op = "System") => WriteLog("Security", "WARN", message, op);
        public void LogUserAction(string action, string op = "System") => WriteLog("User Actions", "INFO", action, op);
        public void LogNetwork(string message) => WriteLog("Network", "INFO", message);

        public void LogActivity(string activityMessage) => LogUserAction(activityMessage);
        public void LogAudit(string op, string action) => LogSecurity(action, op);

        public IEnumerable<LogEntryDTO> GetRecentLogs(int limit = 1000)
        {
            var logs = new List<LogEntryDTO>();
            try
            {
                using (var conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand($"SELECT Category, LogLevel, Message, Operator, CreatedAt FROM EnterpriseLogs ORDER BY CreatedAt DESC LIMIT {limit}", conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            logs.Add(new LogEntryDTO
                            {
                                Category = reader["Category"].ToString(),
                                LogLevel = reader["LogLevel"].ToString(),
                                Message = reader["Message"].ToString(),
                                Operator = reader["Operator"].ToString(),
                                CreatedAt = Convert.ToDateTime(reader["CreatedAt"])
                            });
                        }
                    }
                }
            }
            catch { }
            return logs;
        }
    }
}