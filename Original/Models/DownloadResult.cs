namespace VideoDownloader.Models;

public enum DownloadOutcome
{
    Succeeded = 0,
    Failed = 1,
    Cancelled = 2
}

/// <summary>Structured result of a download attempt; never relies on console output alone.</summary>
public sealed class DownloadResult
{
    public DownloadOutcome Outcome { get; init; }

    public bool Success => Outcome == DownloadOutcome.Succeeded;

    public int ExitCode { get; init; }

    public string? OutputFilePath { get; init; }

    public long FileSizeBytes { get; init; }

    public string? ErrorMessage { get; init; }

    /// <summary>Trimmed tail of the tool's diagnostics, useful for troubleshooting.</summary>
    public string Diagnostics { get; init; } = string.Empty;

    public TimeSpan Duration { get; init; }

    public static DownloadResult Succeeded(string outputFilePath, long fileSize, string diagnostics, TimeSpan duration) => new()
    {
        Outcome = DownloadOutcome.Succeeded,
        ExitCode = 0,
        OutputFilePath = outputFilePath,
        FileSizeBytes = fileSize,
        Diagnostics = diagnostics,
        Duration = duration
    };

    public static DownloadResult Failed(string error, int exitCode = -1, string diagnostics = "") => new()
    {
        Outcome = DownloadOutcome.Failed,
        ExitCode = exitCode,
        ErrorMessage = error,
        Diagnostics = diagnostics
    };

    public static DownloadResult Cancelled() => new()
    {
        Outcome = DownloadOutcome.Cancelled,
        ErrorMessage = "The download was cancelled."
    };
}

/// <summary>Result of a media probe performed by ffprobe.</summary>
public sealed class MediaProbeResult
{
    public bool Success { get; init; }

    public string? ErrorMessage { get; init; }

    public int ExitCode { get; init; }

    public TimeSpan Duration { get; init; }

    public string? FormatName { get; init; }

    public string? FormatLongName { get; init; }

    public long SizeBytes { get; init; }

    public int BitRate { get; init; }

    public IReadOnlyList<MediaStreamInfo> Streams { get; init; } = Array.Empty<MediaStreamInfo>();

    public MediaStreamInfo? PrimaryVideoStream => Streams.FirstOrDefault(stream => stream.IsVideo);

    public MediaStreamInfo? PrimaryAudioStream => Streams.FirstOrDefault(stream => stream.IsAudio);

    public string? Resolution
    {
        get
        {
            var video = PrimaryVideoStream;
            return video is null || video.Width <= 0 || video.Height <= 0 ? null : $"{video.Width}x{video.Height}";
        }
    }

    public string Summary =>
        $"{FormatName ?? "unknown"} | {Duration:h\\:mm\\:ss} | video: {PrimaryVideoStream?.CodecName ?? "none"} | audio: {PrimaryAudioStream?.CodecName ?? "none"}";
}

public sealed class MediaStreamInfo
{
    public int Index { get; init; }

    public string CodecType { get; init; } = string.Empty;

    public string? CodecName { get; init; }

    public int Width { get; init; }

    public int Height { get; init; }

    public double FrameRate { get; init; }

    public int SampleRate { get; init; }

    public int Channels { get; init; }

    public string? Language { get; init; }

    public bool IsVideo => string.Equals(CodecType, "video", StringComparison.OrdinalIgnoreCase);

    public bool IsAudio => string.Equals(CodecType, "audio", StringComparison.OrdinalIgnoreCase);
}

/// <summary>Result of an FFmpeg invocation.</summary>
public sealed class FFmpegResult
{
    public bool Success { get; init; }

    public int ExitCode { get; init; }

    public string? OutputFilePath { get; init; }

    public string? ErrorMessage { get; init; }

    public string Diagnostics { get; init; } = string.Empty;

    public static FFmpegResult Failed(string error, int exitCode = -1, string diagnostics = "") => new()
    {
        Success = false,
        ExitCode = exitCode,
        ErrorMessage = error,
        Diagnostics = diagnostics
    };
}
