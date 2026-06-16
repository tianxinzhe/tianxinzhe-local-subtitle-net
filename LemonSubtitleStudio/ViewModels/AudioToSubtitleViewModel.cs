using Prism.Commands;
using Prism.Regions;
using LemonSubtitleStudio.Models;
using LemonSubtitleStudio.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using TaskStatusEnum = LemonSubtitleStudio.Models.TaskStatus;

namespace LemonSubtitleStudio.ViewModels
{
    public class AudioToSubtitleViewModel : INotifyPropertyChanged
    {
        private readonly IFileService _fileService;
        private readonly ITranscriptionService _transcriptionService;
        private readonly ISubtitleService _subtitleService;
        private readonly ILoggingService _loggingService;
        private readonly IRegionManager _regionManager;
        private readonly IHistoryService _historyService;
        private readonly ISettingsService _settingsService;

        public ObservableCollection<TaskItem> Tasks { get; } = new ObservableCollection<TaskItem>();
        public ObservableCollection<SubtitleItem> Subtitles { get; } = new ObservableCollection<SubtitleItem>();
        public ObservableCollection<string> Logs { get; } = new ObservableCollection<string>();

        public List<string> Languages { get; } = new List<string> { "中文", "English", "日本語", "한국어" };
        public List<string> AvailableModels { get; } = new List<string> { "tiny", "base", "small", "medium" };

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
        public DelegateCommand SelectFileCommand { get; }

        public AudioToSubtitleViewModel(IFileService fileService, ITranscriptionService transcriptionService,
            ISubtitleService subtitleService, ILoggingService loggingService,
            IRegionManager regionManager, IHistoryService historyService, ISettingsService settingsService)
        {
            _fileService = fileService;
            _transcriptionService = transcriptionService;
            _subtitleService = subtitleService;
            _loggingService = loggingService;
            _regionManager = regionManager;
            _historyService = historyService;
            _settingsService = settingsService;

            OutputDirectory = _settingsService.DefaultOutputDirectory;
            BrowseOutputDirectoryCommand = new DelegateCommand(BrowseOutputDirectory);
            StartProcessingCommand = new DelegateCommand(async () => await StartProcessing());
            ExportAllCommand = new DelegateCommand(ExportAll);
            ClearQueueCommand = new DelegateCommand(ClearQueue);
            EditSubtitleCommand = new DelegateCommand<SubtitleItem>(EditSubtitle);
            SelectFileCommand = new DelegateCommand(AddFiles);

            _loggingService.LogAdded += (s, e) => Logs.Add(e);
            _loggingService.Log("音频转字幕页面已加载");
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

            _loggingService.Log("开始识别字幕...");
            foreach (var task in Tasks)
            {
                task.Status = TaskStatusEnum.Processing;
                task.Progress = 0;
            }

            foreach (var task in Tasks)
            {
                CurrentTaskInfo = $"正在识别字幕: {task.FileName}";
                _loggingService.Log($"识别字幕: {task.FileName}");

                try
                {
                    var progress = new Progress<int>(p =>
                    {
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            task.Progress = p;
                            OverallProgress = (int)((Tasks.IndexOf(task) * 100 + p) / Tasks.Count);
                        });
                    });

                    var subtitles = await _transcriptionService.TranscribeAsync(
                        task.InputPath, SelectedLanguage, SelectedModel, progress);

                    Subtitles.Clear();
                    foreach (var sub in subtitles)
                    {
                        Subtitles.Add(sub);
                    }

                    task.Progress = 100;

                    var srtPath = Path.Combine(OutputDirectory, $"{Path.GetFileNameWithoutExtension(task.InputPath)}.srt");
                    _subtitleService.SaveToSrt(srtPath, subtitles);
                    task.OutputPath = srtPath;
                    task.MediaPath = task.InputPath;

                    task.Status = TaskStatusEnum.Completed;
                    _loggingService.Log($"完成: {task.FileName} -> {srtPath}");
                    await _historyService.AddRecordAsync(task.InputPath, srtPath, TaskStatusEnum.Completed, string.Empty, DateTime.Now);
                }
                catch (Exception ex)
                {
                    task.Status = TaskStatusEnum.Failed;
                    task.ErrorMessage = ex.Message;
                    _loggingService.LogError($"识别失败: {task.FileName}", ex);
                    await _historyService.AddRecordAsync(task.InputPath, string.Empty, TaskStatusEnum.Failed, ex.Message, DateTime.Now);
                }

                OverallProgress = (Tasks.IndexOf(task) + 1) * 100 / Tasks.Count;
            }

            CurrentTaskInfo = "识别完成";
            _loggingService.Log("所有音频识别完成");
        }

        private void ExportAll()
        {
            foreach (var task in Tasks)
            {
                if (task.Status == TaskStatusEnum.Completed && !string.IsNullOrEmpty(task.OutputPath))
                {
                    _loggingService.Log($"导出字幕: {task.OutputPath}");
                }
            }
        }

        private void ClearQueue() { Tasks.Clear(); Subtitles.Clear(); }

        private void AddFiles()
        {
            var files = _fileService.SelectFiles("音频文件|*.mp3;*.wav;*.flac;*.ogg");
            foreach (var file in files)
            {
                Tasks.Add(new TaskItem
                {
                    InputPath = file,
                    FileName = System.IO.Path.GetFileName(file),
                    Status = TaskStatusEnum.Waiting
                });
            }
            _loggingService.Log($"添加了 {files.Length} 个文件");
        }

        private void EditSubtitle(SubtitleItem subtitle)
        {
            var completedTask = Tasks.FirstOrDefault(t => t.Status == TaskStatusEnum.Completed);
            if (completedTask == null)
            {
                _loggingService.LogWarning("没有已完成的任务，无法编辑字幕");
                return;
            }

            var navigationParams = new NavigationParameters();
            navigationParams.Add("MediaPath", completedTask.MediaPath);
            navigationParams.Add("SubtitlePath", completedTask.OutputPath);
            _regionManager.RequestNavigate("ContentRegion", "SubtitleEditorView", navigationParams);
        }

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
                            Status = TaskStatusEnum.Waiting
                        });
                    }
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}