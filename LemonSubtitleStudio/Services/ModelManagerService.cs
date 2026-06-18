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

            var url = $"{_settingsService.HuggingFaceBaseUrl}/ggerganov/whisper.cpp/resolve/main/ggml-{modelName}.bin";
            await DownloadFileAsync(url, modelPath, progress);
        }

        public string GetTranslationModelPath(string modelName)
        {
            return Path.Combine(_settingsService.ModelStoragePath, "onnx", modelName);
        }

        public async Task DownloadTranslationModelAsync(string modelName, IProgress<int> progress)
        {
            var modelDir = GetTranslationModelPath(modelName);

            if (!Directory.Exists(modelDir))
                Directory.CreateDirectory(modelDir);

            var urls = GetTranslationModelDownloadUrls(modelName);
            var tempDir = modelDir + ".tmp";

            try
            {
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
                Directory.CreateDirectory(tempDir);

                int totalFiles = urls.Count;
                int completedFiles = 0;

                foreach (var (fileName, url) in urls)
                {
                    var destPath = Path.Combine(tempDir, fileName);
                    var fileProgress = new Progress<int>(p =>
                    {
                        progress?.Report((completedFiles * 100 + p) / totalFiles);
                    });
                    await DownloadFileAsync(url, destPath, fileProgress);
                    completedFiles++;
                }

                if (Directory.Exists(modelDir)) Directory.Delete(modelDir, true);
                Directory.Move(tempDir, modelDir);
            }
            catch
            {
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
                throw;
            }
        }

        private List<(string FileName, string Url)> GetTranslationModelDownloadUrls(string modelName)
        {
            var baseUrl = _settingsService.HuggingFaceBaseUrl;
            var urls = new List<(string, string)>();

            if (modelName.StartsWith("marianmt-"))
            {
                var langPair = modelName.Replace("marianmt-", "");
                var repo = $"Xenova/opus-mt-{langPair}";
                urls.Add(("encoder_model.onnx", $"{baseUrl}/{repo}/resolve/main/onnx/encoder_model.onnx"));
                urls.Add(("decoder_model.onnx", $"{baseUrl}/{repo}/resolve/main/onnx/decoder_model.onnx"));
                urls.Add(("tokenizer.json", $"{baseUrl}/{repo}/resolve/main/tokenizer.json"));
                urls.Add(("vocab.json", $"{baseUrl}/{repo}/resolve/main/vocab.json"));
                urls.Add(("source.spm", $"{baseUrl}/{repo}/resolve/main/source.spm"));
                urls.Add(("target.spm", $"{baseUrl}/{repo}/resolve/main/target.spm"));
                urls.Add(("config.json", $"{baseUrl}/{repo}/resolve/main/config.json"));
                urls.Add(("generation_config.json", $"{baseUrl}/{repo}/resolve/main/generation_config.json"));
                urls.Add(("special_tokens_map.json", $"{baseUrl}/{repo}/resolve/main/special_tokens_map.json"));
                urls.Add(("tokenizer_config.json", $"{baseUrl}/{repo}/resolve/main/tokenizer_config.json"));
            }
            else if (modelName == "nllb-200-distilled-600M")
            {
                var repo = "lopatnov/nllb-200-distilled-600M-onnx";
                urls.Add(("encoder_model.onnx", $"{baseUrl}/{repo}/resolve/main/encoder_model.onnx"));
                urls.Add(("encoder_model.onnx.data", $"{baseUrl}/{repo}/resolve/main/encoder_model.onnx.data"));
                urls.Add(("decoder_model.onnx", $"{baseUrl}/{repo}/resolve/main/decoder_model.onnx"));
                urls.Add(("decoder_model.onnx.data", $"{baseUrl}/{repo}/resolve/main/decoder_model.onnx.data"));
                urls.Add(("tokenizer.json", $"{baseUrl}/{repo}/resolve/main/tokenizer.json"));
                urls.Add(("sentencepiece.bpe.model", $"{baseUrl}/{repo}/resolve/main/sentencepiece.bpe.model"));
                urls.Add(("config.json", $"{baseUrl}/{repo}/resolve/main/config.json"));
                urls.Add(("generation_config.json", $"{baseUrl}/{repo}/resolve/main/generation_config.json"));
                urls.Add(("special_tokens_map.json", $"{baseUrl}/{repo}/resolve/main/special_tokens_map.json"));
                urls.Add(("tokenizer_config.json", $"{baseUrl}/{repo}/resolve/main/tokenizer_config.json"));
            }
            else if (modelName == "m2m100-418M")
            {
                var repo = "lopatnov/m2m100_418M-onnx";
                urls.Add(("encoder_model.onnx", $"{baseUrl}/{repo}/resolve/main/encoder_model.onnx"));
                urls.Add(("decoder_model.onnx", $"{baseUrl}/{repo}/resolve/main/decoder_model.onnx"));
                urls.Add(("vocab.json", $"{baseUrl}/{repo}/resolve/main/vocab.json"));
                urls.Add(("sentencepiece.bpe.model", $"{baseUrl}/{repo}/resolve/main/sentencepiece.bpe.model"));
                urls.Add(("config.json", $"{baseUrl}/{repo}/resolve/main/config.json"));
                urls.Add(("generation_config.json", $"{baseUrl}/{repo}/resolve/main/generation_config.json"));
                urls.Add(("special_tokens_map.json", $"{baseUrl}/{repo}/resolve/main/special_tokens_map.json"));
                urls.Add(("tokenizer_config.json", $"{baseUrl}/{repo}/resolve/main/tokenizer_config.json"));
                urls.Add(("added_tokens.json", $"{baseUrl}/{repo}/resolve/main/added_tokens.json"));
            }
            else if (modelName == "translate-gemma-4b")
            {
                throw new ArgumentException("translate-gemma-4b is a gated model and requires HuggingFace authentication. Please use a different model.");
            }
            else
            {
                throw new ArgumentException($"Unknown translation model: {modelName}");
            }

            return urls;
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
