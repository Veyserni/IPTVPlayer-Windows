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

        var root = new Grid();
        Content = root;
        root.Children.Add(_videoView);

        var panel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        panel.Children.Add(MakeButton("↶ 10", (_, _) => SeekBy(-10_000), 64));
        panel.Children.Add(MakeButton("⏯", (_, _) => TogglePlayPause(), 54));
        panel.Children.Add(MakeButton("10 ↷", (_, _) => SeekBy(10_000), 64));
        panel.Children.Add(MakeButton("🔊", (_, _) => _mediaPlayer.Mute = !_mediaPlayer.Mute, 48));

        _volume = new Slider { Width = 150, Minimum = 0, Maximum = 100, Value = 80, Margin = new Thickness(8,0,8,0) };
        _volume.ValueChanged += (_, e) => _mediaPlayer.Volume = (int)e.NewValue;
        panel.Children.Add(_volume);

        panel.Children.Add(MakeButton("⛶", (_, _) => ToggleFullscreen(), 52));

        _controls = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(210, 17, 20, 33)),
            CornerRadius = new CornerRadius(14),
            Padding = new Thickness(10),
            Margin = new Thickness(18),
            VerticalAlignment = VerticalAlignment.Bottom,
            HorizontalAlignment = HorizontalAlignment.Center,
            Child = panel,
            Visibility = Visibility.Collapsed
        };
        root.Children.Add(_controls);

        root.MouseEnter += (_, _) => _controls.Visibility = Visibility.Visible;
        root.MouseLeave += (_, _) => _controls.Visibility = Visibility.Collapsed;

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

    private Button MakeButton(string text, RoutedEventHandler onClick, double width)
    {
        var b = new Button
        {
            Content = text,
            Width = width,
            Margin = new Thickness(4,0,4,0),
            Padding = new Thickness(8,6,8,6),
            HorizontalContentAlignment = HorizontalAlignment.Center
        };
        b.Click += onClick;
        return b;
    }

    private void TogglePlayPause()
    {
        if (_mediaPlayer.IsPlaying) _mediaPlayer.Pause();
        else _mediaPlayer.Play();
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
        catch { }
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