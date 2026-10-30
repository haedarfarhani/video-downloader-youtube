namespace VideoDownloader.Models;

/// <summary>
/// User-facing options for a single FFmpeg conversion operation. Codec/bitrate names are kept
/// deliberately simple; users can override them through Advanced Settings later.
/// </summary>
public sealed class ConversionOptions
{
    public string InputPath { get; init; } = string.Empty;

    public string OutputPath { get; init; } = string.Empty;

    public string ContainerExtension { get; init; } = "mp4";

    public bool ExtractAudio { get; init; }

    public string VideoCodec { get; init; } = "libx264";

    public string AudioCodec { get; init; } = "aac";

    public int ResolutionHeight { get; init; }

    public string Framerate { get; init; } = "30";

    public long? VideoBitrateKbps { get; init; }

    public long? AudioBitrateKbps { get; init; }

    public bool PreserveMetadata { get; init; }

    public OverwritePolicy Overwrite { get; init; } = OverwritePolicy.Overwrite;
}
