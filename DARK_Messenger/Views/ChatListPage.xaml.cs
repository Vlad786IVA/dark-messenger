using DARK_Messenger.ViewModels;
using DARK_Messenger.Models;

namespace DARK_Messenger.Views;

public partial class ChatListPage : ContentPage
{
    public ChatListPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is ChatListViewModel vm)
        {
            vm.OnOpenChat += async (chat) =>
            {
                var chatPage = new ChatPage();
                chatPage.SetChatId(chat.Id);
                await Navigation.PushAsync(chatPage);
            };
            vm.OnNewChat += async () => await Shell.Current.GoToAsync("//main/contacts");
            vm.OnLogout += async () => await Shell.Current.GoToAsync("//auth/login");
            await vm.LoadChatsAsync();
        }
    }

    private void OnNewChatClicked(object sender, EventArgs e)
    {
        if (BindingContext is ChatListViewModel vm)
            vm.NewChatCommand.Execute(null);
    }
}
