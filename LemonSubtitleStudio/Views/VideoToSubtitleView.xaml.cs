using LemonSubtitleStudio.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace LemonSubtitleStudio.Views
{
    public partial class VideoToSubtitleView : UserControl
    {
        public VideoToSubtitleView(VideoToSubtitleViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        private void DropHandler(object sender, DragEventArgs e)
        {
            if (DataContext is VideoToSubtitleViewModel viewModel)
            {
                viewModel.DropHandler(sender, e);
            }
        }
    }
}