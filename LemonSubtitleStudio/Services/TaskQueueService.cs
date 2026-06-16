
using System;
using System.Collections.ObjectModel;
using System.Threading;
using LemonSubtitleStudio.Models;

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

        public async System.Threading.Tasks.Task ExecuteTasksAsync(Func<TaskItem, CancellationToken, System.Threading.Tasks.Task> processTask, CancellationToken cancellationToken)
        {
            foreach (var task in Tasks)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                task.Status = Models.TaskStatus.Processing;
                task.Progress = 0;

                try
                {
                    await processTask(task, cancellationToken);
                    task.Status = Models.TaskStatus.Completed;
                    task.Progress = 100;
                }
                catch (Exception ex)
                {
                    task.Status = Models.TaskStatus.Failed;
                    task.ErrorMessage = ex.Message;
                }

                TaskCompleted?.Invoke(this, new TaskCompletedEventArgs(task));
            }
        }
    }

    public class TaskCompletedEventArgs : EventArgs
    {
        public TaskItem Task { get; }
        public TaskCompletedEventArgs(TaskItem task) => Task = task;
    }
}
