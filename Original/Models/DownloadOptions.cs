namespace VideoDownloader.Models;

/// <summary>
/// The kind of media that should be downloaded.
/// </summary>
public enum DownloadMode
{
    Video = 0,
    Audio = 1
}

/// <summary>
/// User supplied options for a single download request.
/// </summary>
public sealed class DownloadOptions
{
    public string Url { get; init; } = string.Empty;

    public string OutputDirectory { get; init; } = string.Empty;

    public DownloadMode Mode { get; init; } = DownloadMode.Video;

    /// <summary>Maximum video height in pixels, 0 means "best available".</summary>
    public int MaxVideoHeight { get; init; }

    /// <summary>Preferred video container (mkv, mp4, webm). "auto" lets yt-dlp decide.</summary>
    public string Container { get; init; } = "mkv";

    /// <summary>Audio target format (mp3, m4a, opus, best).</summary>
    public string AudioFormat { get; init; } = "mp3";

    public bool IncludePlaylist { get; init; }

    public int MaxRetries { get; init; } = 3;

    /// <summary>Title reported by the analyse step, used as a fallback display name.</summary>
    public string? Title { get; init; }

    public DownloadOptions WithUrl(string url) => new()
    {
        Url = url,
        OutputDirectory = OutputDirectory,
        Mode = Mode,
        MaxVideoHeight = MaxVideoHeight,
        Container = Container,
        AudioFormat = AudioFormat,
        IncludePlaylist = IncludePlaylist,
        MaxRetries = MaxRetries,
        Title = Title
    };
}
