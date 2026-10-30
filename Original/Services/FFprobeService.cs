using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using VideoDownloader.Helpers;

namespace VideoDownloader.Services;

public sealed class MediaStreamInfo
{
    public string CodecName { get; init; } = string.Empty;
    public string CodecType { get; init; } = string.Empty; // video/audio
    public int? Width { get; init; }
    public int? Height { get; init; }
    public double? Fps { get; init; }
    public long? BitRate { get; init; }
    public int? Channels { get; init; }
    public int? SampleRate { get; init; }
    public string? Duration { get; init; }
}

public sealed class MediaInfoMetadata
{
    public string FormatName { get; init; } = string.Empty;
    public string FormatLongName { get; init; } = string.Empty;
    public long DurationSeconds { get; init; }
    public long SizeBytes { get; init; }
    public string BitRate { get; init; } = string.Empty;
    public IReadOnlyList<MediaStreamInfo> Streams { get; init; } = Array.Empty<MediaStreamInfo>();
    public bool HasVideo => Streams.Any(s => s.CodecType == "video");
    public bool HasAudio => Streams.Any(s => s.CodecType == "audio");
}

public interface IFFprobeService
{
    Task<MediaInfoMetadata?> InspectAsync(string filePath, CancellationToken cancellationToken = default);
}

public sealed class FFprobeService : IFFprobeService
{
    private static readonly TimeSpan InspectTimeout = TimeSpan.FromSeconds(60);
    private readonly IProcessRunner _processRunner;
    private readonly IToolPathService _toolPaths;
    private readonly ILogger<FFprobeService> _logger;

    public FFprobeService(IProcessRunner processRunner, IToolPathService toolPaths, ILogger<FFprobeService> logger)
    {
        _processRunner = processRunner;
        _toolPaths = toolPaths;
        _logger = logger;
    }

    public async Task<MediaInfoMetadata?> InspectAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath)) { _logger.LogWarning("File not found for inspection: {Path}", filePath); return null; }
        if (!File.Exists(_toolPaths.FFprobePath)) { _logger.LogWarning("FFprobe missing"); return null; }

        var arguments = new List<string>
        {
            "-v", "quiet",
            "-print_format", "json",
            "-show_format",
            "-show_streams",
            "-i", filePath
        };

        try
        {
            var result = await _processRunner
                .RunAsync(_toolPaths.FFprobePath, arguments, timeout: InspectTimeout, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (result.Cancelled) { _logger.LogInformation("FFprobe cancelled for {Path}", filePath); return null; }
            if (!result.Started) { _logger.LogWarning("FFprobe could not start for {Path}", filePath); return null; }
            if (result.ExitCode != 0) { _logger.LogWarning("FFprobe exit {Code} for {Path}: {Diag}", result.ExitCode, filePath, result.Diagnostics); return null; }

            using var doc = JsonDocument.Parse(result.StandardOutput);
            var root = doc.RootElement;
            var format = root.GetProperty("format");
            var streams = root.GetProperty("streams");

            var metadata = new MediaInfoMetadata
            {
                FormatName = format.GetProperty("format_name").GetString() ?? string.Empty,
                FormatLongName = format.GetProperty("format_long_name").GetString() ?? string.Empty,
                DurationSeconds = TryParseDouble(format.GetProperty("duration").GetString()) > 0 ? (long)TryParseDouble(format.GetProperty("duration").GetString()) : 0,
                SizeBytes = format.TryGetProperty("size", out var sz) ? sz.GetString() is {} s && long.TryParse(s, out var szVal) ? szVal : 0 : 0,
                BitRate = format.TryGetProperty("bit_rate", out var br) ? br.GetString() ?? string.Empty : string.Empty,
                Streams = ParseStreams(streams)
            };

            return metadata;
        }
        catch (OperationCanceledException) { return null; }
        catch (Exception ex) { _logger.LogError(ex, "FFprobe inspection failed for {Path}", filePath); return null; }
    }

    private static double TryParseDouble(string? value) => double.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : 0;

    private static List<MediaStreamInfo> ParseStreams(JsonElement streams)
    {
        var list = new List<MediaStreamInfo>();
        foreach (var s in streams.EnumerateArray())
        {
            list.Add(new MediaStreamInfo
            {
                CodecName = s.TryGetProperty("codec_name", out var cn) ? cn.GetString() ?? string.Empty : string.Empty,
                CodecType = s.TryGetProperty("codec_type", out var ct) ? ct.GetString() ?? string.Empty : string.Empty,
                Width = s.TryGetProperty("width", out var w) && w.ValueKind != JsonValueKind.Null ? w.GetInt32() : null,
                Height = s.TryGetProperty("height", out var h) && h.ValueKind != JsonValueKind.Null ? h.GetInt32() : null,
                Fps = s.TryGetProperty("r_frame_rate", out var f) ? TryParseFps(f.GetString()) : null,
                BitRate = s.TryGetProperty("bit_rate", out var b) && b.ValueKind != JsonValueKind.Null ? b.GetInt64() : null,
                Channels = s.TryGetProperty("channels", out var ch) && ch.ValueKind != JsonValueKind.Null ? ch.GetInt32() : null,
                SampleRate = s.TryGetProperty("sample_rate", out var sr) && sr.ValueKind != JsonValueKind.Null ? sr.GetInt32() : null,
                Duration = s.TryGetProperty("duration", out var d) ? d.GetString() : null
            });
        }
        return list;
    }

    private static double? TryParseFps(string? rate)
    {
        if (string.IsNullOrWhiteSpace(rate)) return null;
        if (rate.Contains('/'))
        {
            var parts = rate.Split('/');
            if (double.TryParse(parts[0], out var n) && double.TryParse(parts[1], out var d) && d != 0)
                return n / d;
        }
        return double.TryParse(rate, out var v) ? v : null;
    }
}
