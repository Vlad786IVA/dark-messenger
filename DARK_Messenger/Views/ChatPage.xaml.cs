using DARK_Messenger.ViewModels;

namespace DARK_Messenger.Views;

public partial class ChatPage : ContentPage
{
    private string? _chatId;

    public ChatPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is ChatViewModel vm)
        {
            vm.OnBack += async () => await Shell.Current.GoToAsync("//main/chatlist");
            if (_chatId != null)
                await vm.LoadChatAsync(_chatId);
        }
    }

    public void SetChatId(string chatId)
    {
        _chatId = chatId;
    }

    private void OnEntryCompleted(object? sender, EventArgs e)
    {
        if (BindingContext is ChatViewModel vm)
            vm.SendMessageCommand.Execute(null);
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//main/chatlist");
    }
}
