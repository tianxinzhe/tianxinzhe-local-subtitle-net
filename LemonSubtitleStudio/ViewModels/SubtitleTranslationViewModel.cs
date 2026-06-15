
using Prism.Commands;
using LemonSubtitleStudio.Models;
using LemonSubtitleStudio.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace LemonSubtitleStudio.ViewModels
{
    public class SubtitleTranslationViewModel : INotifyPropertyChanged
    {
        private readonly IFileService _fileService;
        private readonly ILoggingService _loggingService;

        public ObservableCollection<TaskItem> Tasks { get; } = new ObservableCollection<TaskItem>();
        public ObservableCollection<SubtitleItem> Subtitles { get; } = new ObservableCollection<SubtitleItem>();

        public List<string> Languages { get; } = new List<string> { "中文", "English", "日本語", "한국어" };

        private string _sourceLanguage = "English";
        public string SourceLanguage
        {
            get => _sourceLanguage;
            set { _sourceLanguage = value; OnPropertyChanged(); }
        }

        private string _targetLanguage = "中文";
        public string TargetLanguage
        {
            get => _targetLanguage;
            set { _targetLanguage = value; OnPropertyChanged(); }
        }

        private string _outputDirectory = string.Empty;
        public string OutputDirectory
        {
            get => _outputDirectory;
            set { _outputDirectory = value; OnPropertyChanged(); }
        }

        public DelegateCommand BrowseOutputDirectoryCommand { get; }
        public DelegateCommand StartProcessingCommand { get; }
        public DelegateCommand ExportAllCommand { get; }
        public DelegateCommand ClearQueueCommand { get; }

        public SubtitleTranslationViewModel()
        {
            _fileService = new FileService();
            _loggingService = new LoggingService();

            BrowseOutputDirectoryCommand = new DelegateCommand(BrowseOutputDirectory);
            StartProcessingCommand = new DelegateCommand(StartProcessing);
            ExportAllCommand = new DelegateCommand(ExportAll);
            ClearQueueCommand = new DelegateCommand(ClearQueue);
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
                for (int i = 0; i <= 100; i += 10)
                {
                    task.Progress = i;
                    await Task.Delay(200);
                }
                task.Status = TaskStatus.Completed;
            }
        }

        private void ExportAll() => _loggingService.Log("导出所有翻译字幕");
        private void ClearQueue() { Tasks.Clear(); }

        public void DropHandler(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
                foreach (var file in files)
                {
                    var ext = System.IO.Path.GetExtension(file).ToLower();
                    if (ext is ".srt" or ".vtt" or ".ass")
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
