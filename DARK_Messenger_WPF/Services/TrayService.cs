using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Hardcodet.Wpf.TaskbarNotification;

namespace DARK_Messenger_WPF.Services;

public class TrayService : IDisposable
{
    private readonly TaskbarIcon _notifyIcon;
    private readonly Window _window;
    private bool _disposed;

    public TrayService(Window window)
    {
        _window = window;

        _notifyIcon = new TaskbarIcon();
        var path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appicon.ico");
        if (System.IO.File.Exists(path))
            _notifyIcon.IconSource = new BitmapImage(new Uri(path));

        _notifyIcon.ToolTipText = "DARK Messenger";

        var menu = new ContextMenu();
        var openItem = new MenuItem { Header = "Открыть" };
        openItem.Click += (_, _) => Restore();
        menu.Items.Add(openItem);
        menu.Items.Add(new Separator());
        var exitItem = new MenuItem { Header = "Выход" };
        exitItem.Click += (_, _) => Exit();
        menu.Items.Add(exitItem);
        _notifyIcon.ContextMenu = menu;

        _notifyIcon.TrayMouseDoubleClick += (_, _) => Restore();
    }

    public void Show()
    {
        _notifyIcon.Visibility = Visibility.Visible;
    }

    public void Hide()
    {
        _notifyIcon.Visibility = Visibility.Collapsed;
    }

    public void ShowNotification(string title, string text)
    {
        _notifyIcon.ShowBalloonTip(title, text, BalloonIcon.Info);
    }

    private void Restore()
    {
        _window.Show();
        _window.WindowState = WindowState.Normal;
        _window.Activate();
    }

    private void Exit()
    {
        _window.Tag = "forceClose";
        Application.Current.Shutdown();
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _notifyIcon.Dispose();
        }
    }
}
