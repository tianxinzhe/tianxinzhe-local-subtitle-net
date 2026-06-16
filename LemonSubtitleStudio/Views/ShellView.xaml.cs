using System.Windows;
using LemonSubtitleStudio.ViewModels;

namespace LemonSubtitleStudio.Views
{
    public partial class ShellView : Window
    {
        public ShellView(ShellViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            Loaded += (s, e) => viewModel.InitializeNavigation();
        }
    }
}
