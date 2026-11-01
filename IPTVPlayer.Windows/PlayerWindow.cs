using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using LibVLCSharp.Shared;
using LibVLCSharp.WPF;

namespace IPTVPlayer.Windows;

public sealed class PlayerWindow : Window
{
    private readonly LibVLC _libVlc;
    private readonly MediaPlayer _mediaPlayer;
    private readonly VideoView _videoView;
    private readonly Border _controls;
    private readonly Slider _volume;
    private WindowStyle _previousStyle = WindowStyle.SingleBorderWindow;
    private WindowState _previousState = WindowState.Normal;

    public PlayerWindow(LibVLC libVlc, IptvChannel channel)
    {
        _libVlc = libVlc;
        Title = channel.Name;
        Width = 1100;
        Height = 680;
        MinWidth = 700;
        MinHeight = 450;
        Background = Brushes.Black;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        KeyDown += PlayerWindow_KeyDown;

        _mediaPlayer = new MediaPlayer(_libVlc) { Volume = 80 };
        _videoView = new VideoView { MediaPlayer = _mediaPlayer };

        var overlay = new Grid();

        var panel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        panel.Children.Add(MakeButton("↶ 10", "10 sn geri", (_, _) => SeekBy(-10_000), 64));
        panel.Children.Add(MakeButton("⏯", "Oynat / Duraklat", (_, _) => TogglePlayPause(), 54));
        panel.Children.Add(MakeButton("10 ↷", "10 sn ileri", (_, _) => SeekBy(10_000), 64));

        var separator1 = MakeSeparator();
        panel.Children.Add(separator1);

        panel.Children.Add(MakeButton("🔊", "Sesi kapat / aç", (_, _) => ToggleMute(), 48));

        _volume = new Slider
        {
            Width = 150,
            Minimum = 0,
            Maximum = 100,
            Value = 80,
            Margin = new Thickness(8, 0, 8, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        _volume.ValueChanged += (_, e) => _mediaPlayer.Volume = (int)e.NewValue;
        panel.Children.Add(_volume);

        panel.Children.Add(MakeButton("🔇", "Sesi kapat / aç", (_, _) => ToggleMute(), 48));

        var separator2 = MakeSeparator();
        panel.Children.Add(separator2);

        panel.Children.Add(MakeButton("⛶", "Tam ekran (F11)", (_, _) => ToggleFullscreen(), 52));

        _controls = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(215, 17, 20, 33)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(86, 61, 132)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(14),
            Padding = new Thickness(10, 8, 10, 8),
            Margin = new Thickness(18),
            VerticalAlignment = VerticalAlignment.Bottom,
            HorizontalAlignment = HorizontalAlignment.Center,
            Child = panel,
            Visibility = Visibility.Collapsed
        };

        overlay.Children.Add(_controls);

        // LibVLCSharp.WPF overlay içerikleri VideoView.Content içinde olmalı.
        _videoView.Content = overlay;
        Content = _videoView;

        _videoView.MouseMove += (_, _) => _controls.Visibility = Visibility.Visible;
        _videoView.MouseLeave += (_, _) => _controls.Visibility = Visibility.Collapsed;

        Closed += (_, _) =>
        {
            try { _mediaPlayer.Stop(); } catch { }
            _videoView.MediaPlayer = null;
            _mediaPlayer.Dispose();
        };

        using var media = new Media(_libVlc, new Uri(channel.Url));
        media.AddOption(":network-caching=1500");
        media.AddOption(":http-reconnect");
        _mediaPlayer.Play(media);
    }

    private Button MakeButton(string text, string tooltip, RoutedEventHandler onClick, double width)
    {
        var b = new Button
        {
            Content = text,
            ToolTip = tooltip,
            Width = width,
            Margin = new Thickness(4, 0, 4, 0),
            Padding = new Thickness(8, 6, 8, 6),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            Background = new SolidColorBrush(Color.FromRgb(35, 39, 68)),
            Foreground = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.FromRgb(92, 69, 145))
        };
        b.Click += onClick;
        return b;
    }

    private static Separator MakeSeparator()
    {
        return new Separator
        {
            Width = 1,
            Height = 30,
            Margin = new Thickness(8, 0, 8, 0),
            Background = new SolidColorBrush(Color.FromRgb(85, 90, 118))
        };
    }

    private void TogglePlayPause()
    {
        if (_mediaPlayer.IsPlaying)
            _mediaPlayer.Pause();
        else
            _mediaPlayer.Play();
    }

    private void ToggleMute()
    {
        _mediaPlayer.Mute = !_mediaPlayer.Mute;
    }

    private void SeekBy(long delta)
    {
        try
        {
            long target = Math.Max(0, _mediaPlayer.Time + delta);
            if (_mediaPlayer.Length > 0)
                target = Math.Min(target, _mediaPlayer.Length - 250);
            _mediaPlayer.Time = target;
        }
        catch
        {
            // Bazı canlı yayınlar zaman atlamayı desteklemez.
        }
    }

    private void ToggleFullscreen()
    {
        if (WindowStyle != WindowStyle.None)
        {
            _previousStyle = WindowStyle;
            _previousState = WindowState;
            WindowStyle = WindowStyle.None;
            WindowState = WindowState.Maximized;
        }
        else
        {
            WindowStyle = _previousStyle;
            WindowState = _previousState;
        }
    }

    private void PlayerWindow_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Space)
        {
            TogglePlayPause();
            e.Handled = true;
        }
        else if (e.Key == Key.Left)
        {
            SeekBy(-10_000);
            e.Handled = true;
        }
        else if (e.Key == Key.Right)
        {
            SeekBy(10_000);
            e.Handled = true;
        }
        else if (e.Key == Key.F11)
        {
            ToggleFullscreen();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape && WindowStyle == WindowStyle.None)
        {
            ToggleFullscreen();
            e.Handled = true;
        }
    }
}