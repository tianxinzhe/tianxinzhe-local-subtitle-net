using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LemonSubtitleStudio.Models;
using Whisper.net;

namespace LemonSubtitleStudio.Services
{
    public class TranscriptionService : ITranscriptionService
    {
        private readonly IModelManagerService _modelManagerService;
        private readonly List<string> _availableModels = new List<string> { "tiny", "base", "small", "medium" };

        public TranscriptionService(IModelManagerService modelManagerService)
        {
            _modelManagerService = modelManagerService;
        }

        public Task<List<string>> GetAvailableModels()
        {
            return Task.FromResult(_availableModels);
        }

        public Task<bool> IsModelAvailable(string modelName)
        {
            var path = _modelManagerService.GetModelPath(modelName);
            return Task.FromResult(File.Exists(path));
        }

        public Task InitializeModelAsync(string modelName) => Task.CompletedTask;

        public async Task<List<SubtitleItem>> TranscribeAsync(string audioPath, string language, string modelName, IProgress<int> progress)
        {
            var subtitles = new List<SubtitleItem>();
            
            if (!File.Exists(audioPath))
            {
                throw new FileNotFoundException($"Audio file not found: {audioPath}");
            }

            if (!_availableModels.Contains(modelName))
            {
                throw new ArgumentException($"Unsupported model: {modelName}");
            }

            var modelPath = _modelManagerService.GetModelPath(modelName);
            
            if (!File.Exists(modelPath))
            {
                throw new FileNotFoundException($"Model file not found: {modelPath}");
            }

            var whisperLanguage = language switch
            {
                "中文" => "zh",
                "Chinese" => "zh",
                "English" => "en",
                "日本語" => "ja",
                "한국어" => "ko",
                _ => null
            };

            using var whisperFactory = WhisperFactory.FromPath(modelPath);
            using var processor = whisperFactory.CreateBuilder()
                .WithLanguage(whisperLanguage)
                .Build();
            
            using var audioStream = File.OpenRead(audioPath);
            var result = processor.ProcessAsync(audioStream);

            int index = 1;
            await foreach (var segment in result)
            {
                subtitles.Add(new SubtitleItem
                {
                    Index = index++,
                    StartTime = segment.Start,
                    EndTime = segment.End,
                    OriginalText = segment.Text
                });
            }

            return subtitles;
        }
    }
}