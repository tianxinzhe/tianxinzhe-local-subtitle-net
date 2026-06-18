using System;
using System.IO;
using System.Xml.Serialization;

namespace LemonSubtitleStudio.Services
{
    public class SettingsService : ISettingsService
    {
        private readonly string _settingsFile;
        private SettingsData _settings;

        public string ModelStoragePath
        {
            get => _settings.ModelStoragePath;
            set => _settings.ModelStoragePath = value;
        }

        public string DefaultOutputDirectory
        {
            get => _settings.DefaultOutputDirectory;
            set => _settings.DefaultOutputDirectory = value;
        }

        public string DefaultModel
        {
            get => _settings.DefaultModel;
            set => _settings.DefaultModel = value;
        }

        public string DefaultTranslationModel
        {
            get => _settings.DefaultTranslationModel;
            set => _settings.DefaultTranslationModel = value;
        }

        public string DefaultLanguage
        {
            get => _settings.DefaultLanguage;
            set => _settings.DefaultLanguage = value;
        }

        public bool UseGPU
        {
            get => _settings.UseGPU;
            set => _settings.UseGPU = value;
        }

        public string HuggingFaceBaseUrl
        {
            get => _settings.HuggingFaceBaseUrl;
            set => _settings.HuggingFaceBaseUrl = value;
        }

        public string TranslationEngine
        {
            get => _settings.TranslationEngine;
            set => _settings.TranslationEngine = value;
        }

        public SettingsService()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appPath = Path.Combine(appDataPath, "LemonSubtitleStudio");
            if (!Directory.Exists(appPath))
                Directory.CreateDirectory(appPath);
            _settingsFile = Path.Combine(appPath, "settings.xml");
            
            _settings = new SettingsData();
            Load();
        }

        public void Save()
        {
            var serializer = new XmlSerializer(typeof(SettingsData));
            using var writer = new StreamWriter(_settingsFile);
            serializer.Serialize(writer, _settings);
        }

        public void Load()
        {
            if (File.Exists(_settingsFile))
            {
                var serializer = new XmlSerializer(typeof(SettingsData));
                using var reader = new StreamReader(_settingsFile);
                _settings = (SettingsData)serializer.Deserialize(reader);
            }
            else
            {
                SetDefaults();
            }
        }

        public void RestoreDefaults()
        {
            SetDefaults();
            Save();
        }

        private void SetDefaults()
        {
            _settings = new SettingsData
            {
                ModelStoragePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LemonSubtitleStudio", "Models"),
                DefaultOutputDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                DefaultModel = "base",
                DefaultTranslationModel = "marianmt-zh-en",
                DefaultLanguage = "zh",
                UseGPU = true,
                HuggingFaceBaseUrl = "https://huggingface.co",
                TranslationEngine = "Auto"
            };
        }
    }

    public class SettingsData
    {
        public string ModelStoragePath { get; set; } = string.Empty;
        public string DefaultOutputDirectory { get; set; } = string.Empty;
        public string DefaultModel { get; set; } = string.Empty;
        public string DefaultTranslationModel { get; set; } = string.Empty;
        public string DefaultLanguage { get; set; } = string.Empty;
        public bool UseGPU { get; set; } = true;
        public string HuggingFaceBaseUrl { get; set; } = "https://huggingface.co";
        public string TranslationEngine { get; set; } = "Auto";
    }
}
