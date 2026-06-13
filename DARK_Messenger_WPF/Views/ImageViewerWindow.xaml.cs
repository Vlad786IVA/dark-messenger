using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace DARK_Messenger_WPF.Views;

public partial class ImageViewerWindow : Window
{
    public ImageViewerWindow(string imageUrl)
    {
        InitializeComponent();
        Loaded += (_, _) => _ = LoadImageAsync(imageUrl);
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
            ViewerImage.Source = bmp;
        }
        catch { Dispatcher.Invoke(Close); }
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape) Close();
    }
}
