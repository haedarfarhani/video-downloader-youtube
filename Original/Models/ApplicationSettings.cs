using System.Globalization;

namespace VideoDownloader.Models;

public enum ThemeMode { System = 0, Light = 1, Dark = 2 }

public enum Language { English = 0, Persian = 1 }

public enum OverwritePolicy { Ask, Overwrite, Ignore }

public enum ConversionMediaType { Video, Audio }

public class ApplicationSettings
{
    // Core defaults
    public int MaxConcurrentDownloads { get; init; } = 2;
    public string DefaultOutputDirectory { get; init; } = string.Empty;
    public bool AskBeforeOverwriting { get; init; } = true;
    public bool ShowCompletionNotifications { get; init; } = true;

    // UI / theme / language
    public ThemeMode Theme { get; init; } = ThemeMode.System;
    public Language Language { get; init; } = Language.English;

    // Download defaults
    public string DefaultVideoQuality { get; init; } = "best";
    public string DefaultAudioFormat { get; init; } = "bestaudio";
    public string PlaylistBehavior { get; init; } = "ask";
    public string FilenameTemplate { get; init; } = "%(title)~%(id)";
    public int MaxRetryAttempts { get; init; } = 3;

    // Conversion defaults
    public string DefaultConversionOutputDirectory { get; init; } = string.Empty;
    public string DefaultCodecAndPreset { get; init; } = "MP4-H264";
    public int MaxConcurrentConversions { get; init; } = 2;
    public bool HardwareAcceleration { get; init; } = true;

    public static ApplicationSettings CreateDefault(string defaultDirectory) => new()
    {
        DefaultOutputDirectory = defaultDirectory,
        MaxConcurrentDownloads = 2,
        Theme = ThemeMode.System,
        Language = Language.English,
        DefaultConversionOutputDirectory = defaultDirectory,
        DefaultCodecAndPreset = "MP4-H264",
        HardwareAcceleration = true
    };

    public ApplicationSettings Normalize(string fallbackDirectory) => new()
    {
        DefaultOutputDirectory = string.IsNullOrWhiteSpace(DefaultOutputDirectory)
            ? fallbackDirectory : DefaultOutputDirectory.Trim(),
        MaxConcurrentDownloads = Math.Clamp(MaxConcurrentDownloads, 1, 5),
        MaxRetryAttempts = Math.Clamp(MaxRetryAttempts, 0, 9),
        DefaultConversionOutputDirectory = string.IsNullOrWhiteSpace(DefaultConversionOutputDirectory)
            ? fallbackDirectory : DefaultConversionOutputDirectory.Trim(),
        MaxConcurrentConversions = Math.Clamp(MaxConcurrentConversions, 1, 8)
    };

    public CultureInfo Culture => Language == Language.Persian ? new CultureInfo("fa-IR") : new CultureInfo("en-US");
}
