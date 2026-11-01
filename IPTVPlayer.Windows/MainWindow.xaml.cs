using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LibVLCSharp.Shared;

namespace IPTVPlayer.Windows;

public partial class MainWindow : Window
{
    private readonly HttpClient _httpClient;
    private readonly AccountStore _accountStore = new();
    private readonly ObservableCollection<IptvChannel> _visibleChannels = new();
    private readonly List<IptvChannel> _allChannels = new();
    private readonly CancellationTokenSource _shutdown = new();

    private LibVLC? _libVlc;
    private MediaPlayer? _mediaPlayer;
    private CatalogFilter _filter = CatalogFilter.All;
    private string _sportGroup = string.Empty;

    public MainWindow()
    {
        InitializeComponent();

        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(60)
        };
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("VLC/3.0 LibVLC/3.0 IPTVPlayerWindows/1.0");

        ChannelList.ItemsSource = _visibleChannels;
        SavedAccountsList.ItemsSource = _accountStore.Load();

        Loaded += MainWindow_Loaded;
        Closed += MainWindow_Closed;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            Core.Initialize();
            _libVlc = new LibVLC("--network-caching=1500");
            _mediaPlayer = new MediaPlayer(_libVlc);
            VideoView.MediaPlayer = _mediaPlayer;
        }
        catch (Exception ex)
        {
            SetStatus("Oynatıcı başlatılamadı: " + ex.Message);
        }
    }

    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        _shutdown.Cancel();
        try { _mediaPlayer?.Stop(); } catch { }
        VideoView.MediaPlayer = null;
        _mediaPlayer?.Dispose();
        _libVlc?.Dispose();
        _httpClient.Dispose();
        _shutdown.Dispose();
    }

    private async void Login_Click(object sender, RoutedEventArgs e)
    {
        await LoginAsync(ServerBox.Text, UsernameBox.Text, PasswordBox.Password);
    }

    private async void SavedAccount_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag is SavedAccount account)
            await LoginAsync(account.Server, account.Username, account.Password);
    }

    private async Task LoginAsync(string server, string username, string password)
    {
        server = server.Trim().TrimEnd('/');
        username = username.Trim();

        if (string.IsNullOrWhiteSpace(server) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            SetStatus("Sunucu, kullanıcı adı ve şifre gerekli.");
            return;
        }

        try
        {
            SetStatus("Hesap doğrulanıyor...");
            string apiUrl = $"{server}/player_api.php?username={Uri.EscapeDataString(username)}&password={Uri.EscapeDataString(password)}";
            using HttpResponseMessage apiResponse = await _httpClient.GetAsync(apiUrl, _shutdown.Token);
            apiResponse.EnsureSuccessStatusCode();
            string apiText = await apiResponse.Content.ReadAsStringAsync(_shutdown.Token);
            if (!apiText.Contains("\"auth\":1", StringComparison.OrdinalIgnoreCase) &&
                !apiText.Contains("\"auth\": 1", StringComparison.OrdinalIgnoreCase))
            {
                SetStatus("Giriş başarısız. Hesap bilgilerini kontrol edin.");
                return;
            }

            SetStatus("Kanal listesi yükleniyor...");
            string playlistUrl = $"{server}/get.php?username={Uri.EscapeDataString(username)}&password={Uri.EscapeDataString(password)}&type=m3u_plus&output=ts";
            using HttpResponseMessage playlistResponse = await _httpClient.GetAsync(playlistUrl, HttpCompletionOption.ResponseHeadersRead, _shutdown.Token);
            playlistResponse.EnsureSuccessStatusCode();
            await using Stream stream = await playlistResponse.Content.ReadAsStreamAsync(_shutdown.Token);
            List<IptvChannel> parsed = await M3uParser.ParseAsync(stream, _shutdown.Token);

            if (parsed.Count == 0)
            {
                SetStatus("Liste geldi ancak kanal bulunamadı.");
                return;
            }

            _allChannels.Clear();
            _allChannels.AddRange(parsed);
            _accountStore.SaveSuccessful(new SavedAccount { Server = server, Username = username, Password = password });
            SavedAccountsList.ItemsSource = _accountStore.Load();
            ApplyFilters();
            SetStatus($"{_allChannels.Count:N0} kanal yüklendi.");
        }
        catch (OperationCanceledException) when (_shutdown.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            SetStatus("Yükleme hatası: " + ex.Message);
        }
    }

    private void Filter_Click(object sender, RoutedEventArgs e)
    {
        string tag = (sender as Button)?.Tag?.ToString() ?? "All";
        _filter = Enum.TryParse(tag, out CatalogFilter parsed) ? parsed : CatalogFilter.All;
        SportTabs.Visibility = _filter == CatalogFilter.Sport ? Visibility.Visible : Visibility.Collapsed;
        if (_filter != CatalogFilter.Sport)
            _sportGroup = string.Empty;
        ApplyFilters();
    }

    private void SportGroup_Click(object sender, RoutedEventArgs e)
    {
        _sportGroup = (sender as Button)?.Tag?.ToString() ?? string.Empty;
        _filter = CatalogFilter.Sport;
        ApplyFilters();
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        string search = SearchBox.Text.Trim();
        IEnumerable<IptvChannel> query = _allChannels;

        query = _filter switch
        {
            CatalogFilter.Sport => query.Where(IsSport),
            CatalogFilter.Movie => query.Where(x => ContainsAny(x.Group, "film", "movie", "sinema", "vod")),
            CatalogFilter.Series => query.Where(x => ContainsAny(x.Group, "dizi", "series", "tv show")),
            CatalogFilter.Live => query.Where(x => !ContainsAny(x.Group, "film", "movie", "sinema", "vod", "dizi", "series")),
            _ => query
        };

        if (_filter == CatalogFilter.Sport && !string.IsNullOrWhiteSpace(_sportGroup))
        {
            query = query.Where(x =>
                x.Name.Contains(_sportGroup, StringComparison.OrdinalIgnoreCase) ||
                x.Group.Contains(_sportGroup, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x =>
                x.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                x.Group.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        _visibleChannels.Clear();
        foreach (IptvChannel channel in query.Take(5000))
            _visibleChannels.Add(channel);
    }

    private static bool IsSport(IptvChannel channel)
    {
        return ContainsAny(channel.Name + " " + channel.Group,
            "spor", "sport", "bein", "s sport", "tivibu", "tabii", "exxen", "eurosport", "trt spor", "a spor", "d-smart");
    }

    private static bool ContainsAny(string text, params string[] words)
    {
        return words.Any(word => text.Contains(word, StringComparison.OrdinalIgnoreCase));
    }

    private void ChannelList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        PlaySelectedChannel();
    }

    private void PlaySelectedChannel()
    {
        if (ChannelList.SelectedItem is not IptvChannel channel || _libVlc is null || _mediaPlayer is null)
            return;

        try
        {
            using var media = new Media(_libVlc, new Uri(channel.Url));
            media.AddOption(":network-caching=1500");
            media.AddOption(":http-reconnect");
            _mediaPlayer.Play(media);
            NowPlayingText.Text = channel.Name;
            SetStatus("Oynatılıyor: " + channel.Name);
        }
        catch (Exception ex)
        {
            SetStatus("Kanal açılamadı: " + ex.Message);
        }
    }

    private void PlayPause_Click(object sender, RoutedEventArgs e)
    {
        TogglePlayPause();
    }

    private void TogglePlayPause()
    {
        if (_mediaPlayer is null)
            return;

        if (_mediaPlayer.IsPlaying)
            _mediaPlayer.Pause();
        else
            _mediaPlayer.Play();
    }

    private void Fullscreen_Click(object sender, RoutedEventArgs e)
    {
        ToggleFullscreen();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && ChannelList.IsKeyboardFocusWithin)
        {
            PlaySelectedChannel();
            e.Handled = true;
        }
        else if (e.Key == Key.Space)
        {
            TogglePlayPause();
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

    private void ToggleFullscreen()
    {
        if (WindowStyle != WindowStyle.None)
        {
            WindowStyle = WindowStyle.None;
            WindowState = WindowState.Maximized;
        }
        else
        {
            WindowStyle = WindowStyle.SingleBorderWindow;
            WindowState = WindowState.Normal;
        }
    }

    private void SetStatus(string message)
    {
        StatusText.Text = message;
    }
}
