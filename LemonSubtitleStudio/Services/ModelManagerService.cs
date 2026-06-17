using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Whisper.net;

namespace LemonSubtitleStudio.Services
{
    public class ModelManagerService : IModelManagerService
    {
        private readonly ISettingsService _settingsService;
        private readonly List<string> _availableModels = new List<string> { "tiny", "base", "small", "medium" };
        private readonly HttpClient _httpClient;

        public ModelManagerService(ISettingsService settingsService)
        {
            _settingsService = settingsService;
            _httpClient = new HttpClient();
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

        public void SetModelPath(string path)
        {
            _settingsService.ModelStoragePath = path;
        }

        public async Task DownloadModelAsync(string modelName, IProgress<int> progress)
        {
            var modelPath = GetModelPath(modelName);
            var directory = Path.GetDirectoryName(modelPath);

            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory!);

            var url = $"https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-{modelName}.bin";
            await DownloadFileAsync(url, modelPath, progress);
        }

        public string GetTranslationModelPath(string modelName)
        {
            return Path.Combine(_settingsService.ModelStoragePath, "onnx", $"{modelName}.onnx");
        }

        public async Task DownloadTranslationModelAsync(string modelName, IProgress<int> progress)
        {
            var modelPath = GetTranslationModelPath(modelName);
            var directory = Path.GetDirectoryName(modelPath);

            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory!);

            var url = GetTranslationModelDownloadUrl(modelName);
            var tempPath = modelPath + ".tmp";
            try
            {
                await DownloadFileAsync(url, tempPath, progress);
                if (File.Exists(modelPath)) File.Delete(modelPath);
                File.Move(tempPath, modelPath);
            }
            catch
            {
                if (File.Exists(tempPath)) File.Delete(tempPath);
                throw;
            }
        }

        private static string GetTranslationModelDownloadUrl(string modelName)
        {
            return modelName switch
            {
                "nllb-200-distilled-600M" => "https://huggingface.co/facebook/nllb-200-distilled-600M/resolve/main/model.onnx",
                "m2m100-418M" => "https://huggingface.co/facebook/m2m100-418M/resolve/main/model.onnx",
                "translate-gemma-4b" => "https://huggingface.co/google/translate-gemma-4b/resolve/main/model.onnx",
                _ when modelName.StartsWith("marianmt-") =>
                    $"https://huggingface.co/Helsinki-NLP/opus-mt-{modelName.Replace("marianmt-", "")}/resolve/main/onnx/model.onnx",
                _ => throw new ArgumentException($"Unknown translation model: {modelName}")
            };
        }

        private async Task DownloadFileAsync(string url, string destPath, IProgress<int> progress)
        {
            if (File.Exists(destPath)) File.Delete(destPath);

            using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            await using var contentStream = await response.Content.ReadAsStreamAsync();
            await using var fileStream = File.Create(destPath);

            var buffer = new byte[81920];
            int bytesRead;
            long totalBytes = response.Content.Headers.ContentLength ?? -1;
            long downloadedBytes = 0;
            var lastReport = DateTime.UtcNow;

            while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, bytesRead);
                downloadedBytes += bytesRead;
                if (totalBytes > 0)
                {
                    var now = DateTime.UtcNow;
                    if ((now - lastReport).TotalMilliseconds > 100 || downloadedBytes == totalBytes)
                    {
                        lastReport = now;
                        progress?.Report((int)((downloadedBytes * 100) / totalBytes));
                    }
                }
            }
            if (totalBytes < 0) progress?.Report(100);
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
