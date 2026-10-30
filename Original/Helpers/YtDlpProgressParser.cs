using System.Globalization;

namespace VideoDownloader.Helpers;

/// <summary>A parsed snapshot of yt-dlp progress output.</summary>
public readonly record struct ParsedProgress(
    double? Percent,
    long? DownloadedBytes,
    long? TotalBytes,
    double? SpeedBytesPerSecond,
    int? EtaSeconds);

/// <summary>
/// Parses the machine readable progress and final-path lines emitted by yt-dlp.
/// Kept free of UI and process dependencies so it can be unit tested.
/// </summary>
public static class YtDlpProgressParser
{
    private static readonly string[] SizeSuffixes = ["B", "KiB", "MiB", "GiB", "TiB", "PiB"];

    public static bool TryParseProgress(string? line, out ParsedProgress progress)
    {
        progress = default;
        if (string.IsNullOrWhiteSpace(line))
        {
            return false;
        }

        var prefixIndex = line.IndexOf(YtDlpArgumentBuilder.ProgressPrefix, StringComparison.Ordinal);
        if (prefixIndex < 0)
        {
            return false;
        }

        var payload = line[(prefixIndex + YtDlpArgumentBuilder.ProgressPrefix.Length)..];
        var fields = payload.Split('|');
        if (fields.Length < 7)
        {
            return false;
        }

        var downloaded = TryParseSize(fields[1], out var downloadedBytes) ? downloadedBytes : (long?)null;
        long? total = TryParseSize(fields[2], out var totalBytes)
            ? totalBytes
            : TryParseSize(fields[3], out var estimatedBytes)
                ? estimatedBytes
                : null;

        double? percent = TryParsePercent(fields[4], out var parsedPercent)
            ? parsedPercent
            : downloaded is > 0 && total is > 0
                ? Math.Round(downloaded.Value * 100d / total.Value, 1)
                : null;

        double? speed = TryParseSpeed(fields[5], out var parsedSpeed) ? parsedSpeed : null;
        int? eta = TryParseEtaSeconds(fields[6]);

        progress = new ParsedProgress(percent, downloaded, total, speed, eta);
        return true;
    }

    public static bool TryParseFinalPath(string? line, out string path)
    {
        path = string.Empty;
        if (string.IsNullOrWhiteSpace(line))
        {
            return false;
        }

        var marker = line.IndexOf(YtDlpArgumentBuilder.FinalPathPrefix, StringComparison.Ordinal);
        if (marker < 0)
        {
            return false;
        }

        var candidate = line[(marker + YtDlpArgumentBuilder.FinalPathPrefix.Length)..].Trim().Trim('"');
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return false;
        }

        path = candidate;
        return true;
    }

    public static bool TryParsePercent(string? text, out double percent)
    {
        percent = 0;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var cleaned = text.Trim().TrimEnd('%').Trim();
        if (cleaned.Equals("NA", StringComparison.OrdinalIgnoreCase)
            || cleaned.Equals("N/A", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!double.TryParse(cleaned, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
        {
            return false;
        }

        percent = Math.Clamp(value, 0, 100);
        return true;
    }

    public static bool TryParseSize(string? text, out long bytes)
    {
        bytes = 0;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var cleaned = text.Trim();
        if (cleaned.Equals("NA", StringComparison.OrdinalIgnoreCase)
            || cleaned.Equals("N/A", StringComparison.OrdinalIgnoreCase)
            || cleaned.Contains("Unknown", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        cleaned = cleaned.Replace("/s", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();

        var numberEnd = 0;
        while (numberEnd < cleaned.Length
               && (char.IsDigit(cleaned[numberEnd]) || cleaned[numberEnd] is '.' or ','))
        {
            numberEnd++;
        }

        if (numberEnd == 0)
        {
            return false;
        }

        var numberPart = cleaned[..numberEnd].Replace(",", string.Empty);
        if (!double.TryParse(numberPart, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
        {
            return false;
        }

        var unit = cleaned[numberEnd..].Trim();
        var multiplier = 1d;
        for (var i = 0; i < SizeSuffixes.Length; i++)
        {
            if (unit.Equals(SizeSuffixes[i], StringComparison.OrdinalIgnoreCase)
                || unit.Equals(SizeSuffixes[i][..1], StringComparison.OrdinalIgnoreCase))
            {
                multiplier = Math.Pow(1024, i);
                break;
            }
        }

        if (double.IsNaN(value) || double.IsInfinity(value) || value < 0)
        {
            return false;
        }

        bytes = (long)(value * multiplier);
        return true;
    }

    public static bool TryParseSpeed(string? text, out double bytesPerSecond)
    {
        bytesPerSecond = 0;
        if (!TryParseSize(text, out var bytes) || bytes <= 0)
        {
            return false;
        }

        bytesPerSecond = bytes;
        return true;
    }

    public static int? TryParseEtaSeconds(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var cleaned = text.Trim();
        if (cleaned.Equals("NA", StringComparison.OrdinalIgnoreCase)
            || cleaned.Equals("N/A", StringComparison.OrdinalIgnoreCase)
            || cleaned.Contains("Unknown", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var parts = cleaned.Split(':');
        if (parts.Length is < 2 or > 3)
        {
            return null;
        }

        var total = 0;
        foreach (var part in parts)
        {
            if (!int.TryParse(part, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) || value < 0)
            {
                return null;
            }

            total = (total * 60) + value;
        }

        return total;
    }
}
