namespace VideoDownloader.Helpers;

/// <summary>Human readable byte and speed formatting shared by the UI models.</summary>
public static class ByteFormatter
{
    private static readonly string[] Units = ["B", "KB", "MB", "GB", "TB"];

    public static string Format(long bytes)
    {
        if (bytes <= 0)
        {
            return "0 B";
        }

        double value = bytes;
        var unit = 0;
        while (value >= 1024 && unit < Units.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        return $"{value:0.#} {Units[unit]}";
    }

    public static string FormatSpeed(double bytesPerSecond)
        => $"{Format((long)Math.Max(0, bytesPerSecond))}/s";
}
