using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Resources;
using VideoDownloader.Configuration;
using VideoDownloader.Models;

namespace VideoDownloader.Services;

/// <summary>Loads the selected language's embedded string resource dictionary at runtime.</summary>
public sealed class LocalizationService
{
    private readonly AppPaths _paths;
    private Language _current = Language.English;
    private ResourceDictionary? _dictionary;

    public LocalizationService(AppPaths paths) => _paths = paths;

    public Language Current => _current;

    public async Task LoadAsync(Language language, CancellationToken ct = default)
    {
        _current = language;
        var fileName = language == Language.Persian ? "fa-IR.xaml" : "en-US.xaml";
        var uri = new Uri($"Resources/Strings/{fileName}", UriKind.Relative);
        var dict = new ResourceDictionary { Source = uri };
        // Replace root dictionary so changing Language.Current updates bindings.
        Application.Current?.Resources.MergedDictionaries.Remove(dict);
        Application.Current.Resources.MergedDictionaries.Insert(0, dict);
        _dictionary = dict;
        await Task.CompletedTask;
    }
}
