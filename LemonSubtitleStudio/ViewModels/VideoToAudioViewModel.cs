using Prism.Commands;
using LemonSubtitleStudio.Models;
using LemonSubtitleStudio.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Timers;

namespace LemonSubtitleStudio.ViewModels
{
    public class VideoToAudioViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly IFileService _fileService;
        private readonly IAudioService _audioService;
        private readonly ILoggingService _loggingService;
        private readonly ISettingsService _settingsService;
        private readonly IHistoryService _historyService;
        private readonly Timer _performanceTimer;

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

        public int PendingCount => Tasks.Count(t => t.Status == Models.TaskStatus.Waiting);

        private string _lastCompletedFileName = string.Empty;
        public string LastCompletedFileName
        {
            get => _lastCompletedFileName;
            set { _lastCompletedFileName = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasCompletedTask)); }
        }

        private string _lastCompletedOutputPath = string.Empty;
        public string LastCompletedOutputPath
        {
            get => _lastCompletedOutputPath;
            set { _lastCompletedOutputPath = value; OnPropertyChanged(); }
        }

        public bool HasCompletedTask => !string.IsNullOrEmpty(LastCompletedOutputPath);

        public DelegateCommand PlayCommand { get; }
        public DelegateCommand SkipBackCommand { get; }
        public DelegateCommand SkipForwardCommand { get; }

        public DelegateCommand BrowseOutputDirectoryCommand { get; }
        public DelegateCommand StartProcessingCommand { get; }
        public DelegateCommand ExportAllCommand { get; }
        public DelegateCommand ClearQueueCommand { get; }
        public DelegateCommand SelectFileCommand { get; }

        public VideoToAudioViewModel(IFileService fileService, IAudioService audioService,
            ILoggingService loggingService, ISettingsService settingsService, IHistoryService historyService)
        {
            _fileService = fileService;
            _audioService = audioService;
            _loggingService = loggingService;
            _settingsService = settingsService;
            _historyService = historyService;

            OutputDirectory = _settingsService.DefaultOutputDirectory;

            BrowseOutputDirectoryCommand = new DelegateCommand(BrowseOutputDirectory);
            StartProcessingCommand = new DelegateCommand(async () => await StartProcessing());
            ExportAllCommand = new DelegateCommand(ExportAll);
            ClearQueueCommand = new DelegateCommand(ClearQueue);
            SelectFileCommand = new DelegateCommand(AddFiles);
            PlayCommand = new DelegateCommand(Play);
            SkipBackCommand = new DelegateCommand(SkipBack);
            SkipForwardCommand = new DelegateCommand(SkipForward);

            _loggingService.LogAdded += (s, e) => Logs.Add(e);
            _loggingService.Log("视频转音频页面已加载");

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

            foreach (var task in Tasks)
            {
                task.Status = Models.TaskStatus.Processing;
                task.Progress = 0;
            }

            foreach (var task in Tasks)
            {
                CurrentTaskInfo = $"正在提取音频: {task.FileName}";
                _loggingService.Log($"提取音频: {task.FileName}");

                try
                {
                    var outputPath = await _audioService.ExtractAudioAsync(
                        task.InputPath, OutputDirectory, SelectedFormat, Bitrate);
                    
                    task.OutputPath = outputPath;
                    task.Status = Models.TaskStatus.Completed;
                    task.Progress = 100;
                    LastCompletedFileName = task.FileName;
                    LastCompletedOutputPath = outputPath;

                    _loggingService.Log($"完成: {task.FileName} -> {outputPath}");
                    await _historyService.AddRecordAsync(task.InputPath, outputPath, TaskStatus.Completed, string.Empty, DateTime.Now);
                }
                catch (Exception ex)
                {
                    task.Status = Models.TaskStatus.Failed;
                    task.ErrorMessage = ex.Message;
                    _loggingService.LogError($"提取失败: {task.FileName}", ex);
                    await _historyService.AddRecordAsync(task.InputPath, string.Empty, TaskStatus.Failed, ex.Message, DateTime.Now);
                }

                OverallProgress = (Tasks.IndexOf(task) + 1) * 100 / Tasks.Count;
            }

            CurrentTaskInfo = "提取完成";
            _loggingService.Log("所有音频提取完成");
        }

        private void ExportAll()
        {
            foreach (var task in Tasks)
            {
                if (task.Status == Models.TaskStatus.Completed && !string.IsNullOrEmpty(task.OutputPath))
                {
                    _loggingService.Log($"导出音频: {task.OutputPath}");
                }
            }
        }

        private void ClearQueue() { Tasks.Clear(); _loggingService.Log("队列已清空"); }

        private void Play()
        {
            var completedTask = Tasks.FirstOrDefault(t => t.Status == Models.TaskStatus.Completed && !string.IsNullOrEmpty(t.OutputPath));
            if (completedTask != null)
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = completedTask.OutputPath,
                    UseShellExecute = true
                });
                _loggingService.Log($"播放: {completedTask.FileName}");
            }
            else
            {
                _loggingService.LogWarning("没有已完成的任务可以播放");
            }
        }

        private void SkipBack()
        {
            _loggingService.Log("后退 10 秒");
        }

        private void SkipForward()
        {
            _loggingService.Log("前进 10 秒");
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
                    Status = Models.TaskStatus.Waiting
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
                            Status = Models.TaskStatus.Waiting
                        });
                    }
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}