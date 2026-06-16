
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LemonSubtitleStudio.Services
{
    public class LoggingService : ILoggingService
    {
        private readonly List<string> _logs = new List<string>();
        private readonly string _logFilePath;

        public event EventHandler<string>? LogAdded;

        public LoggingService()
        {
            _logFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
                "LemonSubtitleStudio", "app.log");
            var directory = Path.GetDirectoryName(_logFilePath);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory!);
        }

        public void Log(string message)
        {
            var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] INFO: {message}";
            _logs.Add(logEntry);
            WriteToFile(logEntry);
            LogAdded?.Invoke(this, logEntry);
        }

        public void LogError(string message, Exception? exception = null)
        {
            var logEntry = exception != null 
                ? $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ERROR: {message} - {exception}" 
                : $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ERROR: {message}";
            _logs.Add(logEntry);
            WriteToFile(logEntry);
            LogAdded?.Invoke(this, logEntry);
        }

        public void LogWarning(string message)
        {
            var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] WARN: {message}";
            _logs.Add(logEntry);
            WriteToFile(logEntry);
            LogAdded?.Invoke(this, logEntry);
        }

        public List<string> GetLogs() => _logs.ToList();

        public void Clear() => _logs.Clear();

        private void WriteToFile(string message)
        {
            try
            {
                File.AppendAllText(_logFilePath, message + Environment.NewLine);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"写入日志文件失败: {ex.Message}");
            }
        }
    }
}
