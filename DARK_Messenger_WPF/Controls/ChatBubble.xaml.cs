using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using DARK_Messenger_WPF.Models;

namespace DARK_Messenger_WPF.Controls;

public partial class ChatBubble : UserControl
{
    private Message? _message;
    private MediaPlayer? _mediaPlayer;
    private DispatcherTimer? _audioTimer;
    private bool _isPlaying;
    private bool _audioPrepared;
    private string _audioUrl = "";

    public ChatBubble()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Unloaded += (_, _) => CleanupAudio();
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (DataContext is not Message msg) return;

        _message = msg;
        var border = (Border)Content;

        border.Background = TryFindResource(msg.IsMine ? "MessageSentBrush" : "MessageReceivedBrush") as Brush
            ?? (msg.IsMine ? new SolidColorBrush(Color.FromRgb(43, 82, 120)) : new SolidColorBrush(Color.FromRgb(24, 37, 51)));

        EditedText.Visibility = msg.IsEdited ? Visibility.Visible : Visibility.Collapsed;

        if (msg.IsMine)
        {
            StatusIcon.Text = msg.IsRead ? "✓✓" : "✓";
            StatusIcon.Visibility = Visibility.Visible;
        }
        else
        {
            StatusIcon.Visibility = Visibility.Collapsed;
        }

        ImageBorder.Visibility = Visibility.Collapsed;
        FileBorder.Visibility = Visibility.Collapsed;
        AudioBorder.Visibility = Visibility.Collapsed;
        CleanupAudio();

        if (!string.IsNullOrEmpty(msg.MediaUrl))
        {
            var baseUrl = Services.ApiClient.BaseUrl.Replace("/api", "");
            var fullUrl = $"{baseUrl}{msg.MediaUrl}";

            if (msg.MediaType == "image")
            {
                ImageBorder.Visibility = Visibility.Visible;
                _ = LoadImageAsync(fullUrl);
            }
            else if (msg.MediaType == "audio")
            {
                AudioBorder.Visibility = Visibility.Visible;
                _audioUrl = fullUrl;
                AudioDurationText.Text = "0:00";
                PlayIcon.Text = "▶";
            }
            else
            {
                FileBorder.Visibility = Visibility.Visible;
                FileNameText.Text = msg.MediaUrl?.Split('/').LastOrDefault() ?? "File";
            }
        }
    }

    private async Task LoadImageAsync(string url)
    {
        try
        {
            using var http = new System.Net.Http.HttpClient();
            var bytes = await http.GetByteArrayAsync(url);
            using var ms = new MemoryStream(bytes);
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.StreamSource = ms;
            bmp.EndInit();
            if (bmp.CanFreeze) bmp.Freeze();
            Dispatcher.Invoke(() => MessageImage.Source = bmp);
        }
        catch { }
    }

    private void Image_Click(object sender, MouseButtonEventArgs e)
    {
        if (_message?.MediaUrl == null) return;
        var baseUrl = Services.ApiClient.BaseUrl.Replace("/api", "");
        var fullUrl = $"{baseUrl}{_message.MediaUrl}";
        var viewer = new Views.ImageViewerWindow(fullUrl);
        viewer.ShowDialog();
    }

    private void AudioPlay_Click(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        if (string.IsNullOrEmpty(_audioUrl)) return;

        if (_isPlaying)
        {
            _mediaPlayer?.Pause();
            _isPlaying = false;
            PlayIcon.Text = "▶";
            return;
        }

        if (!_audioPrepared)
        {
            _mediaPlayer = new MediaPlayer();
            _mediaPlayer.MediaOpened += (_, _) =>
            {
                _audioPrepared = true;
                Dispatcher.Invoke(() =>
                {
                    var dur = _mediaPlayer.NaturalDuration;
                    if (dur.HasTimeSpan)
                        AudioDurationText.Text = $"{dur.TimeSpan.Minutes}:{dur.TimeSpan.Seconds:D2}";
                });
            };
            _mediaPlayer.MediaEnded += (_, _) =>
            {
                _isPlaying = false;
                Dispatcher.Invoke(() =>
                {
                    _mediaPlayer.Position = TimeSpan.Zero;
                    PlayIcon.Text = "▶";
                    AudioProgress.Width = 0;
                });
            };
            _mediaPlayer.MediaFailed += (_, args) =>
            {
                Dispatcher.Invoke(() => { PlayIcon.Text = "▶"; AudioDurationText.Text = "error"; });
                Console.Error.WriteLine($"ChatBubble MediaFailed: {args.ErrorException?.Message}");
            };
            _mediaPlayer.Open(new Uri(_audioUrl));
        }

        _mediaPlayer?.Play();
        _isPlaying = true;
        PlayIcon.Text = "⏸";

        _audioTimer?.Stop();
        _audioTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
        _audioTimer.Tick += (_, _) =>
        {
            if (!_isPlaying || _mediaPlayer == null || !_audioPrepared) { _audioTimer.Stop(); return; }
            var pos = _mediaPlayer.Position;
            var dur = _mediaPlayer.NaturalDuration;
            if (!dur.HasTimeSpan || dur.TimeSpan.TotalSeconds <= 0) return;
            var pct = pos.TotalSeconds / dur.TimeSpan.TotalSeconds;
            var trackWidth = AudioTrack.ActualWidth;
            if (trackWidth <= 0) return;
            Dispatcher.Invoke(() => AudioProgress.Width = Math.Round(trackWidth * pct));
        };
        _audioTimer.Start();
    }

    private void CleanupAudio()
    {
        _audioTimer?.Stop();
        _audioTimer = null;
        _mediaPlayer?.Stop();
        _mediaPlayer?.Close();
        _mediaPlayer = null;
        _isPlaying = false;
        _audioPrepared = false;
        PlayIcon.Text = "▶";
        AudioProgress.Width = 0;
    }

    private void Bubble_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_message == null) return;

        var menu = new ContextMenu();
        menu.Items.Add(new MenuItem { Header = "Ответить" });

        if (_message.IsMine)
        {
            menu.Items.Add(new MenuItem { Header = "Редактировать" });
            menu.Items.Add(new MenuItem { Header = "Удалить" });
        }

        foreach (MenuItem item in menu.Items)
        {
            item.Click += (_, _) =>
            {
                var h = (string)item.Header;
                if (h == "Ответить") ShowReply(_message);
                else if (h == "Редактировать") ShowEdit(_message);
                else if (h == "Удалить") DeleteMsg(_message);
            };
        }

        menu.IsOpen = true;
        e.Handled = true;
    }

    private void ShowReply(Message msg) => RaiseEvent(new MessageActionArgs(MessageActionType.Reply, msg));
    private void ShowEdit(Message msg) => RaiseEvent(new MessageActionArgs(MessageActionType.Edit, msg));
    private void DeleteMsg(Message msg) => RaiseEvent(new MessageActionArgs(MessageActionType.Delete, msg));

    public void UpdateContent(string content, bool isEdited)
    {
        if (_message == null) return;
        _message.Content = content;
        _message.IsEdited = isEdited;
        MessageText.Text = content;
        EditedText.Visibility = isEdited ? Visibility.Visible : Visibility.Collapsed;
    }

    public static readonly RoutedEvent MessageActionEvent = EventManager.RegisterRoutedEvent(
        "MessageAction", RoutingStrategy.Bubble, typeof(MessageActionEventHandler), typeof(ChatBubble));

    public event MessageActionEventHandler MessageAction
    {
        add => AddHandler(MessageActionEvent, value);
        remove => RemoveHandler(MessageActionEvent, value);
    }
}

public enum MessageActionType { Reply, Edit, Delete }

public delegate void MessageActionEventHandler(object sender, MessageActionArgs e);

public class MessageActionArgs : RoutedEventArgs
{
    public MessageActionType Action { get; }
    public Message Message { get; }

    public MessageActionArgs(MessageActionType action, Message message)
        : base(ChatBubble.MessageActionEvent)
    {
        Action = action;
        Message = message;
    }
}
