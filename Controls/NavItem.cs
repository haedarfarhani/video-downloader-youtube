using VideoDownloader.WinForms.Services;

namespace VideoDownloader.WinForms.Controls;

/// <summary>A single sidebar destination: glyph, label, hover state and active indicator.</summary>
public sealed class NavItem : Control
{
    private ThemeColors _palette = ThemeColors.Light;
    private bool _hover;
    private bool _active;

    public NavItem()
    {
        SetStyle(
            ControlStyles.Selectable |
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.SupportsTransparentBackColor, true);

        Height = 44;
        Cursor = Cursors.Hand;
        Font = Ui.BodyStrong;
        TabStop = true;
    }

    public string Glyph { get; set; } = string.Empty;

    public bool IsActive
    {
        get => _active;
        set
        {
            if (_active == value)
            {
                return;
            }

            _active = value;
            Invalidate();
        }
    }

    public void ApplyPalette(ThemeColors palette)
    {
        _palette = palette;
        BackColor = palette.Sidebar;
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
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnGotFocus(EventArgs e)
    {
        Invalidate();
        base.OnGotFocus(e);
    }

    protected override void OnLostFocus(EventArgs e)
    {
        Invalidate();
        base.OnLostFocus(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode is Keys.Enter or Keys.Space)
        {
            OnClick(EventArgs.Empty);
            e.Handled = true;
        }

        base.OnKeyDown(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(_palette.Sidebar);
        Ui.Prepare(e.Graphics);

        var bounds = new Rectangle(10, 2, Width - 20, Height - 4);

        if (_active)
        {
            Ui.Fill(e.Graphics, bounds, 10, _palette.SidebarActive);
        }
        else if (_hover)
        {
            Ui.Fill(e.Graphics, bounds, 10, _palette.SidebarHover);
        }

        if (Focused)
        {
            Ui.Stroke(e.Graphics, bounds, 10, Ui.Alpha(_palette.Accent, 90));
        }

        var textColor = _active ? _palette.SidebarText : _palette.SidebarMuted;
        var glyphColor = _active ? _palette.Accent : _palette.SidebarMuted;

        if (_active)
        {
            var indicator = new Rectangle(bounds.X + 2, bounds.Y + (bounds.Height - 18) / 2, 3, 18);
            Ui.Fill(e.Graphics, indicator, 2, _palette.Accent);
        }

        Ui.Glyph(e.Graphics, Glyph, Ui.Icon, glyphColor, new Rectangle(bounds.X + 14, bounds.Y, 22, bounds.Height));
        Ui.SingleLine(e.Graphics, Text, Font, textColor, new Rectangle(bounds.X + 46, bounds.Y, bounds.Width - 52, bounds.Height), Ui.Left);
    }
}
