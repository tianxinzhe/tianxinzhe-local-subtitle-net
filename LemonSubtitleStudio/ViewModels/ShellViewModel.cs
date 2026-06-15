
using Prism.Commands;
using Prism.Regions;
using System.Collections.ObjectModel;

namespace LemonSubtitleStudio.ViewModels
{
    public class ShellViewModel
    {
        private readonly IRegionManager _regionManager;
        public DelegateCommand<string> NavigateCommand { get; }
        public ObservableCollection<string> Languages { get; }
        public string SelectedLanguage { get; set; }

        public ShellViewModel(IRegionManager regionManager)
        {
            _regionManager = regionManager;
            NavigateCommand = new DelegateCommand<string>(Navigate);
            Languages = new ObservableCollection<string> { "中文", "English", "日本語", "한국어" };
            SelectedLanguage = "中文";
            Navigate("VideoToSubtitleView");
        }

        private void Navigate(string viewName)
        {
            _regionManager.RequestNavigate("ContentRegion", viewName);
        }
    }
}
