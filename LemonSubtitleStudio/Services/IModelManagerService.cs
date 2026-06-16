
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LemonSubtitleStudio.Services
{
    public interface IModelManagerService
    {
        Task DownloadModelAsync(string modelName, IProgress<int> progress);
        Task DownloadTranslationModelAsync(string modelName, IProgress<int> progress);
        bool IsModelDownloaded(string modelName);
        string GetModelPath(string modelName);
        string GetTranslationModelPath(string modelName);
        List<string> GetAvailableModels();
        Task InitializeAsync();
        void SetModelPath(string path);
    }
}
