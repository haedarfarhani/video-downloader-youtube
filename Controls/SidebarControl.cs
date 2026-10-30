using VideoDownloader.WinForms.Services;

namespace VideoDownloader.WinForms.Controls;

/// <summary>The persistent left rail: brand, navigation destinations and quick theme toggle.</summary>
public sealed class SidebarControl : Panel
{
    private static readonly (string Section, string Glyph)[] Sections =
    [
        ("Dashboard", "\uE80F"),
        ("Downloads", "\uE896"),
        ("Converter", "\uE895"),
        ("History", "\uE81C"),
        ("Settings", "\uE713")
    ];

    private readonly IThemeService _theme;
    private readonly ILocalizationService _localization;
    private readonly Action<string> _navigate;
    private readonly Dictionary<string, NavItem> _items = new(StringComparer.Ordinal);

    private readonly Label _brandName = null!;
    private readonly Label _brandTagline = null!;
    private readonly Label _sectionLabel = null!;
    private readonly Label _versionLabel = null!;
    private readonly NavItem _themeItem = null!;
    private readonly NavItem _languageItem = null!;

    private ThemeColors _palette;
    private bool _ready;

    public SidebarControl(IThemeService theme, ILocalizationService localization, Action<string> navigate)
    {
        _theme = theme;
        _localization = localization;
        _navigate = navigate;
        _palette = theme.GetThemeColors();

        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw, true);

        Dock = DockStyle.Left;
        Width = 264;
        Font = Ui.Body;
        BackColor = _palette.Sidebar;

        _brandName = MakeLabel("VideoDownloader", Ui.Subheading, _palette.SidebarText);
        _brandTagline = MakeLabel("Media toolkit", Ui.Caption, _palette.SidebarMuted);
        _sectionLabel = MakeLabel("WORKSPACE", Ui.CaptionStrong, _palette.SidebarMuted);
        _versionLabel = MakeLabel($"v{Application.ProductVersion.Split('+')[0]}", Ui.Caption, _palette.SidebarMuted);

        Controls.Add(_brandName);
        Controls.Add(_brandTagline);
        Controls.Add(_sectionLabel);
        Controls.Add(_versionLabel);

        foreach (var (section, glyph) in Sections)
        {
            var item = new NavItem
            {
                Glyph = glyph,
                Text = _localization[section],
                Tag = section
            };

            item.Click += (_, _) => _navigate((string)item.Tag!);
            item.ApplyPalette(_palette);

            _items[section] = item;
            Controls.Add(item);
        }

        _themeItem = new NavItem { Glyph = "\uE706", Text = "Dark mode" };
        _themeItem.Click += (_, _) => _theme.Toggle();
        _themeItem.ApplyPalette(_palette);
        Controls.Add(_themeItem);

        _languageItem = new NavItem { Glyph = "\uE8C1", Text = "English" };
        _languageItem.Click += (_, _) => _localization.SetLanguage(_localization.CurrentLanguage == "fa" ? "en" : "fa");
        _languageItem.ApplyPalette(_palette);
        Controls.Add(_languageItem);

        localization.LanguageChanged += OnLanguageChanged;
        theme.ThemeChanged += OnThemeChanged;

        _ready = true;
        PerformLayout();
    }

    public void SetActiveItem(string section)
    {
        foreach (var (key, item) in _items)
        {
            item.IsActive = string.Equals(key, section, StringComparison.Ordinal);
        }
    }

    private Label MakeLabel(string text, Font font, Color color) => new()
    {
        Text = text,
        Font = font,
        AutoSize = true,
        ForeColor = color,
        BackColor = _palette.Sidebar
    };

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);

        if (!_ready)
        {
            return;
        }

        _brandName.Location = new Point(78, 26);
        _brandTagline.Location = new Point(79, 47);
        _sectionLabel.Location = new Point(26, 90);

        var navTop = 114;
        foreach (var (_, item) in _items)
        {
            item.SetBounds(12, navTop, Width - 24, 44);
            navTop += 48;
        }

        navTop += 12;
        _themeItem.SetBounds(12, navTop, Width - 24, 44);
        navTop += 44;

        _languageItem.SetBounds(12, navTop, Width - 24, 44);

        _versionLabel.Location = new Point(26, Height - 32);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(_palette.Sidebar);
        Ui.Prepare(e.Graphics);

        var badge = new Rectangle(24, 24, 40, 40);
        Ui.Fill(e.Graphics, badge, 12, _palette.AccentSoft);
        Ui.Glyph(e.Graphics, "\uE714", Ui.IconLarge, _palette.Accent, badge);

        using var divider = new Pen(_palette.SidebarBorder);
        e.Graphics.DrawLine(divider, 0, Height - 54, Width, Height - 54);
        e.Graphics.DrawLine(divider, Width - 1, 0, Width - 1, Height);
    }

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        if (IsDisposed)
        {
            return;
        }

        _palette = _theme.GetThemeColors();
        BackColor = _palette.Sidebar;

        foreach (var label in new[] { _brandName, _brandTagline, _sectionLabel, _versionLabel })
        {
            label.BackColor = _palette.Sidebar;
        }

        _brandName.ForeColor = _palette.SidebarText;
        _brandTagline.ForeColor = _palette.SidebarMuted;
        _sectionLabel.ForeColor = _palette.SidebarMuted;
        _versionLabel.ForeColor = _palette.SidebarMuted;

        foreach (var item in _items.Values)
        {
            item.ApplyPalette(_palette);
        }

        _themeItem.ApplyPalette(_palette);
        _languageItem.ApplyPalette(_palette);
        UpdateToggleLabels();

        Invalidate();
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        if (IsDisposed)
        {
            return;
        }

        foreach (var (section, item) in _items)
        {
            item.Text = _localization[section];
            item.Invalidate();
        }

        UpdateToggleLabels();
        Invalidate();
    }

    private void UpdateToggleLabels()
    {
        _themeItem.Text = _theme.IsDarkTheme ? "Light mode" : "Dark mode";
        _themeItem.Invalidate();

        _languageItem.Text = _localization.CurrentLanguage == "fa" ? "فارسی" : "English";
        _languageItem.Invalidate();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _localization.LanguageChanged -= OnLanguageChanged;
            _theme.ThemeChanged -= OnThemeChanged;
        }

        base.Dispose(disposing);
    }
}
