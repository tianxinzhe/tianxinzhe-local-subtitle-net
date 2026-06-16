using System.Windows;
using System.Windows.Controls;
using LemonSubtitleStudio.ViewModels;

namespace LemonSubtitleStudio.Views
{
    public partial class VideoToAudioView : UserControl
    {
        public VideoToAudioView(VideoToAudioViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        private void DropHandler(object sender, DragEventArgs e)
        {
            if (DataContext is VideoToAudioViewModel viewModel)
            {
                viewModel.DropHandler(sender, e);
            }
        }
    }
}