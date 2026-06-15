
using Prism.Commands;
using LemonSubtitleStudio.Services;
using System.ComponentModel;

namespace LemonSubtitleStudio.ViewModels
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        private readonly ISettingsService _settingsService;
        private readonly IFileService _fileService;

        public List<string> Models { get; } = new List<string> { "tiny", "base", "small", "medium", "large" };
        public List<string> Languages { get; } = new List<string> { "中文", "English", "日本語", "한국어" };

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

        public DelegateCommand BrowseModelPathCommand { get; }
        public DelegateCommand BrowseOutputPathCommand { get; }
        public DelegateCommand SaveCommand { get; }

        public SettingsViewModel()
        {
            _settingsService = new SettingsService();
            _fileService = new FileService();

            LoadSettings();

            BrowseModelPathCommand = new DelegateCommand(BrowseModelPath);
            BrowseOutputPathCommand = new DelegateCommand(BrowseOutputPath);
            SaveCommand = new DelegateCommand(Save);
        }

        private void LoadSettings()
        {
            ModelStoragePath = _settingsService.ModelStoragePath;
            DefaultOutputDirectory = _settingsService.DefaultOutputDirectory;
            SelectedModel = _settingsService.DefaultModel;
            SelectedLanguage = _settingsService.DefaultLanguage;
            UseGPU = _settingsService.UseGPU;
        }

        private void BrowseModelPath()
        {
            var folder = _fileService.SelectFolder("选择模型存储目录");
            if (!string.IsNullOrEmpty(folder))
                ModelStoragePath = folder;
        }

        private void BrowseOutputPath()
        {
            var folder = _fileService.SelectFolder("选择默认输出目录");
            if (!string.IsNullOrEmpty(folder))
                DefaultOutputDirectory = folder;
        }

        private void Save()
        {
            _settingsService.ModelStoragePath = ModelStoragePath;
            _settingsService.DefaultOutputDirectory = DefaultOutputDirectory;
            _settingsService.DefaultModel = SelectedModel;
            _settingsService.DefaultLanguage = SelectedLanguage;
            _settingsService.UseGPU = UseGPU;
            _settingsService.Save();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
