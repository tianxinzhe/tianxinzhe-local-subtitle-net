
using System;

namespace LemonSubtitleStudio.Models
{
    public enum TaskStatus
    {
        Waiting,
        Processing,
        Completed,
        Failed
    }

    public class TaskItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string InputPath { get; set; } = string.Empty;
        public string OutputPath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public TaskStatus Status { get; set; } = TaskStatus.Waiting;
        public int Progress { get; set; } = 0;
        public string ErrorMessage { get; set; } = string.Empty;
        public string MediaPath { get; set; } = string.Empty;
    }
}
