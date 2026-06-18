
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

        public string FileSizeFormatted
        {
            get
            {
                try
                {
                    if (string.IsNullOrEmpty(InputPath) || !System.IO.File.Exists(InputPath))
                        return "0 B";
                    var info = new System.IO.FileInfo(InputPath);
                    long size = info.Length;
                    if (size < 1024) return $"{size} B";
                    if (size < 1024 * 1024) return $"{size / 1024.0:F1} KB";
                    if (size < 1024 * 1024 * 1024) return $"{size / (1024.0 * 1024):F1} MB";
                    return $"{size / (1024.0 * 1024 * 1024):F2} GB";
                }
                catch { return "0 B"; }
            }
        }

        public string DurationFormatted => "00:00";
        public string RemainingTimeFormatted => "--:--";

        public int NodeCount { get; set; }
    }
}
