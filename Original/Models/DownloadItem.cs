using CommunityToolkit.Mvvm.ComponentModel;
using VideoDownloader.Helpers;

namespace VideoDownloader.Models;

/// <summary>Lifecycle of a queued download.</summary>
public enum DownloadState
{
    Pending = 0,
    Downloading = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4
}

/// <summary>
/// A single item tracked by the download queue. The item raises change notifications
/// so the UI can observe progress without polling.
/// </summary>
public sealed partial class DownloadItem : ObservableObject
{
    public DownloadItem()
    {
        Title = string.Empty;
        State = DownloadState.Pending;
    }

    public string Id { get; init; } = Guid.NewGuid().ToString("N");

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    public DownloadMode Mode { get; init; } = DownloadMode.Video;

    public string Url { get; init; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowUrl))]
    public partial string Title { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StateText))]
    [NotifyPropertyChangedFor(nameof(IsActive))]
    [NotifyPropertyChangedFor(nameof(CanRetry))]
    [NotifyPropertyChangedFor(nameof(CanCancel))]
    [NotifyPropertyChangedFor(nameof(ShowError))]
    public partial DownloadState State { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProgressText))]
    public partial double ProgressPercent { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProgressText))]
    [NotifyPropertyChangedFor(nameof(SizeText))]
    public partial long DownloadedBytes { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SizeText))]
    public partial long TotalBytes { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SpeedText))]
    public partial double SpeedBytesPerSecond { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EtaText))]
    public partial int EtaSeconds { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowError))]
    [NotifyPropertyChangedFor(nameof(ErrorMessageText))]
    public partial string? ErrorMessage { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(OutputPathText))]
    public partial string? OutputPath { get; set; }

    [ObservableProperty]
    public partial int RetryCount { get; set; }

    [ObservableProperty]
    public partial DateTime? CompletedAt { get; set; }

    public string ProgressText => ProgressPercent <= 0
        ? "Waiting for media..."
        : $"{ProgressPercent:F1}%";

    public string StateText => State switch
    {
        DownloadState.Pending => "Pending",
        DownloadState.Downloading => "Downloading",
        DownloadState.Completed => "Completed",
        DownloadState.Failed => "Failed",
        DownloadState.Cancelled => "Cancelled",
        _ => "Unknown"
    };

    public string SizeText => TotalBytes > 0
        ? $"{ByteFormatter.Format(DownloadedBytes)} of {ByteFormatter.Format(TotalBytes)}"
        : DownloadedBytes > 0
            ? ByteFormatter.Format(DownloadedBytes)
            : string.Empty;

    public string SpeedText => SpeedBytesPerSecond > 0
        ? ByteFormatter.FormatSpeed(SpeedBytesPerSecond)
        : string.Empty;

    public string EtaText => EtaSeconds > 0
        ? $"Remaining {TimeSpan.FromSeconds(EtaSeconds):hh\\:mm\\:ss}"
        : string.Empty;

    /// <summary>False when the title already is the URL, so the URL is not shown twice.</summary>
    public bool ShowUrl => !string.Equals(Title, Url, StringComparison.OrdinalIgnoreCase);

    public string ErrorMessageText => ErrorMessage ?? string.Empty;

    public string OutputPathText => OutputPath ?? string.Empty;

    public bool IsActive => State == DownloadState.Downloading;

    public bool CanRetry => State is DownloadState.Failed or DownloadState.Cancelled;

    public bool CanCancel => State is DownloadState.Pending or DownloadState.Downloading;

    public bool ShowError => State == DownloadState.Failed && !string.IsNullOrWhiteSpace(ErrorMessage);

    /// <summary>Applies a progress snapshot; must be called on the UI thread.</summary>
    public void ApplyProgress(DownloadProgress progress)
    {
        if (progress.Percent is { } percent)
        {
            ProgressPercent = percent;
        }

        if (progress.DownloadedBytes is { } downloaded)
        {
            DownloadedBytes = downloaded;
        }

        if (progress.TotalBytes is { } total && total > 0)
        {
            TotalBytes = total;
        }

        if (progress.SpeedBytesPerSecond is { } speed)
        {
            SpeedBytesPerSecond = speed;
        }

        if (progress.EtaSeconds is { } eta)
        {
            EtaSeconds = eta;
        }
    }
}

/// <summary>A single progress snapshot reported by yt-dlp.</summary>
public readonly record struct DownloadProgress(
    double? Percent,
    long? DownloadedBytes,
    long? TotalBytes,
    double? SpeedBytesPerSecond,
    int? EtaSeconds);
