using System.Drawing.Drawing2D;
using System.Drawing.Text;
using VideoDownloader.WinForms.Services;

namespace VideoDownloader.WinForms.Controls;

/// <summary>Shared drawing primitives and typography for the custom-drawn UI.</summary>
internal static class Ui
{
    public const string FontFamily = "Segoe UI";
    public const string IconFamily = "Segoe MDL2 Assets";

    public static readonly Font Display = new(FontFamily, 20f, FontStyle.Bold);
    public static readonly Font Heading = new(FontFamily, 12.5f, FontStyle.Bold);
    public static readonly Font Subheading = new(FontFamily, 10.5f, FontStyle.Bold);
    public static readonly Font Body = new(FontFamily, 9.75f);
    public static readonly Font BodyStrong = new(FontFamily, 9.75f, FontStyle.Bold);
    public static readonly Font Caption = new(FontFamily, 8.75f);
    public static readonly Font CaptionStrong = new(FontFamily, 8.75f, FontStyle.Bold);
    public static readonly Font Metric = new(FontFamily, 23f, FontStyle.Bold);
    public static readonly Font Icon = new(IconFamily, 12f);
    public static readonly Font IconSmall = new(IconFamily, 9f);
    public static readonly Font IconLarge = new(IconFamily, 16f);

    public static readonly Color Transparent = Color.Transparent;

    public static readonly TextFormatFlags Left = TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine;
    public static readonly TextFormatFlags LeftTop = TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.NoPadding;
    public static readonly TextFormatFlags Center = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine;
    public static readonly TextFormatFlags Right = TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine;

    public static GraphicsPath Rounded(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();

        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return path;
        }

        var d = Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height));
        if (d <= 0)
        {
            path.AddRectangle(bounds);
            return path;
        }

        path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
        path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
        path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    public static void Prepare(Graphics g)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
    }

    public static void Fill(Graphics g, Rectangle bounds, int radius, Color color)
    {
        using var path = Rounded(bounds, radius);
        using var brush = new SolidBrush(color);
        g.FillPath(brush, path);
    }

    public static void Stroke(Graphics g, Rectangle bounds, int radius, Color color, float width = 1f)
    {
        using var path = Rounded(bounds, radius);
        using var pen = new Pen(color, width);
        g.DrawPath(pen, path);
    }

    /// <summary>Paints a rounded card with a soft drop shadow into the given bounds.</summary>
    public static void Card(Graphics g, Rectangle bounds, ThemeColors palette, int radius, Color? fill = null, Color? border = null, bool shadow = true)
    {
        if (shadow && bounds.Width > 8 && bounds.Height > 8)
        {
            var shadowBounds = new Rectangle(bounds.X + 1, bounds.Y + 2, bounds.Width - 2, bounds.Height - 2);
            Fill(g, shadowBounds, radius, palette.Shadow);
        }

        Fill(g, bounds, radius, fill ?? palette.Card);
        Stroke(g, bounds, radius - 1 >= 0 ? radius : 0, border ?? palette.CardBorder);
    }

    public static Color Mix(Color from, Color to, double amount)
    {
        amount = Math.Clamp(amount, 0, 1);
        return Color.FromArgb(
            (int)Math.Round(from.R + ((to.R - from.R) * amount)),
            (int)Math.Round(from.G + ((to.G - from.G) * amount)),
            (int)Math.Round(from.B + ((to.B - from.B) * amount)));
    }

    public static Color Alpha(Color color, int alpha) => Color.FromArgb(Math.Clamp(alpha, 0, 255), color);

    public static void Text(Graphics g, string text, Font font, Color color, Rectangle bounds, TextFormatFlags flags)
        => TextRenderer.DrawText(g, text, font, bounds, color, flags);

    public static void SingleLine(Graphics g, string text, Font font, Color color, Rectangle bounds, TextFormatFlags flags)
    {
        var oneLine = text.Replace("\r", " ").Replace("\n", " ");
        Text(g, oneLine, font, color, bounds, flags | TextFormatFlags.EndEllipsis);
    }

    public static void Glyph(Graphics g, string glyph, Font font, Color color, Rectangle bounds, TextFormatFlags flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding)
        => TextRenderer.DrawText(g, glyph, font, bounds, color, flags);

    public static void SetHostColor(Control control, Color color)
    {
        control.BackColor = color;
    }
}
