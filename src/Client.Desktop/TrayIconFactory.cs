using System.Drawing;
using System.Drawing.Drawing2D;
using Avalonia.Controls;
using AvaloniaBitmap = Avalonia.Media.Imaging.Bitmap;

namespace Lumora.Client.Desktop;

/// <summary>
/// Generates the "Eclipse" brand glyph at runtime instead of shipping .ico assets — two
/// overlapping circles (your clipboard and the room's) is deliberately the whole mark: bold
/// enough to survive a 16px tray icon, and public vs. private is carried entirely by fill
/// color so there's nothing finer to lose at that size. See plan §Wybór przestrzeni: a mix-up
/// about which room the clipboard is currently syncing to is the costliest UX mistake here.
/// </summary>
public static class TrayIconFactory
{
    public static readonly Color PublicColor = Color.FromArgb(0x7C, 0x6C, 0xF0);
    public static readonly Color PrivateColor = Color.FromArgb(0xC9, 0x7F, 0x32);

    private static readonly Lazy<WindowIcon> BrandIconLazy = new(() => CreateEclipseIcon(PublicColor));

    /// <summary>The one static brand mark used for window title-bar/taskbar icons — those
    /// don't track the active room the way the tray icon does, so they stay a fixed color.</summary>
    public static WindowIcon BrandIcon => BrandIconLazy.Value;

    public static WindowIcon CreatePublicIcon() => CreateEclipseIcon(PublicColor);

    public static WindowIcon CreatePrivateIcon() => CreateEclipseIcon(PrivateColor);

    private static WindowIcon CreateEclipseIcon(Color fill)
    {
        using var bitmap = DrawEclipse(size: 32, fill, background: null);
        using var stream = new MemoryStream();
        bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
        stream.Position = 0;
        return new WindowIcon(new AvaloniaBitmap(stream));
    }

    /// <summary>
    /// Draws the two-circle mark. With <paramref name="background"/> null the circles are
    /// filled with <paramref name="fill"/> directly on a transparent canvas (tray icon, window
    /// icon). With a background color, it draws the app-tile version instead — a rounded-square
    /// gradient tile behind white circles, matching the exe/taskbar icon and Android launcher.
    /// </summary>
    internal static Bitmap DrawEclipse(int size, Color fill, (Color From, Color To)? background)
    {
        var bitmap = new Bitmap(size, size);
        using var g = Graphics.FromImage(bitmap);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);

        var leftFill = fill;
        var rightFill = Color.FromArgb(140, fill.R, fill.G, fill.B);

        if (background is { } tile)
        {
            var cornerRadius = size * 0.22f;
            using var tileBrush = new LinearGradientBrush(
                new RectangleF(0, 0, size, size), tile.From, tile.To, LinearGradientMode.ForwardDiagonal);
            using var tilePath = RoundedRect(0, 0, size, size, cornerRadius);
            g.FillPath(tileBrush, tilePath);

            leftFill = Color.White;
            rightFill = Color.FromArgb(140, 255, 255, 255);
        }

        // Circle geometry is expressed as fractions of `size` so it scales identically from a
        // 16px tray glyph up to a 512px Android launcher icon.
        var radius = size * 0.215f;
        var leftCenter = new PointF(size * 0.375f, size * 0.5f);
        var rightCenter = new PointF(size * 0.625f, size * 0.5f);

        using (var rightBrush = new SolidBrush(rightFill))
        {
            g.FillEllipse(rightBrush, rightCenter.X - radius, rightCenter.Y - radius, radius * 2, radius * 2);
        }

        using (var leftBrush = new SolidBrush(leftFill))
        {
            g.FillEllipse(leftBrush, leftCenter.X - radius, leftCenter.Y - radius, radius * 2, radius * 2);
        }

        return bitmap;
    }

    private static GraphicsPath RoundedRect(float x, float y, float width, float height, float radius)
    {
        var path = new GraphicsPath();
        var diameter = radius * 2;
        path.AddArc(x, y, diameter, diameter, 180, 90);
        path.AddArc(x + width - diameter, y, diameter, diameter, 270, 90);
        path.AddArc(x + width - diameter, y + height - diameter, diameter, diameter, 0, 90);
        path.AddArc(x, y + height - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}
