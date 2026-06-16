using System;
using System.Windows;
using System.Windows.Controls;
using LemonSubtitleStudio.ViewModels;

namespace LemonSubtitleStudio.Views
{
    public partial class SubtitleEditorView : UserControl
    {
        public SubtitleEditorView(SubtitleEditorViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is SubtitleEditorViewModel viewModel)
            {
                viewModel.MediaPlayerCommand += OnMediaPlayerCommand;
                MediaPlayer.MediaEnded += OnMediaEnded;
                MediaPlayer.MediaOpened += OnMediaOpened;
            }
        }

        private void OnMediaPlayerCommand(object? sender, MediaPlayerCommandEventArgs e)
        {
            switch (e.Command)
            {
                case MediaCommand.Open:
                    if (!string.IsNullOrEmpty(e.Parameter))
                    {
                        MediaPlayer.Source = new Uri(e.Parameter);
                    }
                    break;
                case MediaCommand.Play:
                    MediaPlayer.Play();
                    break;
                case MediaCommand.Pause:
                    MediaPlayer.Pause();
                    break;
                case MediaCommand.Stop:
                    MediaPlayer.Stop();
                    break;
            }
        }

        private void OnMediaEnded(object sender, RoutedEventArgs e)
        {
            MediaPlayer.Stop();
        }

        private void OnMediaOpened(object sender, RoutedEventArgs e)
        {
            if (DataContext is SubtitleEditorViewModel viewModel)
            {
                viewModel.TotalTime = MediaPlayer.NaturalDuration.HasTimeSpan
                    ? MediaPlayer.NaturalDuration.TimeSpan.ToString(@"hh\:mm\:ss")
                    : "00:00:00";
            }
        }
    }
}