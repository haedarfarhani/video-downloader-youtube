namespace VideoDownloader.WinForms.Services;

public interface IThemeService
{
    event EventHandler? ThemeChanged;

    ThemeColors GetThemeColors();

    bool IsDarkTheme { get; set; }

    void Toggle();
}

/// <summary>
/// The palette used by every screen. Colors are grouped by role so controls never
/// hard-code a shade and both themes stay consistent.
/// </summary>
public sealed class ThemeColors
{
    public required bool IsDark { get; init; }

    // Page chrome
    public required Color Background { get; init; }
    public required Color Surface { get; init; }
    public required Color Divider { get; init; }

    // Sidebar (always a deep tone, light or dark theme)
    public required Color Sidebar { get; init; }
    public required Color SidebarBorder { get; init; }
    public required Color SidebarText { get; init; }
    public required Color SidebarMuted { get; init; }
    public required Color SidebarHover { get; init; }
    public required Color SidebarActive { get; init; }

    // Cards and inputs
    public required Color Card { get; init; }
    public required Color CardBorder { get; init; }
    public required Color CardHover { get; init; }
    public required Color Input { get; init; }
    public required Color InputBorder { get; init; }

    // Text
    public required Color TextPrimary { get; init; }
    public required Color TextSecondary { get; init; }
    public required Color TextMuted { get; init; }

    // Accent
    public required Color Accent { get; init; }
    public required Color AccentHover { get; init; }
    public required Color AccentPressed { get; init; }
    public required Color AccentSoft { get; init; }
    public required Color OnAccent { get; init; }

    // Status
    public required Color Success { get; init; }
    public required Color SuccessSoft { get; init; }
    public required Color Danger { get; init; }
    public required Color DangerSoft { get; init; }
    public required Color Warning { get; init; }
    public required Color Chip { get; init; }

    public Color Shadow => IsDark ? Color.FromArgb(90, 0, 0, 0) : Color.FromArgb(16, 15, 23, 42);

    public static ThemeColors Light => new()
    {
        IsDark = false,
        Background = Color.FromArgb(0xF4, 0xF5, 0xF7),
        Surface = Color.White,
        Divider = Color.FromArgb(0xE7, 0xE9, 0xEE),

        Sidebar = Color.FromArgb(0x11, 0x17, 0x22),
        SidebarBorder = Color.FromArgb(0x1E, 0x26, 0x33),
        SidebarText = Color.FromArgb(0xE8, 0xEC, 0xF2),
        SidebarMuted = Color.FromArgb(0x8C, 0x9A, 0xAD),
        SidebarHover = Color.FromArgb(0x1B, 0x23, 0x30),
        SidebarActive = Color.FromArgb(0x23, 0x2C, 0x3B),

        Card = Color.White,
        CardBorder = Color.FromArgb(0xE7, 0xE9, 0xEE),
        CardHover = Color.FromArgb(0xFA, 0xFB, 0xFC),
        Input = Color.FromArgb(0xF7, 0xF8, 0xFA),
        InputBorder = Color.FromArgb(0xE2, 0xE5, 0xEB),

        TextPrimary = Color.FromArgb(0x0F, 0x17, 0x2A),
        TextSecondary = Color.FromArgb(0x5B, 0x67, 0x7A),
        TextMuted = Color.FromArgb(0x94, 0xA0, 0xB0),

        Accent = Color.FromArgb(0xF9, 0x73, 0x16),
        AccentHover = Color.FromArgb(0xEA, 0x58, 0x0C),
        AccentPressed = Color.FromArgb(0xC2, 0x41, 0x0C),
        AccentSoft = Color.FromArgb(0xFF, 0xF1, 0xE6),
        OnAccent = Color.White,

        Success = Color.FromArgb(0x16, 0xA3, 0x4A),
        SuccessSoft = Color.FromArgb(0xE9, 0xF7, 0xEF),
        Danger = Color.FromArgb(0xDC, 0x26, 0x26),
        DangerSoft = Color.FromArgb(0xFE, 0xEE, 0xEE),
        Warning = Color.FromArgb(0xD9, 0x77, 0x06),
        Chip = Color.FromArgb(0xF1, 0xF3, 0xF7)
    };

    public static ThemeColors Dark => new()
    {
        IsDark = true,
        Background = Color.FromArgb(0x0E, 0x11, 0x16),
        Surface = Color.FromArgb(0x12, 0x17, 0x1E),
        Divider = Color.FromArgb(0x23, 0x2B, 0x36),

        Sidebar = Color.FromArgb(0x09, 0x0C, 0x11),
        SidebarBorder = Color.FromArgb(0x16, 0x1C, 0x25),
        SidebarText = Color.FromArgb(0xE6, 0xEA, 0xF0),
        SidebarMuted = Color.FromArgb(0x7A, 0x88, 0x9C),
        SidebarHover = Color.FromArgb(0x14, 0x1A, 0x23),
        SidebarActive = Color.FromArgb(0x1C, 0x24, 0x30),

        Card = Color.FromArgb(0x16, 0x1B, 0x22),
        CardBorder = Color.FromArgb(0x24, 0x2C, 0x38),
        CardHover = Color.FromArgb(0x1A, 0x20, 0x29),
        Input = Color.FromArgb(0x1A, 0x20, 0x29),
        InputBorder = Color.FromArgb(0x2C, 0x35, 0x43),

        TextPrimary = Color.FromArgb(0xE6, 0xEA, 0xF0),
        TextSecondary = Color.FromArgb(0x9A, 0xA7, 0xB8),
        TextMuted = Color.FromArgb(0x6B, 0x7A, 0x8F),

        Accent = Color.FromArgb(0xFB, 0x92, 0x3C),
        AccentHover = Color.FromArgb(0xFD, 0xBA, 0x74),
        AccentPressed = Color.FromArgb(0xEA, 0x58, 0x0C),
        AccentSoft = Color.FromArgb(0x2A, 0x1D, 0x13),
        OnAccent = Color.FromArgb(0x1A, 0x10, 0x08),

        Success = Color.FromArgb(0x34, 0xD3, 0x99),
        SuccessSoft = Color.FromArgb(0x12, 0x2A, 0x23),
        Danger = Color.FromArgb(0xF8, 0x71, 0x71),
        DangerSoft = Color.FromArgb(0x2E, 0x1A, 0x1A),
        Warning = Color.FromArgb(0xFB, 0xBF, 0x24),
        Chip = Color.FromArgb(0x1E, 0x25, 0x30)
    };
}

public class ThemeService : IThemeService
{
    private bool _isDarkTheme;

    public event EventHandler? ThemeChanged;

    public bool IsDarkTheme
    {
        get => _isDarkTheme;
        set
        {
            if (_isDarkTheme == value)
            {
                return;
            }

            _isDarkTheme = value;
            ThemeChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Toggle() => IsDarkTheme = !IsDarkTheme;

    public ThemeColors GetThemeColors() => IsDarkTheme ? ThemeColors.Dark : ThemeColors.Light;
}
