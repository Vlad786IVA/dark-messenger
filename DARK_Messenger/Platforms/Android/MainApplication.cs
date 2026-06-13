using Android.App;
using Android.Runtime;

namespace DARK_Messenger;

[Application]
public class MainApplication : MauiApplication
{
	public MainApplication(IntPtr handle, JniHandleOwnership ownership)
		: base(handle, ownership)
	{
	}

	protected override MauiApp CreateMauiApp()
	{
	    try
	    {
	        return MauiProgram.CreateMauiApp();
	    }
	    catch (System.Exception ex)
	    {
	        Android.Util.Log.Error("DARK_INIT", "CreateMauiApp failed: " + ex);
	        Android.Widget.Toast.MakeText(Android.App.Application.Context, "INIT: " + ex.Message, Android.Widget.ToastLength.Long)!.Show();
	        throw;
	    }
	}
}
