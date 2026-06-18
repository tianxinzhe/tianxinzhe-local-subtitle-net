using Prism.Commands;
using LemonSubtitleStudio.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;

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
        public List<string> TranslationEngines { get; } = new List<string> { "Auto", "ONNX (Local)", "Web API" };

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

        private string _huggingFaceBaseUrl = "https://huggingface.co";
        public string HuggingFaceBaseUrl
        {
            get => _huggingFaceBaseUrl;
            set { _huggingFaceBaseUrl = value; OnPropertyChanged(); }
        }

        private string _translationEngine = "Auto";
        public string TranslationEngine
        {
            get => _translationEngine;
            set { _translationEngine = value; OnPropertyChanged(); }
        }

        private ModelInfo? _selectedModelInfo;
        public ModelInfo? SelectedModelInfo
        {
            get => _selectedModelInfo;
            set { _selectedModelInfo = value; OnPropertyChanged(); }
        }

        public ObservableCollection<NamingConventionInfo> NamingConventions { get; } = new ObservableCollection<NamingConventionInfo>
        {
            new() { Name = "Original", Description = "Keep the original filename", Example = "video.srt", IsSelected = true },
            new() { Name = "Language Suffix", Description = "Append language code to filename", Example = "video.zh.srt", IsSelected = false },
            new() { Name = "Custom Pattern", Description = "Use a custom naming pattern", Example = "video_translated.srt", IsSelected = false },
        };

        public DelegateCommand BrowseModelPathCommand { get; }
        public DelegateCommand BrowseOutputPathCommand { get; }
        public DelegateCommand SaveCommand { get; }
        public DelegateCommand<ModelInfo> DownloadModelCommand { get; }
        public DelegateCommand<ModelInfo> DeleteModelCommand { get; }
        public DelegateCommand<ModelInfo> SetDefaultModelCommand { get; }
        public DelegateCommand<string> SwitchModelCategoryCommand { get; }
        public DelegateCommand RestoreDefaultsCommand { get; }

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
            DownloadModelCommand = new DelegateCommand<ModelInfo>(async (model) => await DownloadModel(model));
            DeleteModelCommand = new DelegateCommand<ModelInfo>(DeleteModel);
            SetDefaultModelCommand = new DelegateCommand<ModelInfo>(SetDefaultModel);
            SwitchModelCategoryCommand = new DelegateCommand<string>(SwitchModelCategory);
            RestoreDefaultsCommand = new DelegateCommand(RestoreDefaults);
        }

        private void LoadSettings()
        {
            ModelStoragePath = _settingsService.ModelStoragePath;
            DefaultOutputDirectory = _settingsService.DefaultOutputDirectory;
            SelectedModel = _settingsService.DefaultModel;
            SelectedLanguage = _settingsService.DefaultLanguage;
            UseGPU = _settingsService.UseGPU;
            HuggingFaceBaseUrl = _settingsService.HuggingFaceBaseUrl;
            TranslationEngine = _settingsService.TranslationEngine;
        }

        private void LoadAvailableModels()
        {
            AvailableModels.Clear();
            if (_modelCategory == "Whisper 模型")
            {
                var modelDescs = new Dictionary<string, string>
                {
                    ["tiny"] = "Fastest, least accurate",
                    ["base"] = "Fast, moderate accuracy",
                    ["small"] = "Balanced speed and accuracy",
                    ["medium"] = "Accurate, needs more resources"
                };
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
                        Size = exists ? GetFileSize(path) : "未安装",
                        Description = modelDescs.TryGetValue(modelName, out var d) ? d : string.Empty
                    });
                }
            }
            else
            {
                var modelDescs = new Dictionary<string, string>
                {
                    ["marianmt-zh-en"] = "Chinese to English",
                    ["marianmt-en-zh"] = "English to Chinese",
                    ["nllb-200-distilled-600M"] = "200-language NLLB model",
                    ["m2m100-418M"] = "100-language M2M model"
                };
                var translationModels = new List<string> { "marianmt-zh-en", "marianmt-en-zh", "nllb-200-distilled-600M", "m2m100-418M" };
                foreach (var modelName in translationModels)
                {
                    var path = _modelManagerService.GetTranslationModelPath(modelName);
                    var exists = Directory.Exists(path);
                    var isDefault = modelName == _settingsService.DefaultTranslationModel;

                    AvailableModels.Add(new ModelInfo
                    {
                        Name = modelName,
                        Category = "Translation",
                        IsInstalled = exists,
                        IsDefault = isDefault,
                        Size = exists ? GetDirectorySize(path) : "未安装",
                        Description = modelDescs.TryGetValue(modelName, out var d) ? d : string.Empty
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

        private string GetDirectorySize(string path)
        {
            try
            {
                var size = Directory.GetFiles(path, "*", SearchOption.AllDirectories).Sum(f => new FileInfo(f).Length);
                if (size >= 1024 * 1024 * 1024)
                    return $"{size / (1024 * 1024 * 1024):F2} GB";
                if (size >= 1024 * 1024)
                    return $"{size / (1024 * 1024):F2} MB";
                return $"{size / 1024:F2} KB";
            }
            catch (Exception ex)
            {
                _loggingService.LogError("获取目录大小失败", ex);
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

        private async System.Threading.Tasks.Task DownloadModel(ModelInfo model)
        {
            if (model == null || model.IsInstalled || model.IsDownloading) return;

            model.ErrorMessage = string.Empty;
            model.Progress = 0;
            model.IsDownloading = true;

            try
            {
                var progressHandler = new Progress<int>(progress =>
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        model.Progress = progress;
                    });
                });
                if (model.Category == "Whisper")
                    await _modelManagerService.DownloadModelAsync(model.Name, progressHandler);
                else
                    await _modelManagerService.DownloadTranslationModelAsync(model.Name, progressHandler);
                LoadAvailableModels();
            }
            catch (Exception ex)
            {
                model.ErrorMessage = ex.Message;
                _loggingService.LogError("下载模型失败: " + model.Name, ex);
            }
            finally
            {
                model.IsDownloading = false;
            }
        }

        private void DeleteModel(ModelInfo model)
        {
            if (model == null || !model.IsInstalled) return;

            var path = model.Category == "Whisper"
                ? _modelManagerService.GetModelPath(model.Name)
                : _modelManagerService.GetTranslationModelPath(model.Name);

            if (model.Category == "Whisper")
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                    LoadAvailableModels();
                }
            }
            else
            {
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                    LoadAvailableModels();
                }
            }
        }

        private void SetDefaultModel(ModelInfo model)
        {
            if (model == null) return;

            if (model.Category == "Whisper")
                SelectedModel = model.Name;
            else
                _settingsService.DefaultTranslationModel = model.Name;
            LoadAvailableModels();
        }

        private void SwitchModelCategory(string category)
        {
            if (category == "Speech") ModelCategory = "Whisper 模型";
            else ModelCategory = "翻译模型";
        }

        private void RestoreDefaults()
        {
            _settingsService.RestoreDefaults();
            LoadSettings();
            LoadAvailableModels();
            HuggingFaceBaseUrl = "https://huggingface.co";
        }

        private void Save()
        {
            _settingsService.ModelStoragePath = ModelStoragePath;
            _settingsService.DefaultOutputDirectory = DefaultOutputDirectory;
            _settingsService.DefaultModel = SelectedModel;
            _settingsService.DefaultLanguage = SelectedLanguage;
            _settingsService.UseGPU = UseGPU;
            _settingsService.HuggingFaceBaseUrl = HuggingFaceBaseUrl;
            _settingsService.TranslationEngine = TranslationEngine;
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
        public string Description { get; set; } = string.Empty;

        private bool _isDownloading;
        public bool IsDownloading
        {
            get => _isDownloading;
            set
            {
                if (_isDownloading == value) return;
                _isDownloading = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanDownload));
                OnPropertyChanged(nameof(StatusLabel));
                OnPropertyChanged(nameof(StatusColor));
            }
        }

        private int _progress;
        public int Progress
        {
            get => _progress;
            set
            {
                _progress = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ProgressText));
                OnPropertyChanged(nameof(StatusLabel));
                OnPropertyChanged(nameof(StatusColor));
            }
        }

        private string _errorMessage = string.Empty;
        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                _errorMessage = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasError));
                OnPropertyChanged(nameof(StatusLabel));
                OnPropertyChanged(nameof(StatusColor));
            }
        }

        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
        public bool CanDownload => !IsInstalled && !IsDownloading;
        public string ProgressText => IsInstalled ? "100%" : (Progress > 0 ? $"{Progress}%" : "Not Downloaded");

        public string StatusLabel
        {
            get
            {
                if (HasError) return "Failed";
                if (IsDefault) return "Default";
                if (IsInstalled) return "Installed";
                if (IsDownloading) return $"{Progress}%";
                return "Not Installed";
            }
        }

        public string StatusColor
        {
            get
            {
                if (HasError) return "#FF4D4D";
                if (IsDefault) return "#FFB95C";
                if (IsInstalled) return "#4ADE80";
                if (IsDownloading) return "#FFA500";
                return "#606070";
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        public void RaiseProgressChanged() => OnPropertyChanged(nameof(ProgressText));
        protected void OnPropertyChanged(string? propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public class NamingConventionInfo : INotifyPropertyChanged
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Example { get; set; } = string.Empty;

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected == value) return;
                _isSelected = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string? propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}