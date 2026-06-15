
using System.IO;
using Whisper.net;
using Whisper.net.Ggml;

namespace LemonSubtitleStudio.Services
{
    public class ModelManagerService : IModelManagerService
    {
        private readonly ISettingsService _settingsService;
        private readonly List<string> _availableModels = new List<string> { "tiny", "base", "small", "medium", "large" };

        public ModelManagerService(ISettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        public List<string> GetAvailableModels() => _availableModels;

        public bool IsModelDownloaded(string modelName)
        {
            var modelPath = GetModelPath(modelName);
            return File.Exists(modelPath);
        }

        public string GetModelPath(string modelName)
        {
            return Path.Combine(_settingsService.ModelStoragePath, $"{modelName}.bin");
        }

        public async Task DownloadModelAsync(string modelName, IProgress<int> progress)
        {
            var modelPath = GetModelPath(modelName);
            var directory = Path.GetDirectoryName(modelPath);
            
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory!);

            using var modelStream = await WhisperGgmlDownloader.GetGgmlModelAsync(GetGgmlType(modelName));
            using var fileStream = File.Create(modelPath);
            
            var buffer = new byte[8192];
            int bytesRead;
            long totalBytes = modelStream.Length;
            long downloadedBytes = 0;

            while ((bytesRead = await modelStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, bytesRead);
                downloadedBytes += bytesRead;
                progress?.Report((int)((downloadedBytes * 100) / totalBytes));
            }
        }

        private GgmlType GetGgmlType(string modelName)
        {
            return modelName switch
            {
                "tiny" => GgmlType.Tiny,
                "base" => GgmlType.Base,
                "small" => GgmlType.Small,
                "medium" => GgmlType.Medium,
                "large" => GgmlType.Large,
                _ => GgmlType.Base
            };
        }

        public Task InitializeAsync()
        {
            var modelPath = _settingsService.ModelStoragePath;
            if (!Directory.Exists(modelPath))
                Directory.CreateDirectory(modelPath);
            
            return Task.CompletedTask;
        }
    }
}
