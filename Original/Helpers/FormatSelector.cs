namespace VideoDownloader.Helpers;

/// <summary>
/// Translates user friendly quality/container choices into yt-dlp format selectors.
/// The selectors degrade gracefully so a download still succeeds when the preferred
/// combination is unavailable for a given site.
/// </summary>
public static class FormatSelector
{
    public const string AutomaticContainer = "auto";

    public static readonly string[] VideoContainers = ["mkv", "mp4", "webm", AutomaticContainer];

    public static readonly string[] AudioFormats = ["mp3", "m4a", "opus", "best"];

    /// <summary>Allowed heights shown in the UI; 0 means "best available".</summary>
    public static readonly int[] VideoHeights = [0, 2160, 1440, 1080, 720, 480, 360, 240];

    public static bool IsSupportedContainer(string? container)
        => !string.IsNullOrWhiteSpace(container)
           && VideoContainers.Contains(container, StringComparer.OrdinalIgnoreCase);

    public static bool IsSupportedAudioFormat(string? format)
        => !string.IsNullOrWhiteSpace(format)
           && AudioFormats.Contains(format, StringComparer.OrdinalIgnoreCase);

    public static string NormalizeContainer(string? container)
        => IsSupportedContainer(container)
            ? container!.Trim().ToLowerInvariant()
            : "mkv";

    public static string NormalizeAudioFormat(string? format)
        => IsSupportedAudioFormat(format)
            ? format!.Trim().ToLowerInvariant()
            : "mp3";

    /// <summary>
    /// Builds a video format expression. mp4/webm are only preferred, never forced, so
    /// incompatible codec combinations fall back to the best available stream instead of failing.
    /// </summary>
    public static string BuildVideoFormat(int maxHeight, string? container)
    {
        var heightFilter = maxHeight > 0 ? $"[height<={Math.Clamp(maxHeight, 144, 4320)}]" : string.Empty;

        return NormalizeContainer(container) switch
        {
            "mp4" =>
                $"bestvideo{heightFilter}[ext=mp4]+bestaudio[ext=m4a]/best{heightFilter}[ext=mp4]/bestvideo{heightFilter}+bestaudio/best{heightFilter}/best",
            "webm" =>
                $"bestvideo{heightFilter}[ext=webm]+bestaudio[ext=webm]/best{heightFilter}[ext=webm]/bestvideo{heightFilter}+bestaudio/best{heightFilter}/best",
            _ =>
                $"bestvideo{heightFilter}+bestaudio/best{heightFilter}/best"
        };
    }

    public static string BuildAudioFormat(string? audioFormat) => NormalizeAudioFormat(audioFormat);
}
