using System.Net.Http.Json;
using Android.Content;
using Android.Content.PM;
using AndroidX.Core.Content.PM;
using Lumora.Contracts.Updates;
using FileProvider = AndroidX.Core.Content.FileProvider;

namespace Lumora.Client.Android.Updates;

public sealed record UpdateCheckResult(bool IsAvailable, AndroidReleaseDto? Release);

/// <summary>
/// Etap 2 updater (plan §Dystrybucja): checks the server's /updates/android/latest, and if
/// its VersionCode is newer than this install's own, downloads and hands the APK to the
/// system installer. No silent auto-update — the user always sees and confirms the install
/// prompt, same as any APK installed outside Google Play.
/// </summary>
public sealed class UpdateService(HttpClient http)
{
    public async Task<UpdateCheckResult> CheckAsync(CancellationToken ct)
    {
        HttpResponseMessage response;
        try
        {
            response = await http.GetAsync("updates/android/latest", ct);
        }
        catch (HttpRequestException)
        {
            return new UpdateCheckResult(false, null);
        }

        if (!response.IsSuccessStatusCode)
        {
            return new UpdateCheckResult(false, null);
        }

        var release = await response.Content.ReadFromJsonAsync<AndroidReleaseDto>(ct);
        if (release is null || release.VersionCode <= GetInstalledVersionCode())
        {
            return new UpdateCheckResult(false, null);
        }

        return new UpdateCheckResult(true, release);
    }

    /// <summary>Downloads the APK to cache and hands it to the system package installer via
    /// ACTION_VIEW + FileProvider — REQUEST_INSTALL_PACKAGES is declared in AndroidManifest.xml.
    /// The OS shows its own confirmation screen; this call never installs anything silently.</summary>
    public async Task DownloadAndInstallAsync(Context context, CancellationToken ct)
    {
        var bytes = await http.GetByteArrayAsync("updates/android/latest/apk", ct);

        var dir = new Java.IO.File(context.CacheDir, "updates");
        dir.Mkdirs();
        var file = new Java.IO.File(dir, "lumora-update.apk");
        await File.WriteAllBytesAsync(file.AbsolutePath, bytes, ct);

        var uri = FileProvider.GetUriForFile(context, context.PackageName + ".fileprovider", file)!;

        var intent = new Intent(Intent.ActionView);
        intent.SetDataAndType(uri, "application/vnd.android.package-archive");
        intent.AddFlags(ActivityFlags.NewTask | ActivityFlags.GrantReadUriPermission);
        context.StartActivity(intent);
    }

    private static int GetInstalledVersionCode()
    {
        var context = global::Android.App.Application.Context;
        var info = context.PackageManager!.GetPackageInfo(context.PackageName!, PackageInfoFlags.MatchAll)!;
        return (int)PackageInfoCompat.GetLongVersionCode(info);
    }
}
