using Android.Content;
using AndroidUri = Android.Net.Uri;
using FileProvider = AndroidX.Core.Content.FileProvider;

namespace Lumora.Client.Android.Clipboard;

/// <summary>
/// Android's ClipData carries images as a content:// Uri, not raw bytes, so pushing/receiving
/// a clipboard image means round-tripping through a cache file exposed via FileProvider
/// (declared in AndroidManifest.xml + Resources/xml/file_paths.xml).
/// </summary>
internal static class ClipboardImageFile
{
    private const string AuthoritySuffix = ".fileprovider";

    public static AndroidUri WriteToCache(Context context, byte[] pngBytes)
    {
        var dir = new Java.IO.File(context.CacheDir, "clipboard-images");
        dir.Mkdirs();
        var file = new Java.IO.File(dir, $"{Guid.NewGuid():N}.png");
        File.WriteAllBytes(file.AbsolutePath, pngBytes);

        return FileProvider.GetUriForFile(context, context.PackageName + AuthoritySuffix, file)!;
    }

    public static byte[]? ReadFromUri(Context context, AndroidUri uri)
    {
        using var stream = context.ContentResolver?.OpenInputStream(uri);
        if (stream is null)
        {
            return null;
        }

        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }
}
