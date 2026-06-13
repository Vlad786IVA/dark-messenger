using DARK_Messenger.ViewModels;

namespace DARK_Messenger.Views;

public partial class LoginPage : ContentPage
{
    private LoginViewModel? _vm;

    public LoginPage()
    {
        InitializeComponent();
        _vm = new LoginViewModel();
        BindingContext = _vm;
        _vm.OnLoginSuccess += async () => await Shell.Current.GoToAsync("//main");
        _vm.OnGoToRegister += async () => await Shell.Current.GoToAsync("//register");
        _vm.OnError += msg =>
        {
            StatusText.Text = msg;
            StatusBorder.IsVisible = true;
        };
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        StatusBorder.IsVisible = false;
        LoginPanel.IsVisible = true;
    }

    private void OnLoginClicked(object sender, EventArgs e)
    {
        if (_vm != null) _ = _vm.LoginAsync();
    }

    private void OnRegisterClicked(object sender, EventArgs e)
    {
        _vm?.GoToRegister();
    }

    private void TogglePassword_Click(object sender, EventArgs e)
    {
        _vm?.TogglePassword();
    }
}
