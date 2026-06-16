using Prism.Commands;
using LemonSubtitleStudio.Models;
using LemonSubtitleStudio.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace LemonSubtitleStudio.ViewModels
{
    public class VideoToAudioViewModel : INotifyPropertyChanged
    {
        private readonly IFileService _fileService;
        private readonly IAudioService _audioService;
        private readonly ILoggingService _loggingService;
        private readonly ISettingsService _settingsService;
        private readonly IHistoryService _historyService;

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

            _loggingService.LogAdded += (s, e) => Logs.Add(e);
            _loggingService.Log("视频转音频页面已加载");
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