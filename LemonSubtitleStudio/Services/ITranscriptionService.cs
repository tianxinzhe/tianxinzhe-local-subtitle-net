using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LemonSubtitleStudio.Models;

namespace LemonSubtitleStudio.Services
{
    public interface ITranscriptionService
    {
        Task<List<SubtitleItem>> TranscribeAsync(string audioPath, string language, string modelName, IProgress<int> progress);
        Task<bool> IsModelAvailable(string modelName);
        Task<List<string>> GetAvailableModels();
        Task InitializeModelAsync(string modelName);
    }
}