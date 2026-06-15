
using Prism.Commands;
using LemonSubtitleStudio.Models;
using LemonSubtitleStudio.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace LemonSubtitleStudio.ViewModels
{
    public class SubtitleEditorViewModel : INotifyPropertyChanged
    {
        private readonly IFileService _fileService;
        private readonly ILoggingService _loggingService;

        public ObservableCollection<SubtitleItem> Subtitles { get; } = new ObservableCollection<SubtitleItem>();

        private string _currentFileName = string.Empty;
        public string CurrentFileName
        {
            get => _currentFileName;
            set { _currentFileName = value; OnPropertyChanged(); }
        }

        private SubtitleItem _selectedSubtitle;
        public SubtitleItem SelectedSubtitle
        {
            get => _selectedSubtitle;
            set { _selectedSubtitle = value; OnPropertyChanged(); }
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
            set { _editEndTime = value; OnPropertyChanged(); }
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
            set { _totalTime = value; OnPropertyChanged(); }
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
        public DelegateCommand AddSubtitleCommand { get; }
        public DelegateCommand DeleteSubtitleCommand { get; }

        public SubtitleEditorViewModel()
        {
            _fileService = new FileService();
            _loggingService = new LoggingService();

            OpenMediaCommand = new DelegateCommand(OpenMedia);
            OpenSubtitleCommand = new DelegateCommand(OpenSubtitle);
            SaveCommand = new DelegateCommand(Save);
            AddSubtitleCommand = new DelegateCommand(AddSubtitle);
            DeleteSubtitleCommand = new DelegateCommand(DeleteSubtitle);

            StatusMessage = "请打开视频/音频和字幕文件";
        }

        private void OpenMedia()
        {
            var files = _fileService.SelectFiles("媒体文件|*.mp4;*.mkv;*.avi;*.mov;*.mp3;*.wav");
            if (files.Any())
            {
                CurrentFileName = System.IO.Path.GetFileName(files[0]);
                StatusMessage = $"已加载: {CurrentFileName}";
            }
        }

        private void OpenSubtitle()
        {
            var files = _fileService.SelectFiles("字幕文件|*.srt;*.vtt;*.ass");
            if (files.Any())
            {
                LoadSubtitles(files[0]);
            }
        }

        private void LoadSubtitles(string path)
        {
            Subtitles.Clear();
            var lines = File.ReadAllLines(path);
            for (int i = 0; i < lines.Length; i++)
            {
                if (int.TryParse(lines[i], out int index))
                {
                    if (i + 2 < lines.Length)
                    {
                        var timeLine = lines[i + 1];
                        var text = lines[i + 2];
                        Subtitles.Add(new SubtitleItem
                        {
                            Index = index,
                            OriginalText = text
                        });
                    }
                }
            }
            StatusMessage = $"已加载 {Subtitles.Count} 条字幕";
        }

        private void Save()
        {
            StatusMessage = "字幕已保存";
        }

        private void AddSubtitle()
        {
            Subtitles.Add(new SubtitleItem
            {
                Index = Subtitles.Count + 1,
                StartTime = TimeSpan.Zero,
                EndTime = TimeSpan.FromSeconds(2),
                OriginalText = "新字幕"
            });
        }

        private void DeleteSubtitle()
        {
            if (SelectedSubtitle != null)
            {
                Subtitles.Remove(SelectedSubtitle);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
