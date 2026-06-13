using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Widget;

namespace DARK_Messenger;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, Exported = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            var msg = e.ExceptionObject?.ToString() ?? "null";
            Android.Util.Log.Error("DARK_CRASH", msg);
            RunOnUiThread(() => Toast.MakeText(this, "Unhandled: " + msg.Substring(0, Math.Min(200, msg.Length)), ToastLength.Long)!.Show());
        };
        TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            var msg = e.Exception?.ToString() ?? "null";
            Android.Util.Log.Error("DARK_CRASH", msg);
            RunOnUiThread(() => Toast.MakeText(this, "Task: " + msg.Substring(0, Math.Min(200, msg.Length)), ToastLength.Long)!.Show());
        };
        AndroidEnvironment.UnhandledExceptionRaiser += (s, e) =>
        {
            var msg = e.Exception?.ToString() ?? "null";
            Android.Util.Log.Error("DARK_CRASH_NE", msg);
            RunOnUiThread(() => Toast.MakeText(this, "Native: " + msg.Substring(0, Math.Min(200, msg.Length)), ToastLength.Long)!.Show());
        };
        base.OnCreate(savedInstanceState);
    }
}
