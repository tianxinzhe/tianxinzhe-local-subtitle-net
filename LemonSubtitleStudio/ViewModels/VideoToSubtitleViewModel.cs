
using Prism.Commands;
using LemonSubtitleStudio.Models;
using LemonSubtitleStudio.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace LemonSubtitleStudio.ViewModels
{
    public class VideoToSubtitleViewModel : INotifyPropertyChanged
    {
        private readonly IFileService _fileService;
        private readonly ITaskQueueService _taskQueueService;
        private readonly ILoggingService _loggingService;
        private readonly ISettingsService _settingsService;

        public ObservableCollection<TaskItem> Tasks { get; } = new ObservableCollection<TaskItem>();
        public ObservableCollection<SubtitleItem> Subtitles { get; } = new ObservableCollection<SubtitleItem>();
        public ObservableCollection<string> Logs { get; } = new ObservableCollection<string>();

        public List<string> Languages { get; } = new List<string> { "中文", "English", "日本語", "한국어" };
        public List<string> ProcessingModes { get; } = new List<string> { "高精度", "快速" };
        public List<string> Models { get; } = new List<string> { "tiny", "base", "small", "medium", "large" };

        private string _selectedLanguage = "中文";
        public string SelectedLanguage
        {
            get => _selectedLanguage;
            set { _selectedLanguage = value; OnPropertyChanged(); }
        }

        private string _selectedProcessingMode = "高精度";
        public string SelectedProcessingMode
        {
            get => _selectedProcessingMode;
            set { _selectedProcessingMode = value; OnPropertyChanged(); }
        }

        private string _selectedModel = "base";
        public string SelectedModel
        {
            get => _selectedModel;
            set { _selectedModel = value; OnPropertyChanged(); }
        }

        private string _outputDirectory = string.Empty;
        public string OutputDirectory
        {
            get => _outputDirectory;
            set { _outputDirectory = value; OnPropertyChanged(); }
        }

        private string _memoryUsage = "0MB";
        public string MemoryUsage
        {
            get => _memoryUsage;
            set { _memoryUsage = value; OnPropertyChanged(); }
        }

        private string _cpuUsage = "0%";
        public string CpuUsage
        {
            get => _cpuUsage;
            set { _cpuUsage = value; OnPropertyChanged(); }
        }

        private int _overallProgress = 0;
        public int OverallProgress
        {
            get => _overallProgress;
            set { _overallProgress = value; OnPropertyChanged(); }
        }

        private string _currentTaskInfo = string.Empty;
        public string CurrentTaskInfo
        {
            get => _currentTaskInfo;
            set { _currentTaskInfo = value; OnPropertyChanged(); }
        }

        public DelegateCommand BrowseOutputDirectoryCommand { get; }
        public DelegateCommand StartProcessingCommand { get; }
        public DelegateCommand ExportAllCommand { get; }
        public DelegateCommand ClearQueueCommand { get; }
        public DelegateCommand<SubtitleItem> EditSubtitleCommand { get; }
        public DelegateCommand AddFilesCommand { get; }

        public VideoToSubtitleViewModel()
        {
            _fileService = new FileService();
            _taskQueueService = new TaskQueueService();
            _loggingService = new LoggingService();
            _settingsService = new SettingsService();

            OutputDirectory = _settingsService.DefaultOutputDirectory;

            BrowseOutputDirectoryCommand = new DelegateCommand(BrowseOutputDirectory);
            StartProcessingCommand = new DelegateCommand(StartProcessing);
            ExportAllCommand = new DelegateCommand(ExportAll);
            ClearQueueCommand = new DelegateCommand(ClearQueue);
            EditSubtitleCommand = new DelegateCommand<SubtitleItem>(EditSubtitle);
            AddFilesCommand = new DelegateCommand(AddFiles);

            _loggingService.LogAdded += (s, e) => Logs.Add(e);
            _loggingService.Log("视频转字幕页面已加载");
        }

        private void BrowseOutputDirectory()
        {
            var folder = _fileService.SelectFolder("选择输出目录");
            if (!string.IsNullOrEmpty(folder))
                OutputDirectory = folder;
        }

        private async void StartProcessing()
        {
            if (Tasks.Count == 0) return;

            _loggingService.Log("开始处理任务...");
            foreach (var task in Tasks)
            {
                task.Status = TaskStatus.Processing;
                task.Progress = 0;
            }

            foreach (var task in Tasks)
            {
                CurrentTaskInfo = $"正在处理: {task.FileName}";
                _loggingService.Log($"处理文件: {task.FileName}");

                for (int i = 0; i <= 100; i += 10)
                {
                    task.Progress = i;
                    OverallProgress = (int)((Tasks.IndexOf(task) * 100 + i) / Tasks.Count);
                    await Task.Delay(200);
                }

                task.Status = TaskStatus.Completed;
                _loggingService.Log($"完成: {task.FileName}");
            }

            CurrentTaskInfo = "所有任务已完成";
            _loggingService.Log("处理完成");
        }

        private void ExportAll()
        {
            _loggingService.Log("导出所有字幕");
        }

        private void ClearQueue()
        {
            Tasks.Clear();
            _loggingService.Log("队列已清空");
        }

        private void EditSubtitle(SubtitleItem subtitle)
        {
            _loggingService.Log($"编辑字幕: {subtitle.Index}");
        }

        private void AddFiles()
        {
            var files = _fileService.SelectFiles("视频文件|*.mp4;*.mkv;*.avi;*.mov");
            foreach (var file in files)
            {
                Tasks.Add(new TaskItem
                {
                    InputPath = file,
                    FileName = System.IO.Path.GetFileName(file),
                    Status = TaskStatus.Waiting
                });
            }
            _loggingService.Log($"添加了 {files.Length} 个文件");
        }

        public void DropHandler(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
                foreach (var file in files)
                {
                    var ext = System.IO.Path.GetExtension(file).ToLower();
                    if (ext is ".mp4" or ".mkv" or ".avi" or ".mov")
                    {
                        Tasks.Add(new TaskItem
                        {
                            InputPath = file,
                            FileName = System.IO.Path.GetFileName(file),
                            Status = TaskStatus.Waiting
                        });
                    }
                }
                _loggingService.Log($"拖拽添加了 {files.Length} 个文件");
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
