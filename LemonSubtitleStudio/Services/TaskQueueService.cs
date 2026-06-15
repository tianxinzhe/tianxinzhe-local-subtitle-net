
using LemonSubtitleStudio.Models;
using System.Collections.ObjectModel;

namespace LemonSubtitleStudio.Services
{
    public class TaskQueueService : ITaskQueueService
    {
        public ObservableCollection<TaskItem> Tasks { get; } = new ObservableCollection<TaskItem>();
        public event EventHandler<TaskCompletedEventArgs>? TaskCompleted;

        public void AddTask(TaskItem task)
        {
            Tasks.Add(task);
        }

        public void RemoveTask(TaskItem task)
        {
            Tasks.Remove(task);
        }

        public void ClearAll()
        {
            Tasks.Clear();
        }

        public async Task ExecuteTasksAsync(CancellationToken cancellationToken)
        {
            foreach (var task in Tasks)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                task.Status = TaskStatus.Processing;
                task.Progress = 0;

                try
                {
                    await ExecuteTaskAsync(task, cancellationToken);
                    task.Status = TaskStatus.Completed;
                    task.Progress = 100;
                }
                catch (Exception ex)
                {
                    task.Status = TaskStatus.Failed;
                    task.ErrorMessage = ex.Message;
                }

                TaskCompleted?.Invoke(this, new TaskCompletedEventArgs(task));
            }
        }

        private async Task ExecuteTaskAsync(TaskItem task, CancellationToken cancellationToken)
        {
            await Task.Delay(1000, cancellationToken);
            task.Progress = 33;
            await Task.Delay(1000, cancellationToken);
            task.Progress = 66;
            await Task.Delay(1000, cancellationToken);
            task.Progress = 100;
        }
    }

    public class TaskCompletedEventArgs : EventArgs
    {
        public TaskItem Task { get; }
        public TaskCompletedEventArgs(TaskItem task) => Task = task;
    }
}
