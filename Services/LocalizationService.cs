namespace VideoDownloader.WinForms.Services;

public interface ILocalizationService
{
    event EventHandler? LanguageChanged;
    string this[string key] { get; }
    string CurrentLanguage { get; }
    void SetLanguage(string lang);
}

public class LocalizationService : ILocalizationService
{
    public event EventHandler? LanguageChanged;
    public string CurrentLanguage { get; private set; } = "en";
    
    private readonly Dictionary<string, Dictionary<string, string>> _translations = new()
    {
        ["en"] = new() {
            ["Dashboard"] = "Dashboard",
            ["Downloads"] = "Downloads",
            ["Converter"] = "Converter",
            ["History"] = "History",
            ["Settings"] = "Settings"
        },
        ["fa"] = new() {
            ["Dashboard"] = "داشبورد",
            ["Downloads"] = "دانلودها",
            ["Converter"] = "تبدیل",
            ["History"] = "تاریخچه",
            ["Settings"] = "تنظیمات"
        }
    };
    
    public string this[string key] => _translations.TryGetValue(CurrentLanguage, out var dict) && dict.TryGetValue(key, out var value) ? value : key;
    
    public void SetLanguage(string lang)
    {
        CurrentLanguage = lang;
        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }
}