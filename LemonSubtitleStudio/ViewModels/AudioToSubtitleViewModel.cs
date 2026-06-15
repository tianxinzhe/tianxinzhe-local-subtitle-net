
using Prism.Commands;
using LemonSubtitleStudio.Models;
using LemonSubtitleStudio.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace LemonSubtitleStudio.ViewModels
{
    public class AudioToSubtitleViewModel : INotifyPropertyChanged
    {
        private readonly IFileService _fileService;
        private readonly ILoggingService _loggingService;

        public ObservableCollection<TaskItem> Tasks { get; } = new ObservableCollection<TaskItem>();
        public ObservableCollection<SubtitleItem> Subtitles { get; } = new ObservableCollection<SubtitleItem>();
        public ObservableCollection<string> Logs { get; } = new ObservableCollection<string>();

        public List<string> Languages { get; } = new List<string> { "中文", "English", "日本語", "한국어" };
        public List<string> Models { get; } = new List<string> { "tiny", "base", "small", "medium", "large" };

        private string _selectedLanguage = "中文";
        public string SelectedLanguage
        {
            get => _selectedLanguage;
            set { _selectedLanguage = value; OnPropertyChanged(); }
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

        public AudioToSubtitleViewModel()
        {
            _fileService = new FileService();
            _loggingService = new LoggingService();

            BrowseOutputDirectoryCommand = new DelegateCommand(BrowseOutputDirectory);
            StartProcessingCommand = new DelegateCommand(StartProcessing);
            ExportAllCommand = new DelegateCommand(ExportAll);
            ClearQueueCommand = new DelegateCommand(ClearQueue);
            EditSubtitleCommand = new DelegateCommand<SubtitleItem>(EditSubtitle);

            _loggingService.LogAdded += (s, e) => Logs.Add(e);
            _loggingService.Log("音频转字幕页面已加载");
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
                CurrentTaskInfo = $"正在识别字幕: {task.FileName}";
                for (int i = 0; i <= 100; i += 10)
                {
                    task.Progress = i;
                    await Task.Delay(200);
                }
                task.Status = TaskStatus.Completed;
            }
            CurrentTaskInfo = "识别完成";
        }

        private void ExportAll() => _loggingService.Log("导出所有字幕");
        private void ClearQueue() { Tasks.Clear(); }
        private void EditSubtitle(SubtitleItem subtitle) => _loggingService.Log($"编辑字幕: {subtitle.Index}");

        public void DropHandler(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
                foreach (var file in files)
                {
                    var ext = System.IO.Path.GetExtension(file).ToLower();
                    if (ext is ".mp3" or ".wav" or ".flac" or ".ogg")
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
