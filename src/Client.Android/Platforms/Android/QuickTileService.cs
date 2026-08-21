using Android.App;
using Android.Content;
using Android.Service.QuickSettings;

namespace Lumora.Client.Android;

/// <summary>
/// The Quick Settings tile from plan §Kafelek Szybkich ustawień: tapping it (from any app,
/// pulled down from the notification shade) opens QuickPasteActivity, a small dialog-themed
/// window listing recent clipboard entries — picking one copies it to the phone's clipboard.
/// Because opening that activity gives Lumora input focus, it's also the one moment outside
/// the main app where reading the phone's own clipboard (for "Wyślij mój schowek") is allowed
/// by Android 10+'s background-clipboard-access restriction.
/// </summary>
[Service(Label = "Lumora", Icon = "@mipmap/appicon", Permission = "android.permission.BIND_QUICK_SETTINGS_TILE", Exported = true)]
[IntentFilter(["android.service.quicksettings.action.QS_TILE"])]
public sealed class QuickTileService : TileService
{
    public override void OnClick()
    {
        base.OnClick();

        var intent = new Intent(this, typeof(QuickPasteActivity));
        intent.AddFlags(ActivityFlags.NewTask);

        if (OperatingSystem.IsAndroidVersionAtLeast(34))
        {
            var pendingIntent = PendingIntent.GetActivity(
                this, 0, intent, PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent);
            StartActivityAndCollapse(pendingIntent!);
        }
        else
        {
#pragma warning disable CA1422 // deprecated pre-API 34 fallback, intentional
            StartActivityAndCollapse(intent);
#pragma warning restore CA1422
        }
    }
}
