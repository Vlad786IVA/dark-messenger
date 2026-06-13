using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using DARK_Messenger_WPF.Controls;
using DARK_Messenger_WPF.Models;
using DARK_Messenger_WPF.Services;
using NAudio.Wave;

namespace DARK_Messenger_WPF.Views;

public partial class ChatWindow : Window
{
    private readonly ObservableCollection<Chat> _chats = new();
    private readonly ObservableCollection<Message> _messages = new();
    private readonly ObservableCollection<Contact> _foundUsers = new();
    private readonly DispatcherTimer _pollTimer;
    private readonly DispatcherTimer _searchTimer;
    private readonly DispatcherTimer _typingTimer;
    private readonly CollectionViewSource _chatViewSource = new();
    private readonly TrayService _tray;
    private readonly DispatcherTimer _typingAnimTimer;
    private int _animDotCount;
    private Chat? _selectedChat;
    private int _lastMessageId;
    private int _currentPage = 1;
    private bool _hasMoreMessages = true;
    private bool _loadingMessages;
    private bool _isShowingSearch;
    private bool _isSearchingMessages;
    private Message? _editingMessage;
    private Message? _replyToMessage;
    private DateTime _lastTypingSent = DateTime.MinValue;
    private bool _wsConnected;
    private bool _wsSubscribed;
    private WaveInEvent? _waveIn;
    private WaveFileWriter? _writer;
    private string? _tempAudioFile;
    private bool _isRecording;

    private static readonly Dictionary<string, string[]> EmojiCategories = new()
    {
        ["😊"] = new[] { "😊","😂","🤣","❤️","😍","🥰","😘","😗","😙","😚","🥲","😀","😁","😅","🤣","😂","🙂","😉","😌","😢","😭","😤","😠","😡","🤬","🥺","😳","🤔","🤗","🤭","🤫","😎","🤓","🧐","😏","😩","😫","🥱","😴","🤤","😪" },
        ["👍"] = new[] { "👍","👎","👋","✋","🖐️","🤚","👌","✌️","🤞","🤟","🤘","🤙","👈","👉","👆","👇","☝️","🖕","🙏","💪","🦵","🦶","👐","🤲","🙌","👏","🤝","💅","🤳","💍","👑","🎒","👔","👕","👖","🧣","🧤","🧥","🧦","👗","👘" },
        ["❤️"] = new[] { "❤️","🧡","💛","💚","💙","💜","🖤","🤍","🤎","💕","💞","💗","💖","💘","💝","💟","❣️","♥️","💌","💋","👩‍❤️‍👨","💑","👨‍❤️‍👨","👩‍❤️‍👩","😻","💏","💐","🌸","🌺","🌹","🌷","🌻","🌼","🌿","🌱","☘️","🍀","🌵","🌴","🌲" },
        ["🎉"] = new[] { "🎉","🎊","🎀","🎁","🎈","🎃","🎄","🎆","🎇","✨","🎓","🎯","🎲","🎳","🎮","🎭","🎨","🎵","🎶","🎤","🎧","🎸","🎺","🥁","🎷","🪕","🎻","📯","🎙️","🎚️","🎛️","🎼","🎬","🏆","🏅","🥇","🥈","🥉","🏆","🏵️","🎖️" },
        ["🍕"] = new[] { "🍕","🍔","🍟","🌭","🍿","🧇","🥞","🧀","🥩","🍗","🍖","🌮","🌯","🍜","🍝","🍲","🍛","🍣","🍱","🥟","🦪","🍤","🍙","🍚","🍘","🍥","🥠","🥮","🍡","🥟","🍦","🍩","🍪","🎂","🍰","🧁","🥧","🍫","🍬","🍭","🍮" },
        ["⚽"] = new[] { "⚽","🏀","🏈","⚾","🥎","🎾","🏐","🏉","🎱","🪀","🏓","🏸","🏒","🏑","🥍","🏏","⛳","🥊","🤿","🏹","🎣","🤺","⛸️","🛷","🎿","⛷️","🏂","🪂","🏋️","🤼","🤸","🤾","⛹️","🏊","🤽","🚣","🧗","🚴","🏄","🧘","🏇" },
        ["🐱"] = new[] { "🐱","🐶","🐭","🐹","🐰","🦊","🐻","🐼","🐨","🐯","🦁","🐮","🐷","🐸","🐵","🐔","🐧","🐦","🐤","🐣","🐥","🦆","🦅","🦉","🦇","🐺","🐗","🐴","🦄","🐝","🐛","🦋","🐌","🐞","🐜","🦟","🦗","🕷️","🦂","🐢","🐍" },
        ["🌍"] = new[] { "🌍","🌎","🌏","🌐","🗺️","🗾","🏔️","⛰️","🌋","🏜️","🏝️","🏖️","🏛️","🏗️","🏘️","🏙️","🌃","🌌","🌠","🌈","☀️","🌤️","⛅","🌥️","☁️","🌦️","🌧️","⛈️","🌩️","🌨️","❄️","☃️","⛄","🌪️","🌫️","🌊","💧","💨","☔","🔥","✨" },
    };

    private class SettingsSection
    {
        public string Icon { get; set; } = "";
        public string Name { get; set; } = "";
    }

    private static readonly SettingsSection[] SettingsSections =
    {
        new() { Icon = "👤", Name = "Профиль" },
        new() { Icon = "🎨", Name = "Оформление" },
        new() { Icon = "🔔", Name = "Уведомления" },
        new() { Icon = "🔒", Name = "Конфиденциальность" },
        new() { Icon = "🌐", Name = "Язык" },
        new() { Icon = "📱", Name = "Сессии" },
        new() { Icon = "ℹ️", Name = "О программе" },
        new() { Icon = "🚪", Name = "Выйти" },
    };

    private string _currentEmojiCategory = "😊";

    public ChatWindow()
    {
        InitializeComponent();
        _tray = new TrayService(this);
        _tray.Show();

        MessageList.ItemsSource = _messages;
        SearchResults.ItemsSource = _foundUsers;
        _chatViewSource.Source = _chats;
        ChatList.ItemsSource = _chatViewSource.View;
        LoadEmojis();
        PopulateSettingsSections();

        _pollTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        _pollTimer.Tick += PollTimer_Tick;
        _searchTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        _searchTimer.Tick += SearchTimer_Tick;
        _typingTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.5) };
        _typingTimer.Tag = null;
        _typingTimer.Tick += (_, _) => {
            if (_typingTimer.Tag is ValueTuple<int, bool> state)
                _ = WebSocketService.SendTyping(state.Item1, false, state.Item2);
            _typingTimer.Stop();
        };

        _typingAnimTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
        _typingAnimTimer.Tick += (_, _) =>
        {
            _animDotCount = (_animDotCount % 3) + 1;
            TypingIndicator.Text = "печатает" + new string('.', _animDotCount);
        };

        Loaded += Window_Loaded;
        Closing += Window_Closing;
    }

    private static void AnimateIn(FrameworkElement el, double fromY = 20, double ms = 200)
    {
        el.Visibility = Visibility.Visible;
        el.Opacity = 0;
        el.RenderTransform = new TranslateTransform(0, fromY);
        el.RenderTransformOrigin = new Point(0.5, 0.5);
        var sb = new Storyboard();
        var fade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(ms));
        Storyboard.SetTarget(fade, el);
        Storyboard.SetTargetProperty(fade, new PropertyPath("Opacity"));
        var slide = new DoubleAnimation(fromY, 0, TimeSpan.FromMilliseconds(ms));
        slide.EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut };
        Storyboard.SetTarget(slide, el);
        Storyboard.SetTargetProperty(slide, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
        sb.Children.Add(fade);
        sb.Children.Add(slide);
        sb.Begin();
    }

    private static void AnimateOut(FrameworkElement el, double toY = -20, double ms = 150)
    {
        var sb = new Storyboard();
        var fade = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(ms));
        Storyboard.SetTarget(fade, el);
        Storyboard.SetTargetProperty(fade, new PropertyPath("Opacity"));
        var slide = new DoubleAnimation(0, toY, TimeSpan.FromMilliseconds(ms));
        slide.EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn };
        Storyboard.SetTarget(slide, el);
        Storyboard.SetTargetProperty(slide, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
        sb.Children.Add(fade);
        sb.Children.Add(slide);
        sb.Completed += (_, _) => el.Visibility = Visibility.Collapsed;
        sb.Begin();
    }

    private void SubscribeWebSocket()
    {
        if (_wsSubscribed) return;
        _wsSubscribed = true;
        WebSocketService.OnOnlineStatus += Ws_OnOnlineStatus;
        WebSocketService.OnNewMessage += Ws_OnNewMessage;
        WebSocketService.OnNewGroupMessage += Ws_OnNewGroupMessage;
        WebSocketService.OnEditMessage += Ws_OnEditMessage;
        WebSocketService.OnDeleteMessage += Ws_OnDeleteMessage;
        WebSocketService.OnTyping += Ws_OnTyping;
        WebSocketService.OnConnectionChanged += Ws_OnConnectionChanged;
        _wsConnected = WebSocketService.IsConnected;
        UpdateConnIndicator();
    }

    private void UnsubscribeWebSocket()
    {
        if (!_wsSubscribed) return;
        _wsSubscribed = false;
        WebSocketService.OnOnlineStatus -= Ws_OnOnlineStatus;
        WebSocketService.OnNewMessage -= Ws_OnNewMessage;
        WebSocketService.OnNewGroupMessage -= Ws_OnNewGroupMessage;
        WebSocketService.OnEditMessage -= Ws_OnEditMessage;
        WebSocketService.OnDeleteMessage -= Ws_OnDeleteMessage;
        WebSocketService.OnTyping -= Ws_OnTyping;
        WebSocketService.OnConnectionChanged -= Ws_OnConnectionChanged;
    }

    private void Ws_OnOnlineStatus(int id, bool online)
    {
        Dispatcher.InvokeAsync(() =>
        {
            foreach (var c in _chats) if (c.OtherUser?.Id == id) c.OtherUser.IsOnline = online;
            if (_selectedChat?.OtherUser?.Id == id)
                OnlineStatus.Text = online ? "в сети" : "";
        });
    }

    private void Ws_OnNewMessage(int chatId, Message msg)
    {
        Dispatcher.InvokeAsync(() =>
        {
            if (_selectedChat?.Id == chatId)
            {
                _messages.Add(msg);
                _lastMessageId = msg.Id;
                MessageScroll.ScrollToEnd();
            }
            else
            {
                var c = _chats.FirstOrDefault(x => x.Id == chatId);
                if (c != null) c.UnreadCount++;
            }
            UpdateChatLast(msg, chatId, false);
            if (!msg.IsMine) { PlayNotification(); var chat = _chats.FirstOrDefault(x => x.Id == chatId); ShowTrayNotification(chat?.Name ?? "Новое сообщение", msg.Content); }
        });
    }

    private void Ws_OnNewGroupMessage(int gId, Message msg)
    {
        Dispatcher.InvokeAsync(() =>
        {
            if (_selectedChat?.Id == gId && _selectedChat.IsGroup)
            {
                _messages.Add(msg);
                _lastMessageId = msg.Id;
                MessageScroll.ScrollToEnd();
            }
            else
            {
                var c = _chats.FirstOrDefault(x => x.Id == gId && x.IsGroup);
                if (c != null) c.UnreadCount++;
            }
            UpdateChatLast(msg, gId, true);
            if (!msg.IsMine) { PlayNotification(); var chat = _chats.FirstOrDefault(x => x.Id == gId && x.IsGroup); ShowTrayNotification(chat?.Name ?? "Группа", msg.Content); }
        });
    }

    private void Ws_OnEditMessage(int chatId, int msgId, string content)
    {
        Dispatcher.InvokeAsync(() =>
        {
            var m = _messages.FirstOrDefault(x => x.Id == msgId);
            if (m != null) { m.Content = content; m.IsEdited = true; }
        });
    }

    private void Ws_OnDeleteMessage(int chatId, int msgId)
    {
        Dispatcher.InvokeAsync(() =>
        {
            var m = _messages.FirstOrDefault(x => x.Id == msgId);
            if (m != null) _messages.Remove(m);
        });
    }

    private void Ws_OnTyping(int chatId, int userId, bool isTyping)
    {
        Dispatcher.InvokeAsync(() =>
        {
            if (_selectedChat?.Id == chatId && _selectedChat?.OtherUser?.Id == userId)
            {
                if (isTyping)
                {
                    _animDotCount = 1;
                    TypingIndicator.Text = "печатает.";
                    TypingIndicator.Visibility = Visibility.Visible;
                    _typingAnimTimer.Start();
                }
                else
                {
                    _typingAnimTimer.Stop();
                    TypingIndicator.Visibility = Visibility.Collapsed;
                }
            }
        });
    }

    private void Ws_OnConnectionChanged(bool ok)
    {
        Dispatcher.InvokeAsync(() =>
        {
            _wsConnected = ok;
            UpdateConnIndicator();
            if (ok)
            {
                _pollTimer.Stop();
                OnlineStatus.Text = _selectedChat?.OtherUser?.IsOnline == true ? "в сети" : "";
            }
            else if (_selectedChat != null && !string.IsNullOrEmpty(ApiClient.Token))
            {
                _pollTimer.Start();
            }
        });
    }

    private void UpdateConnIndicator()
    {
        ConnIndicator.Background = _wsConnected
            ? new SolidColorBrush(Color.FromRgb(42, 171, 238))
            : new SolidColorBrush(Color.FromRgb(102, 102, 102));
        if (!_wsConnected) OnlineStatus.Text = "нет подключения";
    }

    private void UpdateChatLast(Message msg, int chatId, bool isGroup)
    {
        var c = _chats.FirstOrDefault(x => x.Id == chatId && x.IsGroup == isGroup);
        if (c != null) { c.LastMessage = msg.Content; c.LastMessageTime = msg.SentAt; }
    }

    private void PlayNotification()
    {
        try { System.Media.SystemSounds.Asterisk.Play(); } catch { }
    }

    private void ShowTrayNotification(string senderName, string preview)
    {
        if (WindowState == WindowState.Minimized || !IsVisible)
            _tray.ShowNotification(senderName, preview);
    }

    private void LoadEmojis()
    {
        foreach (var kv in EmojiCategories)
        {
            var catBtn = new Button { Content = kv.Key, FontSize = 16, Width = 32, Height = 32, Background = Brushes.Transparent, BorderThickness = new Thickness(0), Cursor = Cursors.Hand, Tag = kv.Key };
            catBtn.Click += (s, _) =>
            {
                if (s is Button b && b.Tag is string key)
                {
                    _currentEmojiCategory = key;
                    ShowEmojiCategory(key);
                }
            };
            EmojiCategoryBar.Children.Add(catBtn);
        }
        ShowEmojiCategory(_currentEmojiCategory);
    }

    private void ShowEmojiCategory(string key)
    {
        EmojiPanel.Children.Clear();
        if (!EmojiCategories.TryGetValue(key, out var emojis)) return;
        foreach (var e in emojis)
        {
            var btn = new Button { Content = e, FontSize = 22, Width = 38, Height = 38, Background = Brushes.Transparent, BorderThickness = new Thickness(0), Cursor = Cursors.Hand };
            btn.Click += (_, _) => { MessageInput.Text += e; MessageInput.Focus(); MessageInput.CaretIndex = MessageInput.Text.Length; };
            EmojiPanel.Children.Add(btn);
        }
        foreach (var child in EmojiCategoryBar.Children)
        {
            if (child is Button b)
                b.FontSize = (b.Tag?.ToString() == key) ? 20 : 16;
        }
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        SubscribeWebSocket();
        MessageList.AddHandler(ChatBubble.MessageActionEvent, new MessageActionEventHandler(OnMessageAction));
        await LoadChats();
        if (!string.IsNullOrEmpty(ApiClient.Token)) await WebSocketService.Connect(ApiClient.Token);
        LoadSettingsUserInfo();
        ShowChatsView();
    }

    private void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (IsVisible)
        {
            SubscribeWebSocket();
            if (!_wsConnected && _selectedChat != null && !string.IsNullOrEmpty(ApiClient.Token))
                _pollTimer.Start();
        }
        else
        {
            _pollTimer.Stop();
        }
    }

    private async Task LoadSessions()
    {
        var sessions = await ApiClient.GetSessions();
        if (sessions != null)
            SessionsList.ItemsSource = new System.Collections.ObjectModel.ObservableCollection<Session>(sessions);
    }

    private async void TerminateSession_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is int sessionId)
        {
            btn.IsEnabled = false;
            var ok = await ApiClient.DeleteSession(sessionId);
            if (ok) await LoadSessions();
            else btn.IsEnabled = true;
        }
    }

    private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (Tag?.ToString() != "forceClose")
        {
            e.Cancel = true;
            _typingAnimTimer.Stop();
            _pollTimer.Stop();
            TypingIndicator.Visibility = Visibility.Collapsed;
            CleanupRecording();
            UnsubscribeWebSocket();
            Hide();
            _tray.Show();
            return;
        }

        _typingAnimTimer.Stop();
        CleanupRecording();
        UnsubscribeWebSocket();
        _pollTimer.Stop();
        _searchTimer.Stop();
        _typingTimer.Stop();
        _tray.Hide();
        _tray.Dispose();
        _ = WebSocketService.Disconnect();
    }

    private void CleanupRecording()
    {
        if (_waveIn != null)
        {
            try { _waveIn.StopRecording(); } catch { }
            _waveIn.Dispose();
            _waveIn = null;
        }
        _writer?.Dispose();
        _writer = null;
        _isRecording = false;
        MicBtn.Content = "🎤";
        MessageInput.IsEnabled = true;
    }

    private void Window_PreviewDragOver(object sender, DragEventArgs e)
    {
        e.Effects = _selectedChat != null && e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private async void Window_Drop(object sender, DragEventArgs e)
    {
        if (_selectedChat == null || !e.Data.GetDataPresent(DataFormats.FileDrop)) return;
        var files = (string[])e.Data.GetData(DataFormats.FileDrop);
        foreach (var file in files) await UploadAndSendFile(file);
    }

    private async Task UploadAndSendFile(string path)
    {
        var result = await ApiClient.UploadFile(path);
        if (result.HasValue)
        {
            var (url, mediaType) = result.Value;
            if (url != null)
            {
                if (_selectedChat!.IsGroup)
                    await ApiClient.SendGroupMessage(_selectedChat.Id, "", url, mediaType);
                else
                    await ApiClient.SendMessage(_selectedChat.Id, "", url, mediaType);
                await LoadMessages(_selectedChat.Id);
            }
        }
    }

    private async Task LoadChats()
    {
        if (string.IsNullOrEmpty(ApiClient.Token)) return;
        var chats = await ApiClient.GetChats();
        var groups = await ApiClient.GetGroups();
        _chats.Clear();
        if (chats != null) foreach (var c in chats) _chats.Add(c);
        if (groups != null) foreach (var g in groups) _chats.Add(g);
        UpdateChatListVisibility();
    }

    private void UpdateChatListVisibility()
    {
        if (_isShowingSearch) return;
        NoChatsHint.Visibility = _chats.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        ChatList.Visibility = _chats.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void ShowChatsView()
    {
        SettingsLeftPanel.Visibility = Visibility.Collapsed;
        SettingsContentPanel.Visibility = Visibility.Collapsed;
        ChatViewPanel.Opacity = 0;
        ChatViewPanel.Visibility = Visibility.Visible;
        _isShowingSearch = false;
        ChatSearchBox.Visibility = Visibility.Visible;
        SearchPanel.Visibility = Visibility.Collapsed;
        NoChatsHint.Visibility = _chats.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        ChatList.Visibility = _chats.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        ChatsTab.Background = (Brush)FindResource("PrimaryBrush");
        ContactsTab.Background = Brushes.Transparent;
        NavSettingsBtn.Foreground = (Brush)TryFindResource("TextSecondaryBrush") ?? Brushes.Gray;
        NavChatsBtn.Foreground = (Brush)TryFindResource("AccentBrush") ?? Brushes.DodgerBlue;
        var fade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200));
        Storyboard.SetTarget(fade, ChatViewPanel);
        Storyboard.SetTargetProperty(fade, new PropertyPath("Opacity"));
        new Storyboard { Children = { fade } }.Begin();
    }

    private void ShowSettingsView()
    {
        _pollTimer.Stop();
        ChatViewPanel.Visibility = Visibility.Collapsed;
        SettingsLeftPanel.Opacity = 0;
        SettingsLeftPanel.Visibility = Visibility.Visible;
        SettingsContentPanel.Opacity = 0;
        SettingsContentPanel.Visibility = Visibility.Visible;
        NavChatsBtn.Foreground = (Brush)TryFindResource("TextSecondaryBrush") ?? Brushes.Gray;
        NavSettingsBtn.Foreground = (Brush)TryFindResource("AccentBrush") ?? Brushes.DodgerBlue;
        LoadSettingsUserInfo();
        SettingsSectionList.SelectedIndex = 0;
        AnimateIn(SettingsLeftPanel, 0, 150);
        AnimateIn(SettingsContentPanel, 16, 220);
    }

    private void LoadSettingsUserInfo()
    {
        var username = SettingsService.Username;
        var displayName = SettingsService.DisplayName;
        SettingsUsernameText.Text = $"@{username}";
        SettingsDisplayNameHeader.Text = displayName;
        SettingsDisplayNameBox.Text = displayName;
        SettingsAvatarText.Text = displayName.Length > 0 ? displayName[0].ToString() : "?";
        UpdateThemeDisplay();
    }

    private void UpdateThemeDisplay()
    {
        var isDark = SettingsService.IsDarkTheme;
        SettingsThemeIcon.Text = isDark ? "🌙" : "☀️";
        SettingsThemeLabel.Text = isDark ? "Тёмная тема" : "Светлая тема";
        SettingsThemeDesc.Text = isDark ? "Нажмите, чтобы переключить на светлую" : "Нажмите, чтобы переключить на тёмную";
    }

    private void ShowChats(object? s = null, MouseButtonEventArgs? e = null)
    {
        ShowChatsView();
        ChatSearchBox.Text = "";
    }
    private void NavChats_Click(object s, RoutedEventArgs e) => ShowChats();
    private void NavSettings_Click(object s, RoutedEventArgs e) => ShowSettingsView();
    private void ShowContacts(object? s = null, MouseButtonEventArgs? e = null)
    {
        _isShowingSearch = true;
        ChatSearchBox.Visibility = Visibility.Collapsed;
        ChatList.Visibility = Visibility.Collapsed;
        NoChatsHint.Visibility = Visibility.Collapsed;
        SearchPanel.Visibility = Visibility.Visible;
        ChatsTab.Background = Brushes.Transparent;
        ContactsTab.Background = (Brush)FindResource("PrimaryBrush");
        SearchUserBox.Focus();
    }

    private void NewChatBtn_Click(object s, RoutedEventArgs e) => ShowContacts();
    private async void SettingsSave_Click(object s, RoutedEventArgs e)
    {
        var newName = SettingsDisplayNameBox.Text.Trim();
        SettingsStatusText.Visibility = Visibility.Collapsed;
        if (string.IsNullOrEmpty(newName)) { SettingsStatusText.Text = "Имя не может быть пустым"; SettingsStatusText.Foreground = new SolidColorBrush(Color.FromRgb(233, 69, 96)); SettingsStatusText.Visibility = Visibility.Visible; return; }

        var result = await ApiClient.UpdateProfile(newName);
        if (result != null)
        {
            SettingsService.DisplayName = newName;
            SettingsStatusText.Text = "Сохранено";
            SettingsStatusText.Foreground = (Brush)TryFindResource("AccentBrush") ?? Brushes.DodgerBlue;
            SettingsAvatarText.Text = newName[0].ToString();
            SettingsDisplayNameHeader.Text = newName;
            await LoadChats();
        }
        else
        {
            SettingsStatusText.Text = "Ошибка сохранения";
            SettingsStatusText.Foreground = new SolidColorBrush(Color.FromRgb(233, 69, 96));
        }
        SettingsStatusText.Visibility = Visibility.Visible;
    }

    private async void SettingsTheme_Click(object s, RoutedEventArgs e)
    {
        var wasDark = SettingsService.IsDarkTheme;
        ThemeOverlay.Visibility = Visibility.Visible;
        ThemeOverlay.Opacity = 0;
        var fadeIn = new DoubleAnimation(0, 0.25, TimeSpan.FromMilliseconds(120));
        var fadeOut = new DoubleAnimation(0.25, 0, TimeSpan.FromMilliseconds(120));
        var storyIn = new Storyboard();
        storyIn.Children.Add(fadeIn);
        Storyboard.SetTarget(fadeIn, ThemeOverlay);
        Storyboard.SetTargetProperty(fadeIn, new PropertyPath("Opacity"));

        var tcs = new TaskCompletionSource();
        storyIn.Completed += (_, _) => tcs.TrySetResult();
        storyIn.Begin();
        await tcs.Task;

        App.SwitchTheme(!wasDark);
        UpdateThemeDisplay();

        var storyOut = new Storyboard();
        storyOut.Children.Add(fadeOut);
        Storyboard.SetTarget(fadeOut, ThemeOverlay);
        Storyboard.SetTargetProperty(fadeOut, new PropertyPath("Opacity"));
        var tcs2 = new TaskCompletionSource();
        storyOut.Completed += (_, _) => tcs2.TrySetResult();
        storyOut.Begin();
        await tcs2.Task;

        ThemeOverlay.Visibility = Visibility.Collapsed;
    }

    private async void SettingsAvatar_Click(object s, MouseButtonEventArgs e)
    {
        var d = new Microsoft.Win32.OpenFileDialog { Title = "Выберите аватар", Filter = "Images|*.jpg;*.jpeg;*.png;*.gif;*.bmp" };
        if (d.ShowDialog() == true)
        {
            SettingsStatusText.Visibility = Visibility.Collapsed;
            var url = await ApiClient.UploadAvatar(d.FileName);
            if (url != null)
            {
                SettingsStatusText.Text = "Аватар обновлён";
                SettingsStatusText.Foreground = (Brush)TryFindResource("AccentBrush") ?? Brushes.DodgerBlue;
                SettingsStatusText.Visibility = Visibility.Visible;
            }
        }
    }

    private void Logout_Click(object s, RoutedEventArgs e)
    {
        var result = MessageBox.Show("Выйти из аккаунта?", "DARK", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result != MessageBoxResult.Yes) return;
        SettingsService.Token = "";
        Tag = "forceClose";
        Close();
        var login = new LoginWindow();
        login.Show();
    }

    private void PopulateSettingsSections()
    {
        SettingsSectionList.ItemsSource = SettingsSections;
    }

    private void SettingsBack_Click(object s, RoutedEventArgs e)
    {
        ShowChatsView();
    }

    private void SettingsSectionList_SelectionChanged(object s, SelectionChangedEventArgs e)
    {
        if (SettingsSectionList.SelectedItem is not SettingsSection section) return;

        if (section.Name == "Выйти")
        {
            SettingsSectionList.SelectedIndex = -1;
            Logout_Click(s, e);
            return;
        }

        var pages = new[] { SettingsPageProfile, SettingsPageAppearance, SettingsPageNotifications,
                            SettingsPagePrivacy, SettingsPageLanguage, SettingsPageSessions, SettingsPageAbout };
        FrameworkElement? shown = null;
        foreach (var p in pages) p.Visibility = Visibility.Collapsed;

        switch (section.Name)
        {
            case "Профиль": shown = SettingsPageProfile; break;
            case "Оформление": shown = SettingsPageAppearance; break;
            case "Уведомления": shown = SettingsPageNotifications; break;
            case "Конфиденциальность": shown = SettingsPagePrivacy; break;
            case "Язык": shown = SettingsPageLanguage; break;
            case "Сессии": shown = SettingsPageSessions; _ = LoadSessions(); break;
            case "О программе": shown = SettingsPageAbout; break;
        }

        if (shown != null) AnimateIn(shown, 24, 220);
    }

    private async void CreateGroup_Click(object s, RoutedEventArgs e)
    {
        var w = new NewGroupWindow { Owner = this };
        if (w.ShowDialog() == true && w.CreatedGroupId != null)
        {
            await LoadChats();
            var chat = _chats.FirstOrDefault(c => c.Id == w.CreatedGroupId && c.IsGroup);
            if (chat != null) ChatList.SelectedItem = chat;
        }
    }

    private void ChatList_RightClick(object sender, MouseButtonEventArgs e)
    {
        var fe = e.OriginalSource as FrameworkElement;
        var chat = fe?.DataContext as Chat;
        if (chat == null || chat.IsGroup) return;

        var menu = new ContextMenu();
        var del = new MenuItem { Header = "Удалить чат" };
        del.Click += async (_, _) =>
        {
            await ApiClient.DeleteChat(chat.Id);
            if (_selectedChat?.Id == chat.Id) { _selectedChat = null; _messages.Clear(); HideChatPanel(); }
            await LoadChats();
        };
        menu.Items.Add(del);
        menu.IsOpen = true;
        e.Handled = true;
    }

    private void HideChatPanel()
    {
        ChatHeader.Visibility = Visibility.Collapsed;
        MsgSearchBar.Visibility = Visibility.Collapsed;
        MessageScroll.Visibility = Visibility.Collapsed;
        InputArea.Visibility = Visibility.Collapsed;
        ReplyBar.Visibility = Visibility.Collapsed;
        NoChatText.Visibility = Visibility.Visible;
    }

    private void SearchUserBox_TextChanged(object s, TextChangedEventArgs e) { _searchTimer.Stop(); _searchTimer.Start(); }

    private async void SearchTimer_Tick(object? s, EventArgs e)
    {
        _searchTimer.Stop();
        await SearchUsers();
    }

    private async Task SearchUsers()
    {
        var q = SearchUserBox.Text.Trim();
        if (string.IsNullOrEmpty(q) || q == SearchUserBox.Tag?.ToString()) { _foundUsers.Clear(); return; }
        var users = await ApiClient.SearchUsers(q);
        _foundUsers.Clear();
        foreach (var u in users) _foundUsers.Add(u);
    }

    private async void SearchResults_SelectionChanged(object s, SelectionChangedEventArgs e)
    {
        if (SearchResults.SelectedItem is not Contact contact) return;
        SearchResults.SelectedItem = null;
        var chatId = await ApiClient.CreateChat(contact.UserId);
        if (chatId == null) return;
        await LoadChats();
        ShowChatsView();
        var chat = _chats.FirstOrDefault(c => c.OtherUser?.Id == contact.UserId);
        if (chat != null) ChatList.SelectedItem = chat;
    }

    private async void ChatList_SelectionChanged(object s, SelectionChangedEventArgs e)
    {
        if (ChatList.SelectedItem is Chat chat) await SelectChat(chat);
    }

    private async Task SelectChat(Chat chat)
    {
        _pollTimer.Stop();
        _editingMessage = null;
        _replyToMessage = null;
        _selectedChat = chat;
        chat.UnreadCount = 0;
        ReplyBar.Visibility = Visibility.Collapsed;
        ChatSearchBox.Text = "";
        _currentPage = 1;
        _hasMoreMessages = true;

        await LoadMessages(chat.Id);

        ChatHeader.Visibility = Visibility.Visible;
        MessageScroll.Visibility = Visibility.Visible;
        InputArea.Visibility = Visibility.Visible;
        NoChatText.Visibility = Visibility.Collapsed;
        ChatNameText.Text = chat.Name;
        OnlineStatus.Text = chat.OtherUser?.IsOnline == true ? "в сети" : "";

        if (!_wsConnected && !string.IsNullOrEmpty(ApiClient.Token))
            _pollTimer.Start();
    }

    private async Task LoadMessages(int id, int page = 1)
    {
        if (string.IsNullOrEmpty(ApiClient.Token)) return;
        var msgs = _selectedChat!.IsGroup ? await ApiClient.GetGroupMessages(id, page) : await ApiClient.GetMessages(id, page);
        _messages.Clear();
        if (msgs != null) foreach (var m in msgs) _messages.Add(m);
        if (msgs?.Count > 0) _lastMessageId = msgs.Max(m => m.Id);
        if (msgs == null || msgs.Count < 50) _hasMoreMessages = false;
        MessageScroll.ScrollToEnd();
    }

    private async Task LoadOlderMessages()
    {
        if (_selectedChat == null || _loadingMessages || !_hasMoreMessages) return;
        _loadingMessages = true;
        try
        {
            var nextPage = _currentPage + 1;
            var prevCount = _messages.Count;
            var prevScrollable = MessageScroll.ScrollableHeight;
            var prevOffset = MessageScroll.VerticalOffset;

            var msgs = _selectedChat.IsGroup
                ? await ApiClient.GetGroupMessages(_selectedChat.Id, nextPage)
                : await ApiClient.GetMessages(_selectedChat.Id, nextPage);
            if (msgs == null || msgs.Count == 0) { _hasMoreMessages = false; return; }

            foreach (var m in msgs) _messages.Insert(0, m);
            _currentPage = nextPage;
            if (msgs.Count < 50) _hasMoreMessages = false;

            await Dispatcher.InvokeAsync(() =>
            {
                var heightAdded = MessageScroll.ScrollableHeight - prevScrollable;
                MessageScroll.ScrollToVerticalOffset(prevOffset + heightAdded);
            }, System.Windows.Threading.DispatcherPriority.Background);
        }
        finally { _loadingMessages = false; }
    }

    private void MessageScroll_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (e.VerticalOffset == 0 && e.ExtentHeight > e.ViewportHeight && !_loadingMessages && _hasMoreMessages)
            _ = LoadOlderMessages();
    }

    private async void SendButton_Click(object s, RoutedEventArgs e) => await SendMessage();

    private async void MessageInput_KeyDown(object s, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && Keyboard.Modifiers != ModifierKeys.Shift) { e.Handled = true; await SendMessage(); }
    }

    private void MessageInput_TextChanged(object s, TextChangedEventArgs e)
    {
        if (_selectedChat == null) return;
        if (string.IsNullOrEmpty(MessageInput.Text))
        {
            _typingTimer.Stop();
            _ = WebSocketService.SendTyping(_selectedChat.Id, false, _selectedChat.IsGroup);
            return;
        }
        if ((DateTime.UtcNow - _lastTypingSent).TotalSeconds > 1)
        {
            _lastTypingSent = DateTime.UtcNow;
            var chatId = _selectedChat.Id;
            var isGroup = _selectedChat.IsGroup;
            _ = WebSocketService.SendTyping(chatId, true, isGroup);
            _typingTimer.Tag = (chatId, isGroup);
            _typingTimer.Stop();
            _typingTimer.Start();
        }
    }

    private async Task SendMessage()
    {
        if (_selectedChat == null) return;
        var content = MessageInput.Text.Trim();

        if (_editingMessage != null)
        {
            if (string.IsNullOrEmpty(content)) return;
            var isGroup = _editingMessage.ChatId == 0;
            var chatId = isGroup ? (_editingMessage.GroupId ?? 0) : _editingMessage.ChatId;
            await ApiClient.EditMessage(chatId, _editingMessage.Id, content, isGroup);
            _editingMessage = null;
            MessageInput.Text = "";
            return;
        }

        if (string.IsNullOrEmpty(content)) return;
        MessageInput.Text = "";
        if (string.IsNullOrEmpty(ApiClient.Token)) return;

        Message? msg;
        if (_selectedChat.IsGroup)
            msg = await ApiClient.SendGroupMessage(_selectedChat.Id, content, replyToId: _replyToMessage?.Id);
        else
            msg = await ApiClient.SendMessage(_selectedChat.Id, content, replyToId: _replyToMessage?.Id);

        if (msg != null)
        {
            _messages.Add(msg);
            _lastMessageId = msg.Id;
            _selectedChat.LastMessage = content;
            _selectedChat.LastMessageTime = msg.SentAt;
            MessageScroll.ScrollToEnd();
        }

        _replyToMessage = null;
        ReplyBar.Visibility = Visibility.Collapsed;
    }

    private async void AttachFile_Click(object s, RoutedEventArgs e)
    {
        if (_selectedChat == null) return;
        var d = new Microsoft.Win32.OpenFileDialog { Title = "Выберите файл", Filter = "All files|*.*|Images|*.jpg;*.jpeg;*.png;*.gif;*.bmp|Videos|*.mp4;*.avi;*.mov" };
        if (d.ShowDialog() == true) await UploadAndSendFile(d.FileName);
    }

    private void MicBtn_Click(object s, RoutedEventArgs e)
    {
        if (_isRecording) StopRecording();
        else StartRecording();
    }

    private void StartRecording()
    {
        if (_selectedChat == null) return;
        try
        {
            _tempAudioFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"DARK_voice_{Guid.NewGuid():N}.wav");
            _waveIn = new WaveInEvent { WaveFormat = new WaveFormat(48000, 1) };
            _writer = new WaveFileWriter(_tempAudioFile, _waveIn.WaveFormat);
            _waveIn.DataAvailable += (_, args) => { if (_writer != null) _writer.Write(args.Buffer, 0, args.BytesRecorded); };
            _waveIn.StartRecording();
            _isRecording = true;
            MicBtn.Content = "⏹";
            MicBtn.Foreground = new SolidColorBrush(Colors.Red);
            MessageInput.IsEnabled = false;
        }
        catch
        {
            _tempAudioFile = null;
            MicBtn.Content = "🎤";
            MicBtn.Foreground = (Brush)TryFindResource("TextSecondaryBrush") ?? Brushes.Gray;
        }
    }

    private async void StopRecording()
    {
        var chat = _selectedChat;
        var file = _tempAudioFile;
        try
        {
            _waveIn?.StopRecording();
            _waveIn?.Dispose();
            _waveIn = null;
            _writer?.Dispose();
            _writer = null;
            _isRecording = false;
            MicBtn.Content = "🎤";
            MicBtn.Foreground = (Brush)TryFindResource("TextSecondaryBrush") ?? Brushes.Gray;
            MessageInput.IsEnabled = true;

            if (file == null || chat == null || !System.IO.File.Exists(file)) return;

            var result = await ApiClient.UploadFile(file);
            if (result.HasValue)
            {
                var (url, mediaType) = result.Value;
                if (url != null)
                {
                    if (chat.IsGroup)
                        await ApiClient.SendGroupMessage(chat.Id, "", url, mediaType);
                    else
                        await ApiClient.SendMessage(chat.Id, "", url, mediaType);
                    await LoadMessages(chat.Id);
                }
            }

            try { System.IO.File.Delete(file); } catch { }
        }
        catch { }
        finally { _tempAudioFile = null; }
    }

    private void EmojiBtn_Click(object s, RoutedEventArgs e)
    {
        EmojiPopup.IsOpen = !EmojiPopup.IsOpen;
    }

    private void EmojiPopup_Closed(object? s, EventArgs e)
    {
        // handled by StaysOpen=False
    }

    // === Reply ===
    private void ShowReplyBar(Message msg)
    {
        _replyToMessage = msg;
        ReplyNameText.Text = msg.IsMine ? "Вы" : (_selectedChat?.OtherUser?.DisplayName ?? "Пользователь");
        ReplyContentText.Text = msg.Content;
        ReplyBar.Visibility = Visibility.Visible;
    }

    private void CancelReply_Click(object s, RoutedEventArgs e)
    {
        _replyToMessage = null;
        ReplyBar.Visibility = Visibility.Collapsed;
    }

    // === Message actions from ChatBubble (reply, edit, delete) ===
    private async void OnMessageAction(object sender, MessageActionArgs e)
    {
        var msg = e.Message;
        switch (e.Action)
        {
            case MessageActionType.Reply:
                ShowReplyBar(msg);
                break;
            case MessageActionType.Edit:
                StartEdit(msg);
                break;
            case MessageActionType.Delete:
                var isGroup = msg.ChatId == 0;
                var chatId = isGroup ? (msg.GroupId ?? 0) : msg.ChatId;
                await ApiClient.DeleteMessage(chatId, msg.Id, isGroup);
                _messages.Remove(msg);
                break;
        }
    }

    private void StartEdit(Message msg)
    {
        _editingMessage = msg;
        _replyToMessage = null;
        ReplyBar.Visibility = Visibility.Collapsed;
        MessageInput.Text = msg.Content;
        MessageInput.Focus();
        MessageInput.CaretIndex = msg.Content.Length;
    }

    // === Message Search ===
    private void SearchChat_Click(object s, RoutedEventArgs e)
    {
        _isSearchingMessages = !_isSearchingMessages;
        ChatHeader.Visibility = _isSearchingMessages ? Visibility.Collapsed : (_selectedChat != null ? Visibility.Visible : Visibility.Collapsed);
        MsgSearchBar.Visibility = _isSearchingMessages ? Visibility.Visible : Visibility.Collapsed;
        if (_isSearchingMessages) MsgSearchBox.Focus();
    }

    private async void MsgSearchBox_TextChanged(object s, TextChangedEventArgs e)
    {
        if (_selectedChat == null) return;
        var q = MsgSearchBox.Text.Trim();
        if (string.IsNullOrEmpty(q)) { await LoadMessages(_selectedChat.Id); return; }
        var results = _selectedChat.IsGroup ? await ApiClient.SearchGroupMessages(_selectedChat.Id, q) : await ApiClient.SearchMessages(_selectedChat.Id, q);
        _messages.Clear();
        if (results != null) foreach (var m in results) _messages.Add(m);
    }

    private void CloseSearch_Click(object s, RoutedEventArgs e)
    {
        _isSearchingMessages = false;
        MsgSearchBar.Visibility = Visibility.Collapsed;
        ChatHeader.Visibility = _selectedChat != null ? Visibility.Visible : Visibility.Collapsed;
        MsgSearchBox.Text = "";
        if (_selectedChat != null) _ = LoadMessages(_selectedChat.Id);
    }

    // === Poll ===
    private async void PollTimer_Tick(object? s, EventArgs e)
    {
        if (_selectedChat == null || string.IsNullOrEmpty(ApiClient.Token) || _wsConnected)
        {
            if (_wsConnected) _pollTimer.Stop();
            return;
        }
        var currentId = _selectedChat.Id;
        var currentIsGroup = _selectedChat.IsGroup;
        var msgs = currentIsGroup ? await ApiClient.GetGroupMessages(currentId) : await ApiClient.GetMessages(currentId);
        if (msgs == null || _selectedChat?.Id != currentId) return;
        var newMsgs = msgs.Where(m => m.Id > _lastMessageId).ToList();
        foreach (var m in newMsgs) _messages.Add(m);
        if (newMsgs.Any()) { _lastMessageId = newMsgs.Max(m => m.Id); MessageScroll.ScrollToEnd(); }
    }

    // === Chat search ===
    private void ChatSearch_TextChanged(object s, TextChangedEventArgs e)
    {
        var q = ChatSearchBox.Text.Trim().ToLower();
        _chatViewSource.View.Filter = obj =>
        {
            if (obj is not Chat c) return false;
            if (string.IsNullOrEmpty(q)) return true;
            return c.Name.ToLower().Contains(q) ||
                   (c.OtherUser?.Username.ToLower().Contains(q) == true);
        };
        _chatViewSource.View.Refresh();
    }
}
