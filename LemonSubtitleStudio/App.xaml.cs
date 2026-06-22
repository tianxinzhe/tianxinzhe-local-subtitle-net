
using System.Windows;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Regions;
using Prism.Unity;
using LemonSubtitleStudio.Views;
using LemonSubtitleStudio.ViewModels;
using LemonSubtitleStudio.Services;
using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Threading;
using Xabe.FFmpeg;

namespace LemonSubtitleStudio
{
    public partial class App : PrismApplication
    {
        private const string FfmpegDownloadUrl = "https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-win64-gpl.zip";

        public App()
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Dispatcher.UnhandledException += Dispatcher_UnhandledException;
        }

        private void Dispatcher_UnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            LogError("Dispatcher", e.Exception);
            MessageBox.Show($"应用程序启动失败: {e.Exception.Message}\n\n详情已记录到日志文件", "启动错误", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;
            LogError("UnhandledException", ex);
            MessageBox.Show($"应用程序启动失败: {ex?.Message}\n\n详情已记录到日志文件", "启动错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void LogError(string type, Exception ex)
        {
            try
            {
                var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error.log");
                var innerEx = ex?.InnerException;
                var innerStack = string.Empty;
                while (innerEx != null)
                {
                    innerStack += $"\n--- Inner Exception: {innerEx.Message}\n{innerEx.StackTrace}";
                    innerEx = innerEx.InnerException;
                }
                var logContent = $"[{DateTime.Now}] {type}: {ex?.Message}\n{ex?.StackTrace}{innerStack}\n\n";
                File.AppendAllText(logPath, logContent);
            }
            catch { }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                var ffmpegPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "ffmpeg");
                Directory.CreateDirectory(ffmpegPath);
                if (File.Exists(Path.Combine(ffmpegPath, "ffmpeg.exe")) && File.Exists(Path.Combine(ffmpegPath, "ffprobe.exe")))
                {
                    FFmpeg.SetExecutablesPath(ffmpegPath);
                }
                else
                {
                    _ = Task.Run(() => DownloadFfmpegAsync(ffmpegPath));
                }
                base.OnStartup(e);
            }
            catch (Exception ex)
            {
                LogError("OnStartup", ex);
                MessageBox.Show($"应用程序启动失败: {ex.Message}\n\n详情已记录到日志文件", "启动错误", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }

        private static async Task DownloadFfmpegAsync(string targetDir)
        {
            try
            {
                var zipPath = Path.Combine(Path.GetTempPath(), "ffmpeg_download.zip");
                if (File.Exists(zipPath)) File.Delete(zipPath);

                using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
                using var stream = await client.GetStreamAsync(FfmpegDownloadUrl);
                using var fs = File.Create(zipPath);
                await stream.CopyToAsync(fs);

                var extractPath = Path.Combine(Path.GetTempPath(), "ffmpeg_extract");
                if (Directory.Exists(extractPath)) Directory.Delete(extractPath, true);
                ZipFile.ExtractToDirectory(zipPath, extractPath);

                var ffmpegExe = Directory.GetFiles(extractPath, "ffmpeg.exe", SearchOption.AllDirectories);
                var ffprobeExe = Directory.GetFiles(extractPath, "ffprobe.exe", SearchOption.AllDirectories);
                if (ffmpegExe.Length > 0) File.Copy(ffmpegExe[0], Path.Combine(targetDir, "ffmpeg.exe"), true);
                if (ffprobeExe.Length > 0) File.Copy(ffprobeExe[0], Path.Combine(targetDir, "ffprobe.exe"), true);

                File.Delete(zipPath);
                Directory.Delete(extractPath, true);

                FFmpeg.SetExecutablesPath(targetDir);
            }
            catch
            {
            }
        }

        protected override Window CreateShell()
        {
            try
            {
                return Container.Resolve<ShellView>();
            }
            catch (Exception ex)
            {
                LogError("CreateShell", ex);
                MessageBox.Show($"创建主窗口失败: {ex.Message}\n\n详情已记录到日志文件", "启动错误", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            try
            {
                containerRegistry.RegisterSingleton<ISettingsService, SettingsService>();
                containerRegistry.RegisterSingleton<ILoggingService, LoggingService>();
                containerRegistry.RegisterSingleton<IFileService, FileService>();
                containerRegistry.RegisterSingleton<IModelManagerService, ModelManagerService>();
                containerRegistry.RegisterSingleton<IAudioService, AudioService>();
                containerRegistry.RegisterSingleton<ITranscriptionService, TranscriptionService>();
                containerRegistry.RegisterSingleton<ITranslationService, TranslationService>();
                containerRegistry.RegisterSingleton<ISubtitleService, SubtitleService>();
                containerRegistry.RegisterSingleton<IHistoryService, HistoryService>();
                containerRegistry.RegisterSingleton<ITaskQueueService, TaskQueueService>();
                containerRegistry.RegisterSingleton<ShellViewModel>();

                containerRegistry.RegisterForNavigation<VideoToSubtitleView, VideoToSubtitleViewModel>();
                containerRegistry.RegisterForNavigation<VideoToAudioView, VideoToAudioViewModel>();
                containerRegistry.RegisterForNavigation<AudioToSubtitleView, AudioToSubtitleViewModel>();
                containerRegistry.RegisterForNavigation<SubtitleTranslationView, SubtitleTranslationViewModel>();
                containerRegistry.RegisterForNavigation<SubtitleEditorView, SubtitleEditorViewModel>();
                containerRegistry.RegisterForNavigation<SettingsView, SettingsViewModel>();
            }
            catch (Exception ex)
            {
                LogError("RegisterTypes", ex);
                MessageBox.Show($"注册依赖失败: {ex.Message}\n\n详情已记录到日志文件", "启动错误", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
        {
        }
    }
}
