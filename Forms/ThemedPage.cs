using VideoDownloader.WinForms.Services;

namespace VideoDownloader.WinForms.Forms;

/// <summary>
/// Base class for the pages hosted inside <c>MainForm</c>. Keeps the form wired to the
/// theme service so a theme switch repaints the page immediately.
/// </summary>
public abstract class ThemedPage : Form
{
    protected ThemedPage(IThemeService themeService)
    {
        ThemeService = themeService;

        DoubleBuffered = true;
        FormBorderStyle = FormBorderStyle.None;
        BackColor = themeService.GetThemeColors().Background;

        themeService.ThemeChanged += OnThemeChanged;
    }

    protected IThemeService ThemeService { get; }

    protected ThemeColors Palette => ThemeService.GetThemeColors();

    /// <summary>Set by <c>MainForm</c> so pages can link to other sections.</summary>
    public Action<string>? NavigateTo { get; set; }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        ApplyPalette();
    }

    public void ApplyPalette()
    {
        if (IsDisposed)
        {
            return;
        }

        var palette = Palette;
        BackColor = palette.Background;
        ApplyTheme(palette);
        Invalidate(true);
    }

    /// <summary>Re-colors the page. Called on load and whenever the theme changes.</summary>
    protected abstract void ApplyTheme(ThemeColors palette);

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        if (IsDisposed)
        {
            return;
        }

        if (InvokeRequired)
        {
            try
            {
                BeginInvoke(new Action(ApplyPalette));
            }
            catch (InvalidOperationException)
            {
                // The page is closing; nothing to re-color.
            }

            return;
        }

        ApplyPalette();
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        ThemeService.ThemeChanged -= OnThemeChanged;
        base.OnFormClosed(e);
    }
}
