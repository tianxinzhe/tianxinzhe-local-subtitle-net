using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LemonSubtitleStudio.Models;

namespace LemonSubtitleStudio.Services
{
    public class HistoryService : IHistoryService
    {
        private readonly string _dbPath;
        private readonly ILoggingService _loggingService;

        public HistoryService(ISettingsService settingsService, ILoggingService loggingService)
        {
            _loggingService = loggingService;
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appPath = Path.Combine(appDataPath, "LemonSubtitleStudio");
            Directory.CreateDirectory(appPath);
            _dbPath = Path.Combine(appPath, "app.db");
            
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            if (!File.Exists(_dbPath))
            {
                SQLiteConnection.CreateFile(_dbPath);
                using var conn = new SQLiteConnection($"Data Source={_dbPath};Version=3;");
                conn.Open();
                
                var cmd = new SQLiteCommand(@"
                    CREATE TABLE IF NOT EXISTS history_records (
                        Id TEXT PRIMARY KEY,
                        InputPath TEXT NOT NULL,
                        OutputPath TEXT,
                        Status INTEGER NOT NULL,
                        ErrorMessage TEXT,
                        CreatedAt TEXT NOT NULL
                    )", conn);
                cmd.ExecuteNonQuery();
            }
        }

        public async Task AddRecordAsync(string inputPath, string outputPath, Models.TaskStatus status, string errorMessage, DateTime createdAt)
        {
            try
            {
                using var conn = new SQLiteConnection($"Data Source={_dbPath};Version=3;");
                await conn.OpenAsync();
                
                var cmd = new SQLiteCommand(@"
                    INSERT INTO history_records (Id, InputPath, OutputPath, Status, ErrorMessage, CreatedAt)
                    VALUES (@Id, @InputPath, @OutputPath, @Status, @ErrorMessage, @CreatedAt)", conn);
                
                cmd.Parameters.AddWithValue("@Id", Guid.NewGuid().ToString());
                cmd.Parameters.AddWithValue("@InputPath", inputPath);
                cmd.Parameters.AddWithValue("@OutputPath", outputPath ?? string.Empty);
                cmd.Parameters.AddWithValue("@Status", (int)status);
                cmd.Parameters.AddWithValue("@ErrorMessage", errorMessage ?? string.Empty);
                cmd.Parameters.AddWithValue("@CreatedAt", createdAt.ToString("yyyy-MM-dd HH:mm:ss"));
                
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                _loggingService.LogError("添加历史记录失败", ex);
            }
        }

        public async Task<List<HistoryRecord>> GetRecordsAsync(int limit = 50)
        {
            var records = new List<HistoryRecord>();
            
            try
            {
                using var conn = new SQLiteConnection($"Data Source={_dbPath};Version=3;");
                await conn.OpenAsync();
                
                var cmd = new SQLiteCommand("SELECT * FROM history_records ORDER BY CreatedAt DESC LIMIT @Limit", conn);
                cmd.Parameters.AddWithValue("@Limit", limit);
                using var reader = await cmd.ExecuteReaderAsync();
            
                while (await reader.ReadAsync())
                {
                    records.Add(new HistoryRecord
                    {
                        Id = Guid.Parse(reader["Id"].ToString()),
                        InputPath = reader["InputPath"].ToString(),
                        OutputPath = reader["OutputPath"].ToString(),
                        Status = (Models.TaskStatus)int.Parse(reader["Status"].ToString()),
                        ErrorMessage = reader["ErrorMessage"].ToString(),
                        CreatedAt = DateTime.Parse(reader["CreatedAt"].ToString())
                    });
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError("获取历史记录失败", ex);
            }
            
            return records;
        }

        public async Task DeleteRecordAsync(Guid id)
        {
            try
            {
                using var conn = new SQLiteConnection($"Data Source={_dbPath};Version=3;");
                await conn.OpenAsync();
                
                var cmd = new SQLiteCommand("DELETE FROM history_records WHERE Id = @Id", conn);
                cmd.Parameters.AddWithValue("@Id", id.ToString());
                
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                _loggingService.LogError("删除历史记录失败", ex);
            }
        }

        public async Task ClearAllAsync()
        {
            try
            {
                using var conn = new SQLiteConnection($"Data Source={_dbPath};Version=3;");
                await conn.OpenAsync();
                
                var cmd = new SQLiteCommand("DELETE FROM history_records", conn);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                _loggingService.LogError("清空历史记录失败", ex);
            }
        }
    }
}