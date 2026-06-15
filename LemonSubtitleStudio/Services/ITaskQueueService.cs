
using LemonSubtitleStudio.Models;

namespace LemonSubtitleStudio.Services
{
    public interface ITaskQueueService
    {
        ObservableCollection<TaskItem> Tasks { get; }
        void AddTask(TaskItem task);
        void RemoveTask(TaskItem task);
        void ClearAll();
        Task ExecuteTasksAsync(CancellationToken cancellationToken);
        event EventHandler<TaskCompletedEventArgs> TaskCompleted;
    }
}
