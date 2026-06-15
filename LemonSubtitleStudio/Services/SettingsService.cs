
using System.IO;
using System.Xml.Serialization;

namespace LemonSubtitleStudio.Services
{
    public class SettingsService : ISettingsService
    {
        private const string SettingsFile = "settings.xml";
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

        public SettingsService()
        {
            _settings = new SettingsData();
            Load();
        }

        public void Save()
        {
            var serializer = new XmlSerializer(typeof(SettingsData));
            using var writer = new StreamWriter(SettingsFile);
            serializer.Serialize(writer, _settings);
        }

        public void Load()
        {
            if (File.Exists(SettingsFile))
            {
                var serializer = new XmlSerializer(typeof(SettingsData));
                using var reader = new StreamReader(SettingsFile);
                _settings = (SettingsData)serializer.Deserialize(reader);
            }
            else
            {
                _settings = new SettingsData
                {
                    ModelStoragePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LemonSubtitleStudio", "Models"),
                    DefaultOutputDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    DefaultModel = "base",
                    DefaultLanguage = "zh",
                    UseGPU = true
                };
            }
        }
    }

    public class SettingsData
    {
        public string ModelStoragePath { get; set; } = string.Empty;
        public string DefaultOutputDirectory { get; set; } = string.Empty;
        public string DefaultModel { get; set; } = string.Empty;
        public string DefaultLanguage { get; set; } = string.Empty;
        public bool UseGPU { get; set; } = true;
    }
}
