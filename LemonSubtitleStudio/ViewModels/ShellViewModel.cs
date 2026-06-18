using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System;
using System.Timers;

namespace LemonSubtitleStudio.ViewModels
{
    public class ShellViewModel : BindableBase, IDisposable
    {
        private readonly IRegionManager _regionManager;
        private readonly Timer _performanceTimer;
        private readonly PerformanceCounter _cpuCounter;
        private bool _disposed = false;

        public DelegateCommand<string> NavigateCommand { get; }
        public DelegateCommand CheckForUpdatesCommand { get; }
        public ObservableCollection<string> Languages { get; }
        public string SelectedLanguage { get; set; }

        public string AppVersion { get; } = GetAppVersion();

        private static string GetAppVersion()
        {
            try
            {
                var assembly = System.Reflection.Assembly.GetEntryAssembly();
                if (assembly != null)
                {
                    var ver = assembly.GetName().Version;
                    if (ver != null) return $"v{ver.Major}.{ver.Minor}.{ver.Build}";
                }
            }
            catch { }
            return "v1.0.0";
        }

        private string _memoryUsage = "0MB";
        public string MemoryUsage
        {
            get => _memoryUsage;
            set { SetProperty(ref _memoryUsage, value); }
        }

        private string _cpuUsage = "0%";
        public string CpuUsage
        {
            get => _cpuUsage;
            set { SetProperty(ref _cpuUsage, value); }
        }

        private int _threadCount = 0;
        public int ThreadCount
        {
            get => _threadCount;
            set { SetProperty(ref _threadCount, value); }
        }

        public ShellViewModel(IRegionManager regionManager)
        {
            _regionManager = regionManager;
            NavigateCommand = new DelegateCommand<string>(Navigate);
            CheckForUpdatesCommand = new DelegateCommand(CheckForUpdates);
            Languages = new ObservableCollection<string> { "中文", "English", "日本語", "한국어" };
            SelectedLanguage = "中文";

            _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            _performanceTimer = new Timer(1000);
            _performanceTimer.Elapsed += OnPerformanceTimerElapsed;
            _performanceTimer.Start();

        }

        public void InitializeNavigation()
        {
            Navigate("VideoToSubtitleView");
        }

        private void OnPerformanceTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            try
            {
                var process = Process.GetCurrentProcess();
                var memoryMb = process.WorkingSet64 / (1024 * 1024);
                MemoryUsage = $"{memoryMb}MB";

                var cpuValue = _cpuCounter.NextValue();
                CpuUsage = $"{cpuValue:F1}%";

                ThreadCount = Process.GetCurrentProcess().Threads.Count;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"性能监控失败: {ex.Message}");
            }
        }

        private void Navigate(string viewName)
        {
            try
            {
                _regionManager.RequestNavigate("ContentRegion", viewName, result =>
                {
                    if (result.Result != true)
                    {
                        var ex = result.Error;
                        var detail = ex?.ToString() ?? "导航失败";
                        System.Diagnostics.Debug.WriteLine($"导航失败: {viewName} - {detail}");
                        try
                        {
                            var logPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error.log");
                            System.IO.File.AppendAllText(logPath, $"[{DateTime.Now}] Navigate {viewName}: {detail}\n\n");
                        }
                        catch { }
                        System.Windows.MessageBox.Show($"导航失败: {ex?.Message}", "错误", 
                            System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"导航异常: {viewName} - {ex}");
                try
                {
                    var logPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error.log");
                    System.IO.File.AppendAllText(logPath, $"[{DateTime.Now}] Navigate Exception {viewName}: {ex}\n\n");
                }
                catch { }
                System.Windows.MessageBox.Show($"导航异常: {ex.Message}", "错误", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void CheckForUpdates()
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://github.com/tianxinzhe/tianxinzhe-local-subtitle-net/releases",
                UseShellExecute = true
            });
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
                _performanceTimer?.Stop();
                _performanceTimer?.Dispose();
                _cpuCounter?.Dispose();
            }
            
            _disposed = true;
        }

        ~ShellViewModel()
        {
            Dispose(false);
        }
    }
}