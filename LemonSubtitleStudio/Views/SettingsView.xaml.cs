using LemonSubtitleStudio.ViewModels;
using System.Windows.Controls;

namespace LemonSubtitleStudio.Views
{
    public partial class SettingsView : UserControl
    {
        public SettingsView(SettingsViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}