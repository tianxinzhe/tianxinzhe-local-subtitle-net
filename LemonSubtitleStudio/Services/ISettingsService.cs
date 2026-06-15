
namespace LemonSubtitleStudio.Services
{
    public interface ISettingsService
    {
        string ModelStoragePath { get; set; }
        string DefaultOutputDirectory { get; set; }
        string DefaultModel { get; set; }
        string DefaultLanguage { get; set; }
        bool UseGPU { get; set; }
        void Save();
        void Load();
    }
}
