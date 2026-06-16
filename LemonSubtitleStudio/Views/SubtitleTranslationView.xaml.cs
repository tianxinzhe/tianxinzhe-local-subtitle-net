using System.Windows;
using System.Windows.Controls;
using LemonSubtitleStudio.ViewModels;

namespace LemonSubtitleStudio.Views
{
    public partial class SubtitleTranslationView : UserControl
    {
        public SubtitleTranslationView(SubtitleTranslationViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        private void DropHandler(object sender, DragEventArgs e)
        {
            if (DataContext is SubtitleTranslationViewModel viewModel)
            {
                viewModel.DropHandler(sender, e);
            }
        }
    }
}