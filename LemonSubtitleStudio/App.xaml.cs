
using Prism.Ioc;
using Prism.Modularity;
using Prism.Regions;
using LemonSubtitleStudio.Views;
using LemonSubtitleStudio.ViewModels;
using LemonSubtitleStudio.Services;

namespace LemonSubtitleStudio
{
    public partial class App
    {
        protected override Window CreateShell()
        {
            return Container.Resolve<ShellView>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterForNavigation<VideoToSubtitleView, VideoToSubtitleViewModel>();
            containerRegistry.RegisterForNavigation<VideoToAudioView, VideoToAudioViewModel>();
            containerRegistry.RegisterForNavigation<AudioToSubtitleView, AudioToSubtitleViewModel>();
            containerRegistry.RegisterForNavigation<SubtitleTranslationView, SubtitleTranslationViewModel>();
            containerRegistry.RegisterForNavigation<SubtitleEditorView, SubtitleEditorViewModel>();
            containerRegistry.RegisterForNavigation<SettingsView, SettingsViewModel>();

            containerRegistry.RegisterSingleton<ISettingsService, SettingsService>();
            containerRegistry.RegisterSingleton<IModelManagerService, ModelManagerService>();
            containerRegistry.RegisterSingleton<ITaskQueueService, TaskQueueService>();
            containerRegistry.RegisterSingleton<ILoggingService, LoggingService>();
            containerRegistry.RegisterSingleton<IFileService, FileService>();
        }

        protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
        {
        }
    }
}
