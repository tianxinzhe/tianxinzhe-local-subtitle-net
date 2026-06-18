using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LemonSubtitleStudio.Models;

namespace LemonSubtitleStudio.Services
{
    public interface ITranslationService
    {
        Task<string> TranslateTextAsync(string content, string sourceLang, string targetLang, string modelName);
        Task<List<SubtitleItem>> TranslateSubtitlesAsync(List<SubtitleItem> subtitles, string sourceLang, string targetLang, string modelName, IProgress<int> progress);
        Task<bool> IsModelAvailable(string modelName);
        Task<List<string>> GetAvailableModels();
        Task<string> TranslateTextWithWebAsync(string content, string sourceLang, string targetLang);
        Task<List<SubtitleItem>> TranslateSubtitlesWithWebAsync(List<SubtitleItem> subtitles, string sourceLang, string targetLang, IProgress<int> progress);
    }
}