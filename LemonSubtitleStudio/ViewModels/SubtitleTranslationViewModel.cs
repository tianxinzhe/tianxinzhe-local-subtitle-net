using Prism.Commands;
using LemonSubtitleStudio.Models;
using LemonSubtitleStudio.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;

namespace LemonSubtitleStudio.ViewModels
{
    public class SubtitleTranslationViewModel : INotifyPropertyChanged
    {
        private readonly IFileService _fileService;
        private readonly ITranslationService _translationService;
        private readonly ISubtitleService _subtitleService;
        private readonly ILoggingService _loggingService;
        private readonly IHistoryService _historyService;
        private readonly ISettingsService _settingsService;

        public ObservableCollection<TaskItem> Tasks { get; } = new ObservableCollection<TaskItem>();
        public ObservableCollection<SubtitleItem> Subtitles { get; } = new ObservableCollection<SubtitleItem>();
        public ObservableCollection<string> Logs { get; } = new ObservableCollection<string>();

        public List<string> Languages { get; } = new List<string> { "中文", "English", "日本語", "한국어" };
        public List<string> TranslationModels { get; } = new List<string> { "marianmt-en-zh", "marianmt-zh-en" };

        private string _selectedTranslationModel = "marianmt-en-zh";
        public string SelectedTranslationModel
        {
            get => _selectedTranslationModel;
            set { _selectedTranslationModel = value; OnPropertyChanged(); }
        }

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

        private int _overallProgress = 0;
        public int OverallProgress
        {
            get => _overallProgress;
            set { _overallProgress = value; OnPropertyChanged(); }
        }

        public DelegateCommand BrowseOutputDirectoryCommand { get; }
        public DelegateCommand StartProcessingCommand { get; }
        public DelegateCommand ExportAllCommand { get; }
        public DelegateCommand ClearQueueCommand { get; }
        public DelegateCommand SelectFileCommand { get; }

        public SubtitleTranslationViewModel(IFileService fileService, ITranslationService translationService,
            ISubtitleService subtitleService, ILoggingService loggingService, IHistoryService historyService, ISettingsService settingsService)
        {
            _fileService = fileService;
            _translationService = translationService;
            _subtitleService = subtitleService;
            _loggingService = loggingService;
            _historyService = historyService;
            _settingsService = settingsService;

            OutputDirectory = _settingsService.DefaultOutputDirectory;
            BrowseOutputDirectoryCommand = new DelegateCommand(BrowseOutputDirectory);
            StartProcessingCommand = new DelegateCommand(async () => await StartProcessing());
            ExportAllCommand = new DelegateCommand(ExportAll);
            ClearQueueCommand = new DelegateCommand(ClearQueue);
            SelectFileCommand = new DelegateCommand(AddFiles);
        }

        private void BrowseOutputDirectory()
        {
            var folder = _fileService.SelectFolder("选择输出目录");
            if (!string.IsNullOrEmpty(folder))
                OutputDirectory = folder;
        }

        private async System.Threading.Tasks.Task StartProcessing()
        {
            if (Tasks.Count == 0) return;

            foreach (var task in Tasks)
            {
                task.Status = LemonSubtitleStudio.Models.TaskStatus.Processing;
                task.Progress = 0;
            }

            foreach (var task in Tasks)
            {
                try
                {
                    var subtitles = LoadSubtitles(task.InputPath);
                    Subtitles.Clear();
                    foreach (var sub in subtitles)
                    {
                        Subtitles.Add(sub);
                    }

                    var progress = new Progress<int>(p =>
                    {
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            task.Progress = p;
                            OverallProgress = (int)((Tasks.IndexOf(task) * 100 + p) / Tasks.Count);
                        });
                    });

                    var modelName = SourceLanguage == "中文" ? "marianmt-zh-en" : "marianmt-en-zh";
                    var translatedSubtitles = await _translationService.TranslateSubtitlesAsync(
                        subtitles, SourceLanguage, TargetLanguage, modelName, progress);

                    Subtitles.Clear();
                    foreach (var sub in translatedSubtitles)
                    {
                        Subtitles.Add(sub);
                    }

                    task.Progress = 100;

                    var outputPath = Path.Combine(OutputDirectory, 
                        $"{Path.GetFileNameWithoutExtension(task.InputPath)}_translated.srt");
                    _subtitleService.SaveToBilingualSrt(outputPath, translatedSubtitles);
                    task.OutputPath = outputPath;

                    task.Status = LemonSubtitleStudio.Models.TaskStatus.Completed;
                    await _historyService.AddRecordAsync(task.InputPath, outputPath, Models.TaskStatus.Completed, string.Empty, DateTime.Now);
                }
                catch (Exception ex)
                {
                    task.Status = LemonSubtitleStudio.Models.TaskStatus.Failed;
                    task.ErrorMessage = ex.Message;
                    await _historyService.AddRecordAsync(task.InputPath, string.Empty, Models.TaskStatus.Failed, ex.Message, DateTime.Now);
                }

                OverallProgress = (Tasks.IndexOf(task) + 1) * 100 / Tasks.Count;
            }
        }

        private List<SubtitleItem> LoadSubtitles(string path)
        {
            var ext = Path.GetExtension(path).ToLower();
            return ext switch
            {
                ".srt" => _subtitleService.LoadFromSrt(path),
                ".vtt" => _subtitleService.LoadFromVtt(path),
                _ => new List<SubtitleItem>()
            };
        }

        private void ExportAll()
        {
            foreach (var task in Tasks)
            {
                if (task.Status == Models.TaskStatus.Completed && !string.IsNullOrEmpty(task.OutputPath))
                {
                    _loggingService.Log($"导出翻译字幕: {task.OutputPath}");
                }
            }
        }

        private void ClearQueue() { Tasks.Clear(); Subtitles.Clear(); }

        private void AddFiles()
        {
            var files = _fileService.SelectFiles("字幕文件|*.srt;*.vtt;*.ass");
            foreach (var file in files)
            {
                Tasks.Add(new TaskItem
                {
                    InputPath = file,
                    FileName = System.IO.Path.GetFileName(file),
                    Status = LemonSubtitleStudio.Models.TaskStatus.Waiting
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
                    if (ext is ".srt" or ".vtt" or ".ass")
                    {
                        Tasks.Add(new TaskItem
                        {
                            InputPath = file,
                            FileName = System.IO.Path.GetFileName(file),
                            Status = LemonSubtitleStudio.Models.TaskStatus.Waiting
                        });
                    }
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}