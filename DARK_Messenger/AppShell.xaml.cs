using Microsoft.Maui.Controls;
using DARK_Messenger.Views;

namespace DARK_Messenger;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute("chat", typeof(ChatPage));
    }
}
