

namespace VideoDownloader.WinForms {
    public class ApplicationSettings
    {
        public string AppName { get; set; } = "VideoDownloader";
        public string Version { get; set; } = "1.0.0";
        public string DefaultLanguage { get; set; } = "en";
        public string DefaultTheme { get; set; } = "Light";
        public string DefaultQuality { get; set; } = "High";
        public string DefaultOutputDirectory { get; set; } = "Downloads";
        public string DefaultHistoryDirectory { get; set; } = "History";
        public string DefaultSettingsPath { get; set; } = "AppSettings.json";
        public bool UseDarkTheme { get; set; } = true;
        public bool EnableProgressLogging { get; set; } = true;
    }
}


