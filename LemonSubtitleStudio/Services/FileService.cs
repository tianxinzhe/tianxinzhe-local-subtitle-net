
using Microsoft.Win32;
using System;
using System.IO;

namespace LemonSubtitleStudio.Services
{
    public class FileService : IFileService
    {
        public string SelectFolder(string title = "选择文件夹")
        {
            var dialog = new OpenFolderDialog { Title = title };
            return dialog.ShowDialog() == true ? dialog.FolderName : string.Empty;
        }

        public string[] SelectFiles(string filter, string title = "选择文件")
        {
            var dialog = new OpenFileDialog 
            { 
                Title = title, 
                Filter = filter,
                Multiselect = true
            };
            return dialog.ShowDialog() == true ? dialog.FileNames : Array.Empty<string>();
        }

        public bool FileExists(string path) => File.Exists(path);

        public void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }

        public string GetFileExtension(string path) => Path.GetExtension(path);

        public string GetFileNameWithoutExtension(string path) => Path.GetFileNameWithoutExtension(path);

        public string GenerateOutputPath(string inputPath, string outputDir, string extension)
        {
            var fileName = GetFileNameWithoutExtension(inputPath);
            var outputPath = Path.Combine(outputDir, $"{fileName}{extension}");
            
            if (!File.Exists(outputPath))
                return outputPath;

            int counter = 1;
            while (File.Exists(outputPath))
            {
                outputPath = Path.Combine(outputDir, $"{fileName}_{counter}{extension}");
                counter++;
            }
            return outputPath;
        }
    }
}
