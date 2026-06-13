using System.Windows;
using System.Windows.Threading;

namespace DARK_Messenger_WPF.Views;

public partial class ToastWindow : Window
{
    private static readonly Queue<(string title, string body)> _queue = new();
    private static ToastWindow? _current;
    private readonly DispatcherTimer _timer;
    private bool _closing;

    public ToastWindow(string title, string body)
    {
        InitializeComponent();
        TitleText.Text = title;
        BodyText.Text = body;

        Owner = Application.Current.MainWindow;
        Left = SystemParameters.WorkArea.Width - 340;
        Top = SystemParameters.WorkArea.Height - 80;

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        _timer.Tick += (_, _) => { _timer.Stop(); Close(); };
        _timer.Start();
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        base.OnClosing(e);
        if (_closing) return;
        _closing = true;
        _current = null;
        ShowNext();
    }

    private static void ShowNext()
    {
        if (_queue.Count == 0 || _current != null) return;
        var (title, body) = _queue.Dequeue();
        _current = new ToastWindow(title, body);
        _current.Show();
    }

    public static void Show(string title, string body)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            _queue.Enqueue((title, body));
            if (_current == null) ShowNext();
        });
    }
}
