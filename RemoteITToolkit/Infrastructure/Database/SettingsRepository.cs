using System;
using System.Collections.Generic;
using System.Data.SQLite;
using RemoteITToolkit.Core.Entities;
using RemoteITToolkit.Core.Interfaces;

namespace RemoteITToolkit.Infrastructure.Database
{
    public class SettingsRepository : ISettingsRepository
    {
        private readonly string _connectionString;
        private readonly ILogger _logger;

        public SettingsRepository(string connectionString, ILogger logger)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Add(Setting entity)
        {
            try
            {
                using (var conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();
                    using (var tx = conn.BeginTransaction())
                    {
                        using (var cmd = new SQLiteCommand("INSERT INTO Settings (Id, [Key], Value, CreatedAt) VALUES (@Id, @Key, @Value, @CreatedAt)", conn, tx))
                        {
                            cmd.Parameters.AddWithValue("@Id", entity.Id.ToString());
                            cmd.Parameters.AddWithValue("@Key", entity.Key);
                            cmd.Parameters.AddWithValue("@Value", entity.Value);
                            cmd.Parameters.AddWithValue("@CreatedAt", entity.CreatedAt);
                            cmd.ExecuteNonQuery();
                        }
                        tx.Commit();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to add setting: {entity?.Key}", ex);
                throw;
            }
        }

        public void Update(Setting entity)
        {
            try
            {
                using (var conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();
                    using (var tx = conn.BeginTransaction())
                    {
                        using (var cmd = new SQLiteCommand("UPDATE Settings SET Value = @Value, UpdatedAt = @UpdatedAt WHERE [Key] = @Key", conn, tx))
                        {
                            entity.UpdatedAt = DateTime.UtcNow;
                            cmd.Parameters.AddWithValue("@Value", entity.Value);
                            cmd.Parameters.AddWithValue("@UpdatedAt", entity.UpdatedAt);
                            cmd.Parameters.AddWithValue("@Key", entity.Key);
                            cmd.ExecuteNonQuery();
                        }
                        tx.Commit();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to update setting: {entity?.Key}", ex);
                throw;
            }
        }

        public IEnumerable<Setting> GetAll()
        {
            var results = new List<Setting>();
            try
            {
                using (var conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand("SELECT Id, [Key], Value, CreatedAt, UpdatedAt FROM Settings", conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            results.Add(new Setting
                            {
                                Id = Guid.Parse(reader["Id"].ToString()),
                                Key = reader["Key"].ToString(),
                                Value = reader["Value"].ToString(),
                                CreatedAt = Convert.ToDateTime(reader["CreatedAt"])
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to retrieve settings.", ex);
            }
            return results;
        }
    }
}