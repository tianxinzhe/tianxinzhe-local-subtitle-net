using System.Windows;
using System.Windows.Controls;
using LemonSubtitleStudio.ViewModels;

namespace LemonSubtitleStudio.Views
{
    public partial class AudioToSubtitleView : UserControl
    {
        public AudioToSubtitleView(AudioToSubtitleViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        private void DropHandler(object sender, DragEventArgs e)
        {
            if (DataContext is AudioToSubtitleViewModel viewModel)
            {
                viewModel.DropHandler(sender, e);
            }
        }
    }
}