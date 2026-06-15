
namespace LemonSubtitleStudio.Services
{
    public interface ILoggingService
    {
        void Log(string message);
        void LogError(string message, Exception? exception = null);
        void LogWarning(string message);
        event EventHandler<string> LogAdded;
        List<string> GetLogs();
        void Clear();
    }
}
