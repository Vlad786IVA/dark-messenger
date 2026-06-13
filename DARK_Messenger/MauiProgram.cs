using Microsoft.Extensions.Logging;
using DARK_Messenger.Services;
using DARK_Messenger.ViewModels;
using DARK_Messenger.Views;

namespace DARK_Messenger;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        builder.Services.AddSingleton<ApiClient>();
        builder.Services.AddSingleton<SocketService>();
        builder.Services.AddSingleton<SettingsService>();
        builder.Services.AddSingleton<ChatService>();

        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<RegisterViewModel>();
        builder.Services.AddTransient<ChatListViewModel>();
        builder.Services.AddTransient<ChatViewModel>();

        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<RegisterPage>();
        builder.Services.AddTransient<ChatListPage>();
        builder.Services.AddTransient<ChatPage>();
        builder.Services.AddTransient<ContactsPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        var app = builder.Build();
        AppServices.Provider = app.Services;
        return app;
    }
}
