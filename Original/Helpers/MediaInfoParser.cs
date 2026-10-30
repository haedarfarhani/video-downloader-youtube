using System.Globalization;
using System.Text.Json;
using VideoDownloader.Models;

namespace VideoDownloader.Helpers;

/// <summary>Parses the JSON document produced by <c>yt-dlp --dump-single-json</c>.</summary>
public static class MediaInfoParser
{
    public static MediaInfo Parse(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new FormatException("yt-dlp returned an unexpected JSON payload.");
        }

        var isPlaylist = string.Equals(GetString(root, "_type"), "playlist", StringComparison.OrdinalIgnoreCase);

        var formats = ParseFormats(root);

        return new MediaInfo
        {
            Title = GetString(root, "title") ?? GetString(root, "id") ?? "Untitled media",
            Author = GetString(root, "uploader")
                     ?? GetString(root, "channel")
                     ?? GetString(root, "creator")
                     ?? string.Empty,
            Extractor = GetString(root, "extractor_key") ?? GetString(root, "extractor") ?? string.Empty,
            WebpageUrl = GetString(root, "webpage_url") ?? GetString(root, "original_url") ?? string.Empty,
            ThumbnailUrl = GetString(root, "thumbnail") ?? string.Empty,
            DurationSeconds = (int)Math.Round(GetDouble(root, "duration") ?? 0),
            Width = GetInt(root, "width") ?? 0,
            Height = GetInt(root, "height") ?? 0,
            FileSizeBytes = GetLong(root, "filesize")
                            ?? GetLong(root, "filesize_approx")
                            ?? 0,
            UploadDate = GetString(root, "upload_date"),
            IsLive = GetBool(root, "is_live") ?? false,
            IsPlaylist = isPlaylist,
            PlaylistCount = GetInt(root, "playlist_count")
                            ?? GetInt(root, "n_entries")
                            ?? (root.TryGetProperty("entries", out var entries) && entries.ValueKind == JsonValueKind.Array ? entries.GetArrayLength() : 0),
            Formats = formats
        };
    }

    private static IReadOnlyList<MediaFormat> ParseFormats(JsonElement root)
    {
        if (!root.TryGetProperty("formats", out var formatsElement) || formatsElement.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<MediaFormat>();
        }

        var formats = new List<MediaFormat>();
        foreach (var item in formatsElement.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            formats.Add(new MediaFormat
            {
                FormatId = GetString(item, "format_id") ?? string.Empty,
                Extension = GetString(item, "ext") ?? string.Empty,
                Resolution = GetString(item, "resolution"),
                Height = GetInt(item, "height") ?? 0,
                Width = GetInt(item, "width") ?? 0,
                VideoCodec = GetString(item, "vcodec"),
                AudioCodec = GetString(item, "acodec"),
                FileSizeBytes = GetLong(item, "filesize") ?? GetLong(item, "filesize_approx"),
                Fps = GetDouble(item, "fps"),
                FormatNote = GetString(item, "format_note")
            });
        }

        return formats;
    }

    private static string? GetString(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var value) || value.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        var text = value.GetString();
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    private static double? GetDouble(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.Number when value.TryGetDouble(out var number) => number,
            JsonValueKind.String when double.TryParse(value.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) => parsed,
            _ => null
        };
    }

    private static int? GetInt(JsonElement element, string property)
    {
        var number = GetDouble(element, property);
        return number is null ? null : (int)Math.Round(number.Value);
    }

    private static long? GetLong(JsonElement element, string property)
    {
        var number = GetDouble(element, property);
        return number is null ? null : (long)Math.Round(number.Value);
    }

    private static bool? GetBool(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => null
        };
    }
}
