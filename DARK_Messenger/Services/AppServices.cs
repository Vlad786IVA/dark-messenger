namespace DARK_Messenger.Services;

public static class AppServices
{
    public static IServiceProvider? Provider { get; set; }
    public static T? Get<T>() where T : class
    {
        return Provider?.GetService<T>();
    }
}
