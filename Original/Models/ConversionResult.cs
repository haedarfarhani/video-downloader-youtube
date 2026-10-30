namespace VideoDownloader.Models;

public sealed class ConversionProgress
{
    public double Percent { get; init; }
    public long DownloadedBytes { get; init; }
    public long TotalBytes { get; init; }
    public double SpeedBytesPerSecond { get; init; }
    public int EtaSeconds { get; init; }
}

public enum ConversionStatus { Queued, Converting, Completed, Failed, Cancelled }

public sealed class ConversionResult
{
    public ConversionStatus Status { get; init; } = ConversionStatus.Queued;
    public double ProgressPercent { get; init; } = 0;
    public string? ErrorSummary { get; init; }
    public string? OutputFilePath { get; init; }
    public long DownloadedBytes { get; init; }
    public long TotalBytes { get; init; }
    public string? SpeedText { get; init; }

    public static ConversionResult Completed() => new() { Status = ConversionStatus.Completed };
    public static ConversionResult Failed(string error) => new() { Status = ConversionStatus.Failed, ErrorSummary = error };
}
