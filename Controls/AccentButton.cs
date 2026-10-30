using VideoDownloader.WinForms.Services;

namespace VideoDownloader.WinForms.Controls;

public enum ButtonVariant
{
    Primary,
    Secondary,
    Ghost,
    Danger
}

/// <summary>A rounded, flat button with hover/pressed states and an optional leading glyph.</summary>
public sealed class AccentButton : Button
{
    private ThemeColors _palette = ThemeColors.Light;
    private bool _hover;
    private bool _pressed;

    public AccentButton()
    {
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.SupportsTransparentBackColor, true);

        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        UseVisualStyleBackColor = false;
        Font = Ui.BodyStrong;
        Height = 38;
        Cursor = Cursors.Hand;
        TabStop = true;
    }

    public ButtonVariant Variant { get; set; } = ButtonVariant.Primary;

    public int CornerRadius { get; set; } = 9;

    public string Glyph { get; set; } = string.Empty;

    public void ApplyPalette(ThemeColors palette)
    {
        _palette = palette;
        BackColor = Parent?.BackColor ?? palette.Background;
        ForeColor = palette.TextPrimary;
        Invalidate();
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        _hover = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hover = false;
        _pressed = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnMouseDown(MouseEventArgs mevent)
    {
        _pressed = true;
        Invalidate();
        base.OnMouseDown(mevent);
    }

    protected override void OnMouseUp(MouseEventArgs mevent)
    {
        _pressed = false;
        Invalidate();
        base.OnMouseUp(mevent);
    }

    protected override void OnEnabledChanged(EventArgs e)
    {
        Invalidate();
        base.OnEnabledChanged(e);
    }

    protected override void OnPaint(PaintEventArgs pevent)
    {
        var host = Parent?.BackColor ?? _palette.Background;
        pevent.Graphics.Clear(host);
        Ui.Prepare(pevent.Graphics);

        var bounds = new Rectangle(0, 0, Width - 1, Height - 1);

        Color fill;
        Color border;
        Color content;

        switch (Variant)
        {
            case ButtonVariant.Secondary:
                fill = _pressed ? Ui.Mix(_palette.Card, _palette.TextPrimary, 0.10)
                    : _hover ? _palette.CardHover : _palette.Card;
                border = _palette.CardBorder;
                content = _palette.TextPrimary;
                break;

            case ButtonVariant.Ghost:
                fill = _hover ? _palette.Chip : host;
                border = Color.Transparent;
                content = _hover ? _palette.TextPrimary : _palette.TextSecondary;
                break;

            case ButtonVariant.Danger:
                fill = _pressed ? Ui.Mix(_palette.DangerSoft, _palette.Danger, 0.18)
                    : _hover ? Ui.Mix(_palette.DangerSoft, _palette.Danger, 0.10) : _palette.DangerSoft;
                border = Ui.Alpha(_palette.Danger, 70);
                content = _palette.Danger;
                break;

            default:
                fill = _pressed ? _palette.AccentPressed : _hover ? _palette.AccentHover : _palette.Accent;
                border = Color.Transparent;
                content = _palette.OnAccent;
                break;
        }

        if (!Enabled)
        {
            fill = Ui.Mix(fill, _palette.Card, 0.55);
            content = _palette.TextMuted;
            border = Variant == ButtonVariant.Primary ? Color.Transparent : _palette.CardBorder;
        }

        if (fill.ToArgb() != host.ToArgb())
        {
            Ui.Fill(pevent.Graphics, bounds, CornerRadius, fill);
        }

        if (border.A != 0)
        {
            Ui.Stroke(pevent.Graphics, bounds, CornerRadius, border);
        }

        if (Focused && Enabled)
        {
            Ui.Stroke(pevent.Graphics, bounds, CornerRadius, Ui.Alpha(_palette.Accent, 120), 1.5f);
        }

        var glyphWidth = string.IsNullOrEmpty(Glyph) ? 0 : 18;
        var textSize = string.IsNullOrEmpty(Text)
            ? Size.Empty
            : TextRenderer.MeasureText(Text, Font, Size.Empty, TextFormatFlags.NoPadding);
        var gap = glyphWidth > 0 && textSize.Width > 0 ? 8 : 0;
        var totalWidth = glyphWidth + gap + textSize.Width;
        var x = Math.Max(6, (Width - totalWidth) / 2);

        if (glyphWidth > 0)
        {
            Ui.Glyph(pevent.Graphics, Glyph, Ui.IconSmall, content, new Rectangle(x, 0, glyphWidth, Height));
            x += glyphWidth + gap;
        }

        if (textSize.Width > 0)
        {
            Ui.Text(pevent.Graphics, Text, Font, content, new Rectangle(x, 0, textSize.Width + 2, Height), Ui.Left);
        }
    }
}
