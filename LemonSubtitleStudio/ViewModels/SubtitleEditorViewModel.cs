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
using System.Windows.Media;

namespace LemonSubtitleStudio.ViewModels
{
    public class SubtitleEditorViewModel : INotifyPropertyChanged, INavigationAware
    {
        private readonly IFileService _fileService;
        private readonly ISubtitleService _subtitleService;
        private readonly ILoggingService _loggingService;

        public ObservableCollection<SubtitleItem> Subtitles { get; } = new ObservableCollection<SubtitleItem>();

        private string _currentFileName = string.Empty;
        public string CurrentFileName
        {
            get => _currentFileName;
            set { _currentFileName = value; OnPropertyChanged(); }
        }

        private string _mediaPath = string.Empty;
        public string MediaPath
        {
            get => _mediaPath;
            set { _mediaPath = value; OnPropertyChanged(); }
        }

        private string _subtitlePath = string.Empty;
        public string SubtitlePath
        {
            get => _subtitlePath;
            set { _subtitlePath = value; OnPropertyChanged(); }
        }

        private SubtitleItem? _selectedSubtitle;
        public SubtitleItem? SelectedSubtitle
        {
            get => _selectedSubtitle;
            set 
            { 
                _selectedSubtitle = value; 
                OnPropertyChanged();
                if (value != null)
                {
                    EditStartTime = value.StartTimeFormatted;
                    EditEndTime = value.EndTimeFormatted;
                }
            }
        }

        private string _editStartTime = "00:00:00.000";
        public string EditStartTime
        {
            get => _editStartTime;
            set { _editStartTime = value; OnPropertyChanged(); }
        }

        private string _editEndTime = "00:00:00.000";
        public string EditEndTime
        {
            get => _editEndTime;
            set 
            { 
                _editEndTime = value; 
                OnPropertyChanged();
                OnPropertyChanged(nameof(DurationFormatted));
                OnPropertyChanged(nameof(CpsFormatted));
            }
        }

        public string DurationFormatted
        {
            get
            {
                try
                {
                    if (TimeSpan.TryParse(EditStartTime, out var start) && TimeSpan.TryParse(EditEndTime, out var end))
                    {
                        var duration = end - start;
                        if (duration.TotalSeconds < 0) duration = TimeSpan.Zero;
                        return $"{(int)duration.TotalMinutes:D2}:{duration.Seconds:D2}.{duration.Milliseconds / 10:D2}";
                    }
                }
                catch { }
                return "00:00.00";
            }
        }

        public string CpsFormatted
        {
            get
            {
                try
                {
                    if (TimeSpan.TryParse(EditStartTime, out var start) && TimeSpan.TryParse(EditEndTime, out var end))
                    {
                        var duration = (end - start).TotalSeconds;
                        if (duration <= 0) return "0.0";
                        var textLength = SelectedSubtitle?.OriginalText?.Length ?? 0;
                        return (textLength / duration).ToString("F1");
                    }
                }
                catch { }
                return "0.0";
            }
        }

        private int _playbackProgress = 0;
        public int PlaybackProgress
        {
            get => _playbackProgress;
            set { _playbackProgress = value; OnPropertyChanged(); }
        }

        private string _currentTime = "00:00:00";
        public string CurrentTime
        {
            get => _currentTime;
            set { _currentTime = value; OnPropertyChanged(); }
        }

        private string _totalTime = "00:00:00";
        public string TotalTime
        {
            get => _totalTime;
            set { _totalTime = value; OnPropertyChanged(); UpdateTimelineMarkers(); }
        }

        private ObservableCollection<string> _timelineMarkers = new ObservableCollection<string>();
        public ObservableCollection<string> TimelineMarkers
        {
            get => _timelineMarkers;
            set { _timelineMarkers = value; OnPropertyChanged(); }
        }

        private void UpdateTimelineMarkers()
        {
            TimelineMarkers.Clear();
            if (!TimeSpan.TryParse(TotalTime, out var total)) return;
            if (total.TotalSeconds <= 0) return;

            int count = (int)Math.Ceiling(total.TotalMinutes / 5);
            if (count < 2) count = 2;
            if (count > 20) count = 20;

            for (int i = 0; i <= count; i++)
            {
                var t = TimeSpan.FromSeconds(total.TotalSeconds * i / count);
                TimelineMarkers.Add(t.ToString(@"hh\:mm\:ss"));
            }
        }

        private string _statusMessage = string.Empty;
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public DelegateCommand OpenMediaCommand { get; }
        public DelegateCommand OpenSubtitleCommand { get; }
        public DelegateCommand SaveCommand { get; }
        public DelegateCommand ExportAllCommand { get; }
        public DelegateCommand AddSubtitleCommand { get; }
        public DelegateCommand DeleteSubtitleCommand { get; }
        public DelegateCommand PlayCommand { get; }
        public DelegateCommand PauseCommand { get; }
        public DelegateCommand StopCommand { get; }

        public event EventHandler<MediaPlayerCommandEventArgs>? MediaPlayerCommand;

        public SubtitleEditorViewModel(IFileService fileService, ISubtitleService subtitleService, ILoggingService loggingService)
        {
            _fileService = fileService;
            _subtitleService = subtitleService;
            _loggingService = loggingService;

            OpenMediaCommand = new DelegateCommand(OpenMedia);
            OpenSubtitleCommand = new DelegateCommand(OpenSubtitle);
            SaveCommand = new DelegateCommand(Save);
            ExportAllCommand = new DelegateCommand(ExportAll);
            AddSubtitleCommand = new DelegateCommand(AddSubtitle);
            DeleteSubtitleCommand = new DelegateCommand(DeleteSubtitle);
            PlayCommand = new DelegateCommand(() => MediaPlayerCommand?.Invoke(this, new MediaPlayerCommandEventArgs(MediaCommand.Play)));
            PauseCommand = new DelegateCommand(() => MediaPlayerCommand?.Invoke(this, new MediaPlayerCommandEventArgs(MediaCommand.Pause)));
            StopCommand = new DelegateCommand(() => MediaPlayerCommand?.Invoke(this, new MediaPlayerCommandEventArgs(MediaCommand.Stop)));

            StatusMessage = "请打开视频/音频和字幕文件";
        }

        private void OpenMedia()
        {
            var files = _fileService.SelectFiles("媒体文件|*.mp4;*.mkv;*.avi;*.mov;*.mp3;*.wav");
            if (files.Any())
            {
                MediaPath = files[0];
                CurrentFileName = System.IO.Path.GetFileName(files[0]);
                StatusMessage = $"已加载媒体: {CurrentFileName}";
                MediaPlayerCommand?.Invoke(this, new MediaPlayerCommandEventArgs(MediaCommand.Open, MediaPath));
            }
        }

        private void OpenSubtitle()
        {
            var files = _fileService.SelectFiles("字幕文件|*.srt;*.vtt;*.ass");
            if (files.Any())
            {
                SubtitlePath = files[0];
                LoadSubtitles(SubtitlePath);
            }
        }

        private void LoadSubtitles(string path)
        {
            Subtitles.Clear();
            var ext = Path.GetExtension(path).ToLower();
            List<SubtitleItem> subtitles;
            
            try
            {
                subtitles = ext switch
                {
                    ".srt" => _subtitleService.LoadFromSrt(path),
                    ".vtt" => _subtitleService.LoadFromVtt(path),
                    _ => new List<SubtitleItem>()
                };

                foreach (var sub in subtitles)
                {
                    Subtitles.Add(sub);
                }
                StatusMessage = $"已加载 {Subtitles.Count} 条字幕";
            }
            catch (Exception ex)
            {
                StatusMessage = $"加载字幕失败: {ex.Message}";
                _loggingService.LogError("加载字幕失败", ex);
            }
        }

        private void Save()
        {
            if (string.IsNullOrEmpty(SubtitlePath))
            {
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "SRT 文件|*.srt|VTT 文件|*.vtt",
                    DefaultExt = ".srt"
                };

                if (dialog.ShowDialog() == true)
                {
                    SubtitlePath = dialog.FileName;
                }
                else
                {
                    return;
                }
            }

            try
            {
                var ext = Path.GetExtension(SubtitlePath).ToLower();
                if (ext == ".srt")
                {
                    _subtitleService.SaveToSrt(SubtitlePath, Subtitles.ToList());
                }
                else if (ext == ".vtt")
                {
                    _subtitleService.SaveToVtt(SubtitlePath, Subtitles.ToList());
                }

                StatusMessage = "字幕已保存";
                _loggingService.Log($"字幕已保存到: {SubtitlePath}");
            }
            catch (Exception ex)
            {
                StatusMessage = $"保存失败: {ex.Message}";
                _loggingService.LogError("保存字幕失败", ex);
            }
        }

        private void ExportAll()
        {
            if (Subtitles.Count == 0)
            {
                StatusMessage = "没有可导出的字幕";
                return;
            }

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "SRT 文件|*.srt|VTT 文件|*.vtt",
                DefaultExt = ".srt",
                FileName = Path.GetFileNameWithoutExtension(CurrentFileName) + "_exported"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var ext = Path.GetExtension(dialog.FileName).ToLower();
                    if (ext == ".srt")
                    {
                        _subtitleService.SaveToSrt(dialog.FileName, Subtitles.ToList());
                    }
                    else if (ext == ".vtt")
                    {
                        _subtitleService.SaveToVtt(dialog.FileName, Subtitles.ToList());
                    }
                    StatusMessage = $"已导出 {Subtitles.Count} 条字幕";
                    _loggingService.Log($"字幕已导出到: {dialog.FileName}");
                }
                catch (Exception ex)
                {
                    StatusMessage = $"导出失败: {ex.Message}";
                    _loggingService.LogError("导出字幕失败", ex);
                }
            }
        }

        private void AddSubtitle()
        {
            var lastSubtitle = Subtitles.LastOrDefault();
            var startTime = lastSubtitle != null ? lastSubtitle.EndTime : TimeSpan.Zero;
            var endTime = startTime + TimeSpan.FromSeconds(2);

            Subtitles.Add(new SubtitleItem
            {
                Index = Subtitles.Count + 1,
                StartTime = startTime,
                EndTime = endTime,
                OriginalText = "新字幕"
            });
        }

        private void DeleteSubtitle()
        {
            if (SelectedSubtitle != null)
            {
                Subtitles.Remove(SelectedSubtitle);
                RenumberSubtitles();
            }
        }

        private void RenumberSubtitles()
        {
            int index = 1;
            foreach (var sub in Subtitles)
            {
                sub.Index = index++;
            }
        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            if (navigationContext.Parameters.TryGetValue("MediaPath", out string mediaPath))
            {
                MediaPath = mediaPath;
                CurrentFileName = System.IO.Path.GetFileName(mediaPath);
                MediaPlayerCommand?.Invoke(this, new MediaPlayerCommandEventArgs(MediaCommand.Open, mediaPath));
            }

            if (navigationContext.Parameters.TryGetValue("SubtitlePath", out string subtitlePath))
            {
                SubtitlePath = subtitlePath;
                LoadSubtitles(subtitlePath);
            }
        }

        public void OnNavigatedFrom(NavigationContext navigationContext) { }

        public bool IsNavigationTarget(NavigationContext navigationContext) => true;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public enum MediaCommand
    {
        Open,
        Play,
        Pause,
        Stop,
        Seek
    }

    public class MediaPlayerCommandEventArgs : EventArgs
    {
        public MediaCommand Command { get; }
        public string? Parameter { get; }

        public MediaPlayerCommandEventArgs(MediaCommand command, string? parameter = null)
        {
            Command = command;
            Parameter = parameter;
        }
    }
}