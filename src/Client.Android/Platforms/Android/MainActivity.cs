using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Lumora.Client.Android.Sharing;

namespace Lumora.Client.Android;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTask,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode |
        ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
[IntentFilter([Intent.ActionSend], Categories = [Intent.CategoryDefault], DataMimeType = "text/plain")]
[IntentFilter([Intent.ActionSend], Categories = [Intent.CategoryDefault], DataMimeType = "image/*")]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        HandleShareIntent(Intent);
    }

    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);
        HandleShareIntent(intent);
    }

    /// <summary>
    /// Reached from the system "Udostępnij → Lumora" share sheet (text or image). Forwards the
    /// payload to ShareTargetHandler, which pushes it to the active room the same way the
    /// in-app "Wyślij schowek" button does — see plan §Share target.
    /// </summary>
    private void HandleShareIntent(Intent? intent)
    {
        if (intent?.Action != Intent.ActionSend)
        {
            return;
        }

        if (intent.Type == "text/plain")
        {
            var text = intent.GetStringExtra(Intent.ExtraText);
            if (!string.IsNullOrEmpty(text))
            {
                ShareTargetHandler.HandleSharedTextAsync(text);
            }

            return;
        }

        if (intent.Type?.StartsWith("image/") == true &&
            intent.GetParcelableExtra(Intent.ExtraStream) is global::Android.Net.Uri uri)
        {
            ShareTargetHandler.HandleSharedImageAsync(this, uri);
        }
    }
}
