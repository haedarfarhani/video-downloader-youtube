using VideoDownloader.Data.Repositories;
using Microsoft.UI.Xaml;
using VideoDownloader.Models;
namespace VideoDownloader.Services;

public enum ThemeMode { System, Light, Dark }

public interface IThemeService
{
    ThemeMode CurrentTheme { get; }
    Task LoadAsync(CancellationToken ct = default);
    Task SetThemeAsync(ThemeMode mode, CancellationToken ct = default);
    Task ApplyLanguageAsync(Language language, CancellationToken ct = default);
}

public sealed class ThemeService : IThemeService
{
    private readonly ISettingsRepository _repo;
    private ThemeMode _current = ThemeMode.System;

    public ThemeService(ISettingsRepository repo) => _repo = repo;

    public ThemeMode CurrentTheme => _current;

    public async Task LoadAsync(CancellationToken ct = default)
    {
        var values = await _repo.GetAllAsync(ct).ConfigureAwait(false);
        if (values.TryGetValue("ThemeMode", out var v) && Enum.TryParse<ThemeMode>(v, out var parsed))
            _current = parsed;
        Apply();
    }

    public async Task SetThemeAsync(ThemeMode mode, CancellationToken ct = default)
    {
        _current = mode;
        await _repo.SetAsync("ThemeMode", mode.ToString(), ct).ConfigureAwait(false);
        Apply();
    }

    public async Task ApplyLanguageAsync(Language language, CancellationToken ct = default)
    {
        var culture = language == Language.Persian ? new System.Globalization.CultureInfo("fa-IR") : new System.Globalization.CultureInfo("en-US");
        System.Globalization.CultureInfo.CurrentUICulture = culture;
        System.Globalization.CultureInfo.CurrentCulture = culture;
        var flow = language == Language.Persian ? Microsoft.UI.Xaml.FlowDirection.RightToLeft : Microsoft.UI.Xaml.FlowDirection.LeftToRight;
        // RTL layout for Persian — applied through root FlowDirection if available in runtime framework
        // if (App.MainWindow != null) { try { App.MainWindow.FlowDirection = flow; } catch { /* optional */ } }
    }

    private void Apply()
    {
        // Applied via XAMLRequestedTheme / AppThemeBinding in UI; persist for consistency
    }
}
