using VideoDownloader.Helpers;

namespace VideoDownloader.Models;

/// <summary>
/// Persisted record of a finished (or failed) download. Only metadata and paths are stored,
/// never media contents.
/// </summary>
public sealed class DownloadHistoryRecord
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");

    public string Url { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string? OutputPath { get; init; }

    public DownloadMode Mode { get; init; } = DownloadMode.Video;

    public DownloadState State { get; init; } = DownloadState.Completed;

    public DateTime StartedAt { get; init; } = DateTime.UtcNow;

    public DateTime? CompletedAt { get; init; }

    public long FileSizeBytes { get; init; }

    public string? ErrorMessage { get; init; }

    public string SizeText => FileSizeBytes > 0 ? ByteFormatter.Format(FileSizeBytes) : string.Empty;

    /// <summary>False when the title already is the URL, so the URL is not shown twice.</summary>
    public bool ShowUrl => !string.IsNullOrWhiteSpace(Title)
        && !string.Equals(Title, Url, StringComparison.OrdinalIgnoreCase);

    public string StartedText => StartedAt.ToLocalTime().ToString("g");

    public string StateText => State switch
    {
        DownloadState.Completed => "Completed",
        DownloadState.Failed => "Failed",
        DownloadState.Cancelled => "Cancelled",
        _ => State.ToString()
    };

    public string ModeText => Mode == DownloadMode.Audio ? "Audio" : "Video";
}

/// <summary>Validation state of one of the bundled command line tools.</summary>
public sealed class ToolStatus
{
    public required string Name { get; init; }

    public required string Path { get; init; }

    public bool Exists { get; init; }

    public bool Responds { get; init; }

    public string? Version { get; init; }

    public string? Error { get; init; }

    public bool IsReady => Exists && Responds && string.IsNullOrEmpty(Error);

    public string StatusText => IsReady
        ? Version ?? "Ready"
        : !Exists
            ? "Not installed"
            : Error ?? "Not responding";

    public string DetailText => Exists ? Path : $"{Path} (missing)";
}

/// <summary>Aggregate result of validating every required tool.</summary>
public sealed class ToolValidationReport
{
    public IReadOnlyList<ToolStatus> Tools { get; init; } = Array.Empty<ToolStatus>();

    public bool IsReady => Tools.Count > 0 && Tools.All(tool => tool.IsReady);

    public bool IsYtDlpReady => Tools.FirstOrDefault(t => t.Name == ToolNames.YtDlp)?.IsReady == true;

    public bool IsFFmpegReady => Tools.FirstOrDefault(t => t.Name == ToolNames.FFmpeg)?.IsReady == true;

    public bool IsFFprobeReady => Tools.FirstOrDefault(t => t.Name == ToolNames.FFprobe)?.IsReady == true;

    public string Summary => IsReady
        ? "All required tools are available."
        : string.Join(" ", Tools.Where(tool => !tool.IsReady).Select(tool => $"{tool.Name}: {tool.StatusText}."));

    public static ToolValidationReport Empty { get; } = new();
}

public static class ToolNames
{
    public const string YtDlp = "yt-dlp";
    public const string FFmpeg = "ffmpeg";
    public const string FFprobe = "ffprobe";
}
