
using Prism.Commands;
using LemonSubtitleStudio.Models;
using LemonSubtitleStudio.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace LemonSubtitleStudio.ViewModels
{
    public class VideoToAudioViewModel : INotifyPropertyChanged
    {
        private readonly IFileService _fileService;
        private readonly ILoggingService _loggingService;
        private readonly ISettingsService _settingsService;

        public ObservableCollection<TaskItem> Tasks { get; } = new ObservableCollection<TaskItem>();
        public ObservableCollection<string> Logs { get; } = new ObservableCollection<string>();

        public List<string> OutputFormats { get; } = new List<string> { "WAV", "MP3" };

        private string _selectedFormat = "MP3";
        public string SelectedFormat
        {
            get => _selectedFormat;
            set { _selectedFormat = value; OnPropertyChanged(); }
        }

        private int _bitrate = 192;
        public int Bitrate
        {
            get => _bitrate;
            set { _bitrate = value; OnPropertyChanged(); }
        }

        private string _outputDirectory = string.Empty;
        public string OutputDirectory
        {
            get => _outputDirectory;
            set { _outputDirectory = value; OnPropertyChanged(); }
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

        public VideoToAudioViewModel()
        {
            _fileService = new FileService();
            _loggingService = new LoggingService();
            _settingsService = new SettingsService();

            OutputDirectory = _settingsService.DefaultOutputDirectory;

            BrowseOutputDirectoryCommand = new DelegateCommand(BrowseOutputDirectory);
            StartProcessingCommand = new DelegateCommand(StartProcessing);
            ExportAllCommand = new DelegateCommand(ExportAll);
            ClearQueueCommand = new DelegateCommand(ClearQueue);

            _loggingService.LogAdded += (s, e) => Logs.Add(e);
            _loggingService.Log("视频转音频页面已加载");
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

            foreach (var task in Tasks)
            {
                task.Status = TaskStatus.Processing;
                task.Progress = 0;
            }

            foreach (var task in Tasks)
            {
                CurrentTaskInfo = $"正在提取音频: {task.FileName}";
                _loggingService.Log($"提取音频: {task.FileName}");

                for (int i = 0; i <= 100; i += 10)
                {
                    task.Progress = i;
                    OverallProgress = (int)((Tasks.IndexOf(task) * 100 + i) / Tasks.Count);
                    await Task.Delay(200);
                }

                task.Status = TaskStatus.Completed;
            }

            CurrentTaskInfo = "提取完成";
        }

        private void ExportAll() => _loggingService.Log("导出所有音频");
        private void ClearQueue() { Tasks.Clear(); _loggingService.Log("队列已清空"); }

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
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
