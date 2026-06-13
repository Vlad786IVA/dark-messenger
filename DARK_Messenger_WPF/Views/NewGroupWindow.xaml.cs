using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using DARK_Messenger_WPF.Models;
using DARK_Messenger_WPF.Services;

namespace DARK_Messenger_WPF.Views;

public partial class NewGroupWindow : Window
{
    public int? CreatedGroupId { get; private set; }
    private readonly ObservableCollection<SelectableContact> _users = new();
    private readonly DispatcherTimer _searchTimer;

    public class SelectableContact : Contact
    {
        public bool IsSelected { get; set; }
    }

    public NewGroupWindow()
    {
        InitializeComponent();
        UserList.ItemsSource = _users;
        _searchTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        _searchTimer.Tick += async (_, _) =>
        {
            _searchTimer.Stop();
            var q = SearchUserBox.Text.Trim();
            if (string.IsNullOrEmpty(q)) { _users.Clear(); return; }
            var users = await ApiClient.SearchUsers(q);
            _users.Clear();
            foreach (var u in users) _users.Add(new SelectableContact { UserId = u.UserId, Username = u.Username, DisplayName = u.DisplayName });
        };
    }

    private void Drag_MouseDown(object sender, MouseButtonEventArgs e) => DragMove();
    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void SearchUserBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _searchTimer.Stop();
        _searchTimer.Start();
    }

    private async void Create_Click(object sender, RoutedEventArgs e)
    {
        var name = GroupNameBox.Text.Trim();
        if (string.IsNullOrEmpty(name)) { ShowStatus("Введите название"); return; }
        var members = _users.Where(u => u.IsSelected).Select(u => u.UserId).ToList();
        if (!members.Any()) { ShowStatus("Выберите участников"); return; }

        var id = await ApiClient.CreateGroup(name, members);
        if (id != null) { CreatedGroupId = id; DialogResult = true; Close(); }
        else ShowStatus("Ошибка создания");
    }

    private void ShowStatus(string msg) { StatusText.Text = msg; StatusText.Visibility = Visibility.Visible; }
}
