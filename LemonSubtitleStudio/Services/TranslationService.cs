using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LemonSubtitleStudio.Models;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace LemonSubtitleStudio.Services
{
    public class TranslationService : ITranslationService, IDisposable
    {
        private readonly ISettingsService _settingsService;
        private readonly ILoggingService _loggingService;
        private readonly List<string> _availableModels = new List<string> { "marianmt-zh-en", "marianmt-en-zh" };
        private InferenceSession? _session;
        private bool _disposed = false;

        public TranslationService(ISettingsService settingsService, ILoggingService loggingService)
        {
            _settingsService = settingsService;
            _loggingService = loggingService;
        }

        public Task<List<string>> GetAvailableModels()
        {
            return Task.FromResult(_availableModels);
        }

        public Task<bool> IsModelAvailable(string modelName)
        {
            var modelPath = GetModelPath(modelName);
            return Task.FromResult(File.Exists(modelPath));
        }

        private string GetModelPath(string modelName)
        {
            return Path.Combine(_settingsService.ModelStoragePath, "onnx", $"{modelName}.onnx");
        }

        public async Task<string> TranslateTextAsync(string content, string sourceLang, string targetLang, string modelName)
        {
            if (!IsModelAvailable(modelName).Result)
            {
                return content;
            }

            var modelPath = GetModelPath(modelName);
            
            try
            {
                if (_session == null)
                {
                    _session = new InferenceSession(modelPath);
                }

                var inputTensor = new DenseTensor<string>(new[] { content }, new[] { 1, 1 });
                var inputs = new List<NamedOnnxValue>
                {
                    NamedOnnxValue.CreateFromTensor("input", inputTensor)
                };

                using var results = _session.Run(inputs);
                var output = results.FirstOrDefault(r => r.Name == "output");
                if (output != null)
                {
                    return output.AsEnumerable<string>().FirstOrDefault() ?? content;
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError("翻译失败", ex);
            }

            return content;
        }

        public async Task<List<SubtitleItem>> TranslateSubtitlesAsync(List<SubtitleItem> subtitles, string sourceLang, string targetLang, string modelName, IProgress<int> progress)
        {
            var translated = new List<SubtitleItem>();
            
            for (int i = 0; i < subtitles.Count; i++)
            {
                var subtitle = subtitles[i];
                var translatedText = await TranslateTextAsync(subtitle.OriginalText, sourceLang, targetLang, modelName);
                
                translated.Add(new SubtitleItem
                {
                    Index = subtitle.Index,
                    StartTime = subtitle.StartTime,
                    EndTime = subtitle.EndTime,
                    OriginalText = subtitle.OriginalText,
                    TranslatedText = translatedText
                });

                progress?.Report((int)((i + 1) * 100.0 / subtitles.Count));
            }

            return translated;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;
            
            if (disposing)
            {
                _session?.Dispose();
            }
            
            _disposed = true;
        }

        ~TranslationService()
        {
            Dispose(false);
        }
    }
}