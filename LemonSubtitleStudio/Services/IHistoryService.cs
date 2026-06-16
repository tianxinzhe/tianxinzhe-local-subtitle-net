using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LemonSubtitleStudio.Models;

namespace LemonSubtitleStudio.Services
{
    public interface IHistoryService
    {
        Task AddRecordAsync(string inputPath, string outputPath, Models.TaskStatus status, string errorMessage, DateTime createdAt);
        Task<List<HistoryRecord>> GetRecordsAsync(int limit = 50);
        Task DeleteRecordAsync(Guid id);
        Task ClearAllAsync();
    }

    public class HistoryRecord
    {
        public Guid Id { get; set; }
        public string InputPath { get; set; } = string.Empty;
        public string OutputPath { get; set; } = string.Empty;
        public Models.TaskStatus Status { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}