
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using LemonSubtitleStudio.Models;

namespace LemonSubtitleStudio.Services
{
    public interface ITaskQueueService
    {
        ObservableCollection<TaskItem> Tasks { get; }
        void AddTask(TaskItem task);
        void RemoveTask(TaskItem task);
        void ClearAll();
        Task ExecuteTasksAsync(Func<TaskItem, CancellationToken, Task> processTask, CancellationToken cancellationToken);
        event EventHandler<TaskCompletedEventArgs> TaskCompleted;
    }
}
