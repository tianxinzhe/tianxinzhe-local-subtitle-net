
namespace LemonSubtitleStudio.Services
{
    public interface IFileService
    {
        string SelectFolder(string title = "选择文件夹");
        string[] SelectFiles(string filter, string title = "选择文件");
        bool FileExists(string path);
        void EnsureDirectoryExists(string path);
        string GetFileExtension(string path);
        string GetFileNameWithoutExtension(string path);
        string GenerateOutputPath(string inputPath, string outputDir, string extension);
    }
}
