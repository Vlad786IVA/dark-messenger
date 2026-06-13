using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace DARK_Launcher;

public partial class MainWindow : Window
{
    private Process? _serverProcess;
    private bool _isRunning;

    private static readonly string ServerDir = Path.GetFullPath(Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "DARK_Server"));

    private static string? FindNode()
    {
        foreach (var dir in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator))
        {
            try
            {
                var full = Path.Combine(dir.Trim('"'), "node.exe");
                if (File.Exists(full)) return full;
                full = Path.Combine(dir.Trim('"'), "node.cmd");
                if (File.Exists(full)) return full;
            }
            catch { }
        }
        try { if (File.Exists("node.exe")) return "node.exe"; } catch { }
        return null;
    }

    public MainWindow()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            Log("Лаунчер запущен");
            Log($"Папка сервера: {ServerDir}");
            Log($"Папка существует: {Directory.Exists(ServerDir)}");

            var node = FindNode();
            Log($"Node.js: {node ?? "НЕ НАЙДЕН!"}");

            if (!Directory.Exists(ServerDir))
                Log("ОШИБКА: папка сервера не найдена!");

            ShowNetworkInfo();
        };
    }

    private void ShowNetworkInfo()
    {
        LocalUrl.Text = "http://localhost:8080";

        var ips = NetworkInterface.GetAllNetworkInterfaces()
            .Where(n => n.OperationalStatus == OperationalStatus.Up)
            .SelectMany(n => n.GetIPProperties().UnicastAddresses)
            .Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(a.Address))
            .Select(a => a.Address.ToString())
            .ToList();

        NetworkUrl.Text = ips.Count > 0
            ? string.Join(", ", ips.Select(ip => $"http://{ip}:8080"))
            : "—";
    }

    private async void ToggleBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_isRunning) await StopServer();
        else await StartServer();
    }

    private async Task StartServer()
    {
        var resolvedDir = Path.GetFullPath(ServerDir);
        if (!Directory.Exists(resolvedDir))
        {
            var msg = $"Папка сервера не найдена:\n{resolvedDir}\n\nОжидалось найти DARK_Server рядом с DARK_Launcher.";
            Log("Ошибка: " + msg);
            MessageBox.Show(msg, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        var nodePath = FindNode();
        if (nodePath == null)
        {
            var msg = "Node.js не найден.\nУстановите Node.js (https://nodejs.org) или проверьте PATH.";
            Log("Ошибка: " + msg);
            MessageBox.Show(msg, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        Log($"Node.js: {nodePath}");

        if (!Directory.Exists(Path.Combine(resolvedDir, "node_modules")))
        {
            Log("node_modules не найдены, запускаю npm install...");
            using var npm = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd",
                    Arguments = $"/c cd /d \"{resolvedDir}\" && npm install",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8,
                }
            };
            npm.Start();
            while (!npm.StandardOutput.EndOfStream)
                Log(await npm.StandardOutput.ReadLineAsync() ?? "");
            while (!npm.StandardError.EndOfStream)
                Log("[ERR] " + (await npm.StandardError.ReadLineAsync() ?? ""));
            await npm.WaitForExitAsync();
        }

        _serverProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = nodePath,
                Arguments = "server.js",
                WorkingDirectory = ServerDir,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
            },
            EnableRaisingEvents = true,
        };

        _serverProcess.OutputDataReceived += (_, args) =>
        {
            if (!string.IsNullOrEmpty(args.Data))
                Dispatcher.Invoke(() => Log(args.Data));
        };
        _serverProcess.ErrorDataReceived += (_, args) =>
        {
            if (!string.IsNullOrEmpty(args.Data))
                Dispatcher.Invoke(() => Log("[ERR] " + args.Data));
        };
        _serverProcess.Exited += (_, _) =>
        {
            _isRunning = false;
            Dispatcher.Invoke(() =>
            {
                StatusDot.Fill = (Brush)FindResource("RedBrush");
                StatusText.Text = "Остановлен";
                StatusText.Foreground = (Brush)FindResource("TextSecondaryBrush");
                ToggleBtn.Content = "▶ Старт";
                ToggleBtn.Background = (Brush)FindResource("AccentBrush");
                Log("Сервер остановлен");
            });
        };

        try
        {
            _serverProcess.Start();
            _serverProcess.BeginOutputReadLine();
            _serverProcess.BeginErrorReadLine();
            _isRunning = true;

            StatusDot.Fill = (Brush)FindResource("GreenBrush");
            StatusText.Text = "Запущен";
            StatusText.Foreground = (Brush)FindResource("GreenBrush");
            ToggleBtn.Content = "■ Стоп";
            ToggleBtn.Background = (Brush)FindResource("RedBrush");
            Log("Сервер запускается...");

            await Task.Delay(1000);
            if (_serverProcess.HasExited)
                Log("Внимание: сервер завершился сразу после запуска");
        }
        catch (Exception ex)
        {
            Log("Ошибка запуска: " + ex.Message);
            _serverProcess?.Dispose();
            _serverProcess = null;
        }
    }

    private async Task StopServer()
    {
        if (_serverProcess == null || _serverProcess.HasExited) return;

        try
        {
            Log("Остановка сервера...");
            _serverProcess.Kill(entireProcessTree: true);
            await _serverProcess.WaitForExitAsync();
        }
        catch { }

        _serverProcess?.Dispose();
        _serverProcess = null;
        _isRunning = false;

        StatusDot.Fill = (Brush)FindResource("RedBrush");
        StatusText.Text = "Остановлен";
        StatusText.Foreground = (Brush)FindResource("TextSecondaryBrush");
        ToggleBtn.Content = "▶ Старт";
        ToggleBtn.Background = (Brush)FindResource("AccentBrush");
    }

    private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_serverProcess != null && !_serverProcess.HasExited)
        {
            _serverProcess.OutputDataReceived -= (_, _) => { };
            _serverProcess.ErrorDataReceived -= (_, _) => { };
            _serverProcess.Kill(entireProcessTree: true);
            _serverProcess.WaitForExit(3000);
            _serverProcess.Dispose();
            _serverProcess = null;
        }
    }

    private void Log(string message)
    {
        var line = $"[{DateTime.Now:HH:mm:ss}] {message}";
        LogText.AppendText((LogText.Text.Length > 0 ? "\n" : "") + line);
        LogText.ScrollToEnd();
        if (LogText.Text.Length > 10000)
        {
            LogText.Text = LogText.Text[^5000..];
            LogText.ScrollToEnd();
        }
    }
}
