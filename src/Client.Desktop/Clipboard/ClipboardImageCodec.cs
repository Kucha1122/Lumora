using System.Runtime.Versioning;
using System.Drawing;
using System.Drawing.Imaging;

namespace Lumora.Client.Desktop.Clipboard;

/// <summary>
/// Converts between the Windows clipboard's CF_DIB format (a BITMAPINFOHEADER with no file
/// header) and PNG, so image content is portable across platforms instead of tied to a
/// Windows-only bitmap layout.
/// </summary>
[SupportedOSPlatform("windows")]
internal static class ClipboardImageCodec
{
    private const int BitmapFileHeaderSize = 14;

    public static byte[] DibToPng(byte[] dib)
    {
        using var bmpStream = new MemoryStream(BitmapFileHeaderSize + dib.Length);
        using (var writer = new BinaryWriter(bmpStream, System.Text.Encoding.ASCII, leaveOpen: true))
        {
            var pixelDataOffset = BitmapFileHeaderSize + ReadHeaderSize(dib) + ReadPaletteSize(dib);

            writer.Write((byte)'B');
            writer.Write((byte)'M');
            writer.Write(BitmapFileHeaderSize + dib.Length); // file size
            writer.Write(0); // reserved
            writer.Write(pixelDataOffset);
        }

        bmpStream.Write(dib);
        bmpStream.Position = 0;

        using var bitmap = new Bitmap(bmpStream);
        using var pngStream = new MemoryStream();
        bitmap.Save(pngStream, ImageFormat.Png);
        return pngStream.ToArray();
    }

    public static byte[] PngToDib(byte[] png)
    {
        using var pngStream = new MemoryStream(png);
        using var bitmap = new Bitmap(pngStream);
        using var bmpStream = new MemoryStream();
        bitmap.Save(bmpStream, ImageFormat.Bmp);

        var bmpBytes = bmpStream.ToArray();
        return bmpBytes[BitmapFileHeaderSize..];
    }

    private static int ReadHeaderSize(byte[] dib) => BitConverter.ToInt32(dib, 0);

    private static int ReadPaletteSize(byte[] dib)
    {
        var bitCount = BitConverter.ToInt16(dib, 14);
        var colorsUsed = BitConverter.ToInt32(dib, 32);

        if (bitCount > 8)
        {
            return 0;
        }

        var paletteEntries = colorsUsed != 0 ? colorsUsed : 1 << bitCount;
        return paletteEntries * 4;
    }
}
