using Prism.Commands;
using LemonSubtitleStudio.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;

namespace LemonSubtitleStudio.ViewModels
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        private readonly ISettingsService _settingsService;
        private readonly IFileService _fileService;
        private readonly IModelManagerService _modelManagerService;
        private readonly ILoggingService _loggingService;

        public List<string> Models { get; } = new List<string> { "tiny", "base", "small", "medium" };
        public List<string> Languages { get; } = new List<string> { "中文", "English", "日本語", "한국어" };

        public ObservableCollection<ModelInfo> AvailableModels { get; } = new ObservableCollection<ModelInfo>();

        public List<string> ModelCategories { get; } = new List<string> { "Whisper 模型", "翻译模型" };

        private string _modelCategory = "Whisper 模型";
        public string ModelCategory
        {
            get => _modelCategory;
            set
            {
                if (_modelCategory == value) return;
                _modelCategory = value;
                OnPropertyChanged();
                LoadAvailableModels();
            }
        }

        private readonly Dictionary<string, int> _progressMap = new Dictionary<string, int>();

        public int GetModelProgress(ModelInfo model) => _progressMap.TryGetValue(model.Name, out var p) ? p : 0;

        public bool IsModelDownloading(ModelInfo model) => _progressMap.ContainsKey(model.Name);

        private string _modelStoragePath = string.Empty;
        public string ModelStoragePath
        {
            get => _modelStoragePath;
            set { _modelStoragePath = value; OnPropertyChanged(); }
        }

        private string _defaultOutputDirectory = string.Empty;
        public string DefaultOutputDirectory
        {
            get => _defaultOutputDirectory;
            set { _defaultOutputDirectory = value; OnPropertyChanged(); }
        }

        private string _selectedModel = "base";
        public string SelectedModel
        {
            get => _selectedModel;
            set { _selectedModel = value; OnPropertyChanged(); }
        }

        private string _selectedLanguage = "中文";
        public string SelectedLanguage
        {
            get => _selectedLanguage;
            set { _selectedLanguage = value; OnPropertyChanged(); }
        }

        private bool _useGPU = true;
        public bool UseGPU
        {
            get => _useGPU;
            set { _useGPU = value; OnPropertyChanged(); }
        }

        private ModelInfo? _selectedModelInfo;
        public ModelInfo? SelectedModelInfo
        {
            get => _selectedModelInfo;
            set { _selectedModelInfo = value; OnPropertyChanged(); }
        }

        private bool _isDownloading;
        public bool IsDownloading
        {
            get => _isDownloading;
            set { _isDownloading = value; OnPropertyChanged(); }
        }

        private int _downloadProgress;
        public int DownloadProgress
        {
            get => _downloadProgress;
            set { _downloadProgress = value; OnPropertyChanged(); }
        }

        public DelegateCommand BrowseModelPathCommand { get; }
        public DelegateCommand BrowseOutputPathCommand { get; }
        public DelegateCommand SaveCommand { get; }
        public DelegateCommand DownloadModelCommand { get; }
        public DelegateCommand DeleteModelCommand { get; }
        public DelegateCommand SetDefaultModelCommand { get; }

        public SettingsViewModel(ISettingsService settingsService, IFileService fileService, IModelManagerService modelManagerService, ILoggingService loggingService)
        {
            _settingsService = settingsService;
            _fileService = fileService;
            _modelManagerService = modelManagerService;
            _loggingService = loggingService;

            LoadSettings();
            LoadAvailableModels();

            BrowseModelPathCommand = new DelegateCommand(BrowseModelPath);
            BrowseOutputPathCommand = new DelegateCommand(BrowseOutputPath);
            SaveCommand = new DelegateCommand(Save);
            DownloadModelCommand = new DelegateCommand(async () => await DownloadModel());
            DeleteModelCommand = new DelegateCommand(DeleteModel);
            SetDefaultModelCommand = new DelegateCommand(SetDefaultModel);
        }

        private void LoadSettings()
        {
            ModelStoragePath = _settingsService.ModelStoragePath;
            DefaultOutputDirectory = _settingsService.DefaultOutputDirectory;
            SelectedModel = _settingsService.DefaultModel;
            SelectedLanguage = _settingsService.DefaultLanguage;
            UseGPU = _settingsService.UseGPU;
        }

        private void LoadAvailableModels()
        {
            AvailableModels.Clear();
            if (_modelCategory == "Whisper 模型")
            {
                foreach (var modelName in Models)
                {
                    var path = _modelManagerService.GetModelPath(modelName);
                    var exists = File.Exists(path);
                    var isDefault = modelName == _settingsService.DefaultModel;

                    AvailableModels.Add(new ModelInfo
                    {
                        Name = modelName,
                        Category = "Whisper",
                        IsInstalled = exists,
                        IsDefault = isDefault,
                        Size = exists ? GetFileSize(path) : "未安装"
                    });
                }
            }
            else
            {
                var translationModels = new List<string> { "marianmt-zh-en", "marianmt-en-zh" };
                foreach (var modelName in translationModels)
                {
                    var path = _modelManagerService.GetTranslationModelPath(modelName);
                    var exists = File.Exists(path);
                    var isDefault = modelName == _settingsService.DefaultTranslationModel;

                    AvailableModels.Add(new ModelInfo
                    {
                        Name = modelName,
                        Category = "Translation",
                        IsInstalled = exists,
                        IsDefault = isDefault,
                        Size = exists ? GetFileSize(path) : "未安装"
                    });
                }
            }
        }

        private string GetFileSize(string path)
        {
            try
            {
                var size = new FileInfo(path).Length;
                if (size >= 1024 * 1024 * 1024)
                    return $"{size / (1024 * 1024 * 1024):F2} GB";
                if (size >= 1024 * 1024)
                    return $"{size / (1024 * 1024):F2} MB";
                return $"{size / 1024:F2} KB";
            }
            catch (Exception ex)
            {
                _loggingService.LogError("获取文件大小失败", ex);
                return "未知";
            }
        }

        private void BrowseModelPath()
        {
            var folder = _fileService.SelectFolder("选择模型存储目录");
            if (!string.IsNullOrEmpty(folder))
            {
                ModelStoragePath = folder;
                _modelManagerService.SetModelPath(folder);
                LoadAvailableModels();
            }
        }

        private void BrowseOutputPath()
        {
            var folder = _fileService.SelectFolder("选择默认输出目录");
            if (!string.IsNullOrEmpty(folder))
                DefaultOutputDirectory = folder;
        }

        private async System.Threading.Tasks.Task DownloadModel()
        {
            if (SelectedModelInfo == null || SelectedModelInfo.IsInstalled) return;
            if (_progressMap.ContainsKey(SelectedModelInfo.Name)) return;

            IsDownloading = true;
            _progressMap[SelectedModelInfo.Name] = 0;
            SelectedModelInfo.RaiseProgressChanged();

            try
            {
                var progressHandler = new Progress<int>(progress =>
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        _progressMap[SelectedModelInfo.Name] = progress;
                        SelectedModelInfo.Progress = progress;
                    });
                });
                if (SelectedModelInfo.Category == "Whisper")
                    await _modelManagerService.DownloadModelAsync(SelectedModelInfo.Name, progressHandler);
                else
                    await _modelManagerService.DownloadTranslationModelAsync(SelectedModelInfo.Name, progressHandler);
                LoadAvailableModels();
            }
            catch (Exception ex)
            {
                _loggingService.LogError("下载模型失败: " + SelectedModelInfo.Name, ex);
            }
            finally
            {
                _progressMap.Remove(SelectedModelInfo.Name);
                IsDownloading = false;
                DownloadProgress = 0;
                if (SelectedModelInfo != null) SelectedModelInfo.Progress = 0;
            }
        }

        private void DeleteModel()
        {
            if (SelectedModelInfo == null || !SelectedModelInfo.IsInstalled) return;

            var path = SelectedModelInfo.Category == "Whisper"
                ? _modelManagerService.GetModelPath(SelectedModelInfo.Name)
                : _modelManagerService.GetTranslationModelPath(SelectedModelInfo.Name);
            if (File.Exists(path))
            {
                File.Delete(path);
                LoadAvailableModels();
            }
        }

        private void SetDefaultModel()
        {
            if (SelectedModelInfo == null) return;

            if (SelectedModelInfo.Category == "Whisper")
                SelectedModel = SelectedModelInfo.Name;
            else
                _settingsService.DefaultTranslationModel = SelectedModelInfo.Name;
            LoadAvailableModels();
        }

        private void Save()
        {
            _settingsService.ModelStoragePath = ModelStoragePath;
            _settingsService.DefaultOutputDirectory = DefaultOutputDirectory;
            _settingsService.DefaultModel = SelectedModel;
            _settingsService.DefaultLanguage = SelectedLanguage;
            _settingsService.UseGPU = UseGPU;
            _settingsService.Save();
            
            LoadAvailableModels();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public class ModelInfo : INotifyPropertyChanged
    {
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = "Whisper";
        public bool IsInstalled { get; set; }
        public bool IsDefault { get; set; }
        public string Size { get; set; } = string.Empty;

        private int _progress;
        public int Progress
        {
            get => _progress;
            set { _progress = value; OnPropertyChanged(nameof(Progress)); OnPropertyChanged(nameof(ProgressText)); }
        }
        public string ProgressText => IsInstalled ? "100%" : (Progress > 0 ? $"{Progress}%" : "未下载");

        public event PropertyChangedEventHandler? PropertyChanged;
        public void RaiseProgressChanged() => OnPropertyChanged(nameof(ProgressText));
        protected void OnPropertyChanged(string? propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}