
namespace LemonSubtitleStudio.Services
{
    public interface IModelManagerService
    {
        Task DownloadModelAsync(string modelName, IProgress<int> progress);
        bool IsModelDownloaded(string modelName);
        string GetModelPath(string modelName);
        List<string> GetAvailableModels();
        Task InitializeAsync();
    }
}
