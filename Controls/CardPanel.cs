using VideoDownloader.WinForms.Services;

namespace VideoDownloader.WinForms.Controls;

/// <summary>A rounded, bordered container used for every panel of content.</summary>
public class CardPanel : Panel
{
    private ThemeColors _palette = ThemeColors.Light;

    public CardPanel()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.UserPaint |
            ControlStyles.ResizeRedraw, true);

        BackColor = _palette.Background;
        Padding = new Padding(20);
    }

    public int CornerRadius { get; set; } = 14;

    public bool ShowShadow { get; set; }

    public Color? FillOverride { get; set; }

    public Color? BorderOverride { get; set; }

    public ThemeColors Palette => _palette;

    public virtual void ApplyPalette(ThemeColors palette)
    {
        _palette = palette;
        BackColor = palette.Background;
        OnPaletteChanged();
        Invalidate();
    }

    /// <summary>Hook for subclasses that own child controls needing re-coloring.</summary>
    protected virtual void OnPaletteChanged()
    {
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(BackColor);
        Ui.Prepare(e.Graphics);

        var bounds = new Rectangle(2, 2, Width - 5, Height - 5);
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        Ui.Card(e.Graphics, bounds, _palette, CornerRadius, FillOverride, BorderOverride, ShowShadow);
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        // Painted in OnPaint so the rounded corners reveal the host color.
    }
}
