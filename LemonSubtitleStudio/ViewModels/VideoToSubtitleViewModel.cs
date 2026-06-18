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
using System.Timers;
using TaskStatusEnum = LemonSubtitleStudio.Models.TaskStatus;

namespace LemonSubtitleStudio.ViewModels
{
    public class VideoToSubtitleViewModel : INotifyPropertyChanged, INavigationAware, IDisposable
    {
        private readonly IFileService _fileService;
        private readonly IAudioService _audioService;
        private readonly ITranscriptionService _transcriptionService;
        private readonly ISubtitleService _subtitleService;
        private readonly ILoggingService _loggingService;
        private readonly ISettingsService _settingsService;
        private readonly IRegionManager _regionManager;
        private readonly IHistoryService _historyService;
        private readonly Timer _performanceTimer;

        public ObservableCollection<TaskItem> Tasks { get; } = new ObservableCollection<TaskItem>();
        public ObservableCollection<SubtitleItem> Subtitles { get; } = new ObservableCollection<SubtitleItem>();
        public ObservableCollection<string> Logs { get; } = new ObservableCollection<string>();

        public List<string> Languages { get; } = new List<string> { "中文", "English", "日本語", "한국어" };
        public List<string> ProcessingModes { get; } = new List<string> { "高精度", "快速" };
        public List<string> AvailableModels { get; } = new List<string> { "tiny", "base", "small", "medium" };

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

        private int _audioExtractionProgress = 0;
        public int AudioExtractionProgress
        {
            get => _audioExtractionProgress;
            set { _audioExtractionProgress = value; OnPropertyChanged(); OnPropertyChanged(nameof(AudioExtractionStatus)); OnPropertyChanged(nameof(AudioExtractionIconKind)); }
        }

        private int _asrProgress = 0;
        public int AsrProgress
        {
            get => _asrProgress;
            set { _asrProgress = value; OnPropertyChanged(); OnPropertyChanged(nameof(AsrStatus)); OnPropertyChanged(nameof(AsrIconKind)); }
        }

        public string AudioExtractionStatus => AudioExtractionProgress >= 100 ? "Complete" : (AudioExtractionProgress > 0 ? "Running" : "Pending");
        public string AudioExtractionIconKind => AudioExtractionProgress >= 100 ? "CheckCircle" : (AudioExtractionProgress > 0 ? "Sync" : "CircleOutline");
        public string AsrStatus => AsrProgress >= 100 ? "Complete" : (AsrProgress > 0 ? "Running" : "Pending");
        public string AsrIconKind => AsrProgress >= 100 ? "CheckCircle" : (AsrProgress > 0 ? "Sync" : "CircleOutline");

        private string _currentTaskInfo = string.Empty;
        public string CurrentTaskInfo
        {
            get => _currentTaskInfo;
            set { _currentTaskInfo = value; OnPropertyChanged(); }
        }

        private string _selectedMediaPath = string.Empty;
        public string SelectedMediaPath
        {
            get => _selectedMediaPath;
            set { _selectedMediaPath = value; OnPropertyChanged(); }
        }

        public DelegateCommand BrowseOutputDirectoryCommand { get; }
        public DelegateCommand StartProcessingCommand { get; }
        public DelegateCommand ExportAllCommand { get; }
        public DelegateCommand ClearQueueCommand { get; }
        public DelegateCommand<SubtitleItem> EditSubtitleCommand { get; }
        public DelegateCommand AddFilesCommand { get; }
        public DelegateCommand SelectFileCommand { get; }
        public DelegateCommand<TaskItem> PreviewVideoCommand { get; }

        public VideoToSubtitleViewModel(IFileService fileService, IAudioService audioService,
            ITranscriptionService transcriptionService, ISubtitleService subtitleService,
            ILoggingService loggingService, ISettingsService settingsService,
            IRegionManager regionManager, IHistoryService historyService)
        {
            _fileService = fileService;
            _audioService = audioService;
            _transcriptionService = transcriptionService;
            _subtitleService = subtitleService;
            _loggingService = loggingService;
            _settingsService = settingsService;
            _regionManager = regionManager;
            _historyService = historyService;

            OutputDirectory = _settingsService.DefaultOutputDirectory;

            BrowseOutputDirectoryCommand = new DelegateCommand(BrowseOutputDirectory);
            StartProcessingCommand = new DelegateCommand(async () => await StartProcessing());
            ExportAllCommand = new DelegateCommand(ExportAll);
            ClearQueueCommand = new DelegateCommand(ClearQueue);
            EditSubtitleCommand = new DelegateCommand<SubtitleItem>(EditSubtitle);
            AddFilesCommand = new DelegateCommand(AddFiles);
            SelectFileCommand = new DelegateCommand(AddFiles);
            PreviewVideoCommand = new DelegateCommand<TaskItem>(PreviewVideo);

            _loggingService.LogAdded += (s, e) => Logs.Add(e);
            _loggingService.Log("视频转字幕页面已加载");

            _performanceTimer = new Timer(1000);
            _performanceTimer.Elapsed += OnPerformanceTimerElapsed;
            _performanceTimer.Start();
        }

        private void OnPerformanceTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            try
            {
                var proc = System.Diagnostics.Process.GetCurrentProcess();
                var memoryMb = proc.WorkingSet64 / 1024 / 1024;
                var cpuValue = GetCpuUsage();
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    MemoryUsage = $"{memoryMb}MB";
                    CpuUsage = $"{cpuValue:F1}%";
                });
            }
            catch { }
        }

        private static double GetCpuUsage()
        {
            try
            {
                var proc = System.Diagnostics.Process.GetCurrentProcess();
                var startCpu = proc.TotalProcessorTime;
                var startTime = DateTime.UtcNow;
                System.Threading.Thread.Sleep(200);
                proc.Refresh();
                var endCpu = proc.TotalProcessorTime;
                var endTime = DateTime.UtcNow;
                var cpuUsedMs = (endCpu - startCpu).TotalMilliseconds;
                var totalMsPassed = (endTime - startTime).TotalMilliseconds;
                return (cpuUsedMs / totalMsPassed) / Environment.ProcessorCount * 100;
            }
            catch { return 0; }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _performanceTimer?.Stop();
                _performanceTimer?.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
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
            if (!_audioService.IsFfmpegAvailable())
            {
                _loggingService.LogError("FFmpeg 不可用，请确保已正确安装");
                return;
            }

            _loggingService.Log("开始处理任务...");
            foreach (var task in Tasks)
            {
                task.Status = TaskStatusEnum.Processing;
                task.Progress = 0;
            }

            foreach (var task in Tasks)
            {
                CurrentTaskInfo = $"正在处理: {task.FileName}";
                _loggingService.Log($"处理文件: {task.FileName}");

                try
                {
                    task.Progress = 0;
                    task.Status = TaskStatusEnum.Processing;

                    AudioExtractionProgress = 0;
                    AsrProgress = 0;

                    CurrentTaskInfo = $"提取音频: {task.FileName}";
                    _loggingService.Log("阶段1/3: 提取音频");

                    var wavPath = await _audioService.ConvertToWavAsync(task.InputPath, OutputDirectory);
                    task.Progress = 33;
                    AudioExtractionProgress = 100;

                    CurrentTaskInfo = $"语音识别: {task.FileName}";
                    _loggingService.Log("阶段2/3: 语音识别");

                    var progress = new Progress<int>(p =>
                    {
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            AsrProgress = p;
                            task.Progress = 33 + (int)(p * 0.66);
                        });
                    });

                    var subtitles = await _transcriptionService.TranscribeAsync(
                        wavPath, SelectedLanguage, SelectedModel, progress);
                    
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

                    File.Delete(wavPath);
                }
                catch (Exception ex)
                {
                    task.Status = TaskStatusEnum.Failed;
                    task.ErrorMessage = ex.Message;
                    _loggingService.LogError($"处理失败: {task.FileName}", ex);

                    await _historyService.AddRecordAsync(task.InputPath, string.Empty, TaskStatusEnum.Failed, ex.Message, DateTime.Now);
                }

                OverallProgress = (Tasks.IndexOf(task) + 1) * 100 / Tasks.Count;
            }

            CurrentTaskInfo = "所有任务已完成";
            _loggingService.Log("处理完成");
        }

        private void ExportAll()
        {
            foreach (var task in Tasks)
            {
                if (task.Status == TaskStatusEnum.Completed && !string.IsNullOrEmpty(task.OutputPath))
                {
                    _fileService.EnsureDirectoryExists(OutputDirectory);
                    _loggingService.Log($"导出字幕: {task.OutputPath}");
                }
            }
        }

        private void ClearQueue()
        {
            Tasks.Clear();
            Subtitles.Clear();
            _loggingService.Log("队列已清空");
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
            navigationParams.Add("MediaPath", SelectedMediaPath);
            navigationParams.Add("SubtitlePath", completedTask.OutputPath);
            _regionManager.RequestNavigate("ContentRegion", "SubtitleEditorView", navigationParams);
        }

        private void PreviewVideo(TaskItem task)
        {
            if (task.Status == TaskStatusEnum.Completed)
            {
                SelectedMediaPath = task.InputPath;
            }
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
                    Status = TaskStatusEnum.Waiting
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
                            Status = TaskStatusEnum.Waiting
                        });
                    }
                }
                _loggingService.Log($"拖拽添加了 {files.Length} 个文件");
            }
        }

        public void OnNavigatedTo(NavigationContext navigationContext) { }

        public void OnNavigatedFrom(NavigationContext navigationContext) { }

        public bool IsNavigationTarget(NavigationContext navigationContext) => true;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}