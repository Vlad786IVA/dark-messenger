using DARK_Messenger.ViewModels;

namespace DARK_Messenger.Views;

public partial class RegisterPage : ContentPage
{
    private RegisterViewModel? _vm;

    public RegisterPage()
    {
        InitializeComponent();
        _vm = new RegisterViewModel();
        BindingContext = _vm;
        _vm.OnRegisterSuccess += async () => await Shell.Current.GoToAsync("//main");
        _vm.OnGoToLogin += async () => await Shell.Current.GoToAsync("//login");
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
    }

    private void OnRegisterClicked(object sender, EventArgs e)
    {
        if (_vm != null) _ = _vm.RegisterAsync();
    }

    private void OnLoginClicked(object sender, EventArgs e)
    {
        _vm?.GoToLogin();
    }

    private void TogglePassword_Click(object sender, EventArgs e)
    {
        _vm?.TogglePassword();
    }
}
