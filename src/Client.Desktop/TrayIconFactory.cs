using System.Drawing;
using System.Drawing.Drawing2D;
using Avalonia.Controls;
using AvaloniaBitmap = Avalonia.Media.Imaging.Bitmap;

namespace Lumora.Client.Desktop;

/// <summary>
/// Generates the two tray glyphs at runtime instead of shipping .ico assets — a filled
/// circle is enough to make public vs. private visually distinct at a glance, which is the
/// one thing that must never be ambiguous (see plan §Wybór przestrzeni: "Pomyłka co do tego,
/// gdzie właśnie leci schowek, jest tu najkosztowniejszym błędem UX").
/// </summary>
public static class TrayIconFactory
{
    public static WindowIcon CreatePublicIcon() => CreateIcon(Color.FromArgb(46, 160, 67), drawLock: false);

    public static WindowIcon CreatePrivateIcon() => CreateIcon(Color.FromArgb(216, 108, 27), drawLock: true);

    private static WindowIcon CreateIcon(Color fill, bool drawLock)
    {
        const int size = 32;
        using var bitmap = new Bitmap(size, size);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            using var brush = new SolidBrush(fill);
            g.FillEllipse(brush, 2, 2, size - 4, size - 4);

            if (drawLock)
            {
                using var lockBrush = new SolidBrush(Color.White);
                g.FillRectangle(lockBrush, 12, 15, 8, 8);
                using var pen = new Pen(Color.White, 2);
                g.DrawArc(pen, 13, 9, 6, 8, 180, 180);
            }
        }

        using var stream = new MemoryStream();
        bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
        stream.Position = 0;
        return new WindowIcon(new AvaloniaBitmap(stream));
    }
}
