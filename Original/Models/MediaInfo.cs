using System.Globalization;
using VideoDownloader.Helpers;

namespace VideoDownloader.Models;

/// <summary>Metadata returned by yt-dlp for a single media item or playlist.</summary>
public sealed class MediaInfo
{
    public string Title { get; init; } = string.Empty;

    public string Author { get; init; } = string.Empty;

    public string Extractor { get; init; } = string.Empty;

    public string WebpageUrl { get; init; } = string.Empty;

    public string ThumbnailUrl { get; init; } = string.Empty;

    public int DurationSeconds { get; init; }

    public int Width { get; init; }

    public int Height { get; init; }

    public long FileSizeBytes { get; init; }

    public string? UploadDate { get; init; }

    public bool IsLive { get; init; }

    public bool IsPlaylist { get; init; }

    public int PlaylistCount { get; init; }

    public IReadOnlyList<MediaFormat> Formats { get; init; } = Array.Empty<MediaFormat>();

    public string DurationText
    {
        get
        {
            if (DurationSeconds <= 0)
            {
                return "Unknown length";
            }

            var duration = TimeSpan.FromSeconds(DurationSeconds);
            return duration.TotalHours >= 1
                ? $"{(int)duration.TotalHours}:{duration.Minutes:00}:{duration.Seconds:00}"
                : $"{duration.Minutes}:{duration.Seconds:00}";
        }
    }

    public string ResolutionText => Width > 0 && Height > 0 ? $"{Width}x{Height}" : string.Empty;

    public string Summary
    {
        get
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(Author))
            {
                parts.Add(Author);
            }

            parts.Add(DurationText);

            if (!string.IsNullOrWhiteSpace(ResolutionText))
            {
                parts.Add(ResolutionText);
            }

            if (IsPlaylist)
            {
                parts.Add($"{PlaylistCount} item(s) in playlist");
            }

            if (FileSizeBytes > 0)
            {
                parts.Add($"~{ByteFormatter.Format(FileSizeBytes)}");
            }

            return string.Join(" \u2022 ", parts);
        }
    }
}

/// <summary>A single downloadable format reported by yt-dlp.</summary>
public sealed class MediaFormat
{
    public string FormatId { get; init; } = string.Empty;

    public string Extension { get; init; } = string.Empty;

    public string? Resolution { get; init; }

    public int Height { get; init; }

    public int Width { get; init; }

    public string? VideoCodec { get; init; }

    public string? AudioCodec { get; init; }

    public long? FileSizeBytes { get; init; }

    public double? Fps { get; init; }

    public string? FormatNote { get; init; }

    public bool HasVideo => !string.IsNullOrEmpty(VideoCodec) && VideoCodec != "none";

    public bool HasAudio => !string.IsNullOrEmpty(AudioCodec) && AudioCodec != "none";

    public string Description
    {
        get
        {
            var bits = new List<string> { FormatId };
            if (!string.IsNullOrWhiteSpace(Resolution))
            {
                bits.Add(Resolution!);
            }
            else if (Height > 0)
            {
                bits.Add($"{Height}p");
            }

            if (Fps is > 0)
            {
                bits.Add($"{Fps.Value.ToString("0.##", CultureInfo.InvariantCulture)}fps");
            }

            if (!string.IsNullOrWhiteSpace(Extension))
            {
                bits.Add(Extension.ToUpperInvariant());
            }

            return string.Join(" ", bits);
        }
    }
}
