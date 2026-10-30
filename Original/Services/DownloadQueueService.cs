using Microsoft.Extensions.Logging;
using VideoDownloader.Models;

namespace VideoDownloader.Services;

/// <summary>
/// Owns the in-memory download queue. Queue state is kept separate from persisted history;
/// completed, failed and cancelled items are written to the history service.
/// </summary>
public interface IDownloadQueueService
{
    IReadOnlyList<DownloadItem> Items { get; }

    int MaxConcurrentDownloads { get; }

    event EventHandler<DownloadItem>? ItemAdded;

    event EventHandler<DownloadItem>? ItemUpdated;

    event EventHandler<DownloadItem>? ItemRemoved;

    DownloadItem Enqueue(DownloadOptions options);

    void Cancel(DownloadItem item);

    void CancelAll();

    void Retry(DownloadItem item);

    void Remove(DownloadItem item);

    void ClearFinished();

    void SetConcurrency(int value);

    Task StopAsync();
}

public sealed class DownloadQueueService : IDownloadQueueService, IAsyncDisposable
{
    private readonly IYtDlpService _ytDlpService;
    private readonly IToolValidationService _toolValidation;
    private readonly IDownloadHistoryService _historyService;
    private readonly IUiDispatcher _uiDispatcher;
    private readonly ILogger<DownloadQueueService> _logger;

    private readonly object _sync = new();
    private readonly List<DownloadItem> _items = [];
    private readonly Dictionary<string, DownloadOptions> _options = [];
    private readonly Dictionary<string, CancellationTokenSource> _cancellations = [];
    private readonly HashSet<string> _cancelRequested = [];
    private readonly HashSet<string> _active = [];
    private readonly CancellationTokenSource _shutdown = new();

    private SemaphoreSlim _gate;
    private readonly List<SemaphoreSlim> _retiredGates = [];
    private int _concurrency;
    private bool _disposed;

    public DownloadQueueService(
        IYtDlpService ytDlpService,
        IToolValidationService toolValidation,
        IDownloadHistoryService historyService,
        ISettingsService settingsService,
        IUiDispatcher uiDispatcher,
        ILogger<DownloadQueueService> logger)
    {
        _ytDlpService = ytDlpService;
        _toolValidation = toolValidation;
        _historyService = historyService;
        _uiDispatcher = uiDispatcher;
        _logger = logger;

        _concurrency = Math.Clamp(settingsService.Current.MaxConcurrentDownloads, 1, 5);
        _gate = new SemaphoreSlim(_concurrency, _concurrency);
    }

    public event EventHandler<DownloadItem>? ItemAdded;

    public event EventHandler<DownloadItem>? ItemUpdated;

    public event EventHandler<DownloadItem>? ItemRemoved;

    public IReadOnlyList<DownloadItem> Items
    {
        get
        {
            lock (_sync)
            {
                return _items.ToArray();
            }
        }
    }

    public int MaxConcurrentDownloads
    {
        get
        {
            lock (_sync)
            {
                return _concurrency;
            }
        }
    }

    public DownloadItem Enqueue(DownloadOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ObjectDisposedException.ThrowIf(_disposed, this);

        var item = new DownloadItem
        {
            Url = options.Url,
            Mode = options.Mode,
            Title = string.IsNullOrWhiteSpace(options.Title) ? options.Url : options.Title!
        };

        lock (_sync)
        {
            _items.Add(item);
            _options[item.Id] = options;
        }

        _uiDispatcher.Post(() => ItemAdded?.Invoke(this, item));
        _logger.LogInformation("Queued download {Id} ({Mode})", item.Id, options.Mode);

        _ = RunItemAsync(item);
        return item;
    }

    public void Cancel(DownloadItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        CancellationTokenSource? cts;
        lock (_sync)
        {
            _cancelRequested.Add(item.Id);
            _cancellations.TryGetValue(item.Id, out cts);
        }

        if (cts is not null)
        {
            try
            {
                cts.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }
        }
        else if (item.State == DownloadState.Pending)
        {
            // Not started yet: nothing to kill, mark it cancelled and stop it from starting.
            PublishUpdate(item, i =>
            {
                i.State = DownloadState.Cancelled;
                i.ErrorMessage = "The download was cancelled before it started.";
            });
        }
    }

    public void CancelAll()
    {
        List<DownloadItem> pending;
        List<CancellationTokenSource> sources;

        lock (_sync)
        {
            pending = _items.Where(item => item.State is DownloadState.Pending or DownloadState.Downloading).ToList();
            foreach (var item in pending)
            {
                _cancelRequested.Add(item.Id);
            }

            sources = _cancellations.Values.ToList();
        }

        foreach (var source in sources)
        {
            try
            {
                source.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }
        }

        foreach (var item in pending.Where(item => item.State == DownloadState.Pending))
        {
            PublishUpdate(item, i =>
            {
                i.State = DownloadState.Cancelled;
                i.ErrorMessage = "The download was cancelled before it started.";
            });
        }

        _logger.LogInformation("Cancellation requested for {Count} queue item(s)", pending.Count);
    }

    public void Retry(DownloadItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (!item.CanRetry)
        {
            return;
        }

        lock (_sync)
        {
            if (!_options.ContainsKey(item.Id))
            {
                _logger.LogWarning("Cannot retry {Id}: the original options are no longer available", item.Id);
                return;
            }

            _cancelRequested.Remove(item.Id);
        }

        PublishUpdate(item, i =>
        {
            i.RetryCount++;
            i.State = DownloadState.Pending;
            i.ErrorMessage = null;
            i.CompletedAt = null;
            i.ProgressPercent = 0;
            i.DownloadedBytes = 0;
            i.TotalBytes = 0;
            i.SpeedBytesPerSecond = 0;
            i.EtaSeconds = 0;
        });

        _ = RunItemAsync(item);
    }

    public void Remove(DownloadItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        bool active;
        lock (_sync)
        {
            active = _active.Contains(item.Id);
        }

        if (active)
        {
            Cancel(item);
            return;
        }

        lock (_sync)
        {
            _items.Remove(item);
            _options.Remove(item.Id);
            _cancelRequested.Remove(item.Id);
        }

        _uiDispatcher.Post(() => ItemRemoved?.Invoke(this, item));
    }

    public void ClearFinished()
    {
        List<DownloadItem> removed;
        lock (_sync)
        {
            removed = _items
                .Where(item => item.State is DownloadState.Completed or DownloadState.Failed or DownloadState.Cancelled)
                .ToList();

            foreach (var item in removed)
            {
                _items.Remove(item);
                _options.Remove(item.Id);
                _cancelRequested.Remove(item.Id);
            }
        }

        foreach (var item in removed)
        {
            _uiDispatcher.Post(() => ItemRemoved?.Invoke(this, item));
        }

        if (removed.Count > 0)
        {
            _logger.LogInformation("Removed {Count} finished item(s) from the queue", removed.Count);
        }
    }

    public void SetConcurrency(int value)
    {
        var clamped = Math.Clamp(value, 1, 5);

        lock (_sync)
        {
            if (clamped == _concurrency)
            {
                return;
            }

            var previous = _gate;
            _gate = new SemaphoreSlim(clamped, clamped);
            _retiredGates.Add(previous);
            _concurrency = clamped;
        }

        _logger.LogInformation("Download concurrency set to {Concurrency}", clamped);
    }

    public Task StopAsync()
    {
        try
        {
            _shutdown.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }

        CancelAll();
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return ValueTask.CompletedTask;
        }

        _disposed = true;

        try
        {
            _shutdown.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }

        lock (_sync)
        {
            foreach (var source in _cancellations.Values)
            {
                source.Dispose();
            }

            _cancellations.Clear();
        }

        _shutdown.Dispose();
        _gate.Dispose();

        foreach (var gate in _retiredGates)
        {
            gate.Dispose();
        }

        _retiredGates.Clear();
        return ValueTask.CompletedTask;
    }

    private async Task RunItemAsync(DownloadItem item)
    {
        SemaphoreSlim gate;
        lock (_sync)
        {
            gate = _gate;
        }

        var acquired = false;
        CancellationTokenSource? cts = null;

        try
        {
            await gate.WaitAsync(_shutdown.Token).ConfigureAwait(false);
            acquired = true;

            DownloadOptions? options;
            lock (_sync)
            {
                _options.TryGetValue(item.Id, out options);
                _active.Add(item.Id);
            }

            if (options is null)
            {
                return;
            }

            if (_shutdown.IsCancellationRequested)
            {
                PublishUpdate(item, i =>
                {
                    i.State = DownloadState.Cancelled;
                    i.ErrorMessage = "The application is closing.";
                });
                return;
            }

            cts = CancellationTokenSource.CreateLinkedTokenSource(_shutdown.Token);
            bool alreadyCancelled;
            lock (_sync)
            {
                _cancellations[item.Id] = cts;
                alreadyCancelled = _cancelRequested.Contains(item.Id);
            }

            if (alreadyCancelled)
            {
                PublishUpdate(item, i =>
                {
                    i.State = DownloadState.Cancelled;
                    i.ErrorMessage = "The download was cancelled before it started.";
                });
                await RecordHistoryAsync(options, item, DownloadState.Cancelled, null, 0, "Cancelled before the download started.").ConfigureAwait(false);
                return;
            }

            var report = await _toolValidation.EnsureReadyAsync(cts.Token).ConfigureAwait(false);

            if (cts.IsCancellationRequested)
            {
                PublishUpdate(item, i =>
                {
                    i.State = DownloadState.Cancelled;
                    i.ErrorMessage = "The download was cancelled.";
                });
                await RecordHistoryAsync(options, item, DownloadState.Cancelled, null, 0, "Cancelled by the user.").ConfigureAwait(false);
                return;
            }

            if (!report.IsReady)
            {
                PublishUpdate(item, i =>
                {
                    i.State = DownloadState.Failed;
                    i.ErrorMessage = report.Summary;
                });
                await RecordHistoryAsync(options, item, DownloadState.Failed, null, 0, report.Summary).ConfigureAwait(false);
                return;
            }

            PublishUpdate(item, i =>
            {
                i.State = DownloadState.Downloading;
                i.ErrorMessage = null;
                i.CompletedAt = null;
            });

            var progress = new Progress<DownloadProgress>(update => PublishUpdate(item, i => i.ApplyProgress(update)));

            var result = await _ytDlpService.DownloadAsync(options, progress, cts.Token).ConfigureAwait(false);

            switch (result.Outcome)
            {
                case DownloadOutcome.Succeeded:
                    PublishUpdate(item, i =>
                    {
                        i.State = DownloadState.Completed;
                        i.ProgressPercent = 100;
                        i.TotalBytes = result.FileSizeBytes > 0 ? result.FileSizeBytes : i.TotalBytes;
                        if (i.TotalBytes > 0)
                        {
                            // yt-dlp does not always print a byte count for fast downloads, but the
                            // finished file size is authoritative.
                            i.DownloadedBytes = i.TotalBytes;
                        }

                        i.OutputPath = result.OutputFilePath;
                        i.CompletedAt = DateTime.UtcNow;
                        i.SpeedBytesPerSecond = 0;
                        i.EtaSeconds = 0;
                    });
                    await RecordHistoryAsync(options, item, DownloadState.Completed, result.OutputFilePath, result.FileSizeBytes, null).ConfigureAwait(false);
                    break;

                case DownloadOutcome.Cancelled:
                    PublishUpdate(item, i =>
                    {
                        i.State = DownloadState.Cancelled;
                        i.ErrorMessage = "The download was cancelled.";
                        i.SpeedBytesPerSecond = 0;
                        i.EtaSeconds = 0;
                    });
                    await RecordHistoryAsync(options, item, DownloadState.Cancelled, null, 0, "Cancelled by the user.").ConfigureAwait(false);
                    break;

                default:
                    PublishUpdate(item, i =>
                    {
                        i.State = DownloadState.Failed;
                        i.ErrorMessage = result.ErrorMessage ?? "The download failed.";
                        i.SpeedBytesPerSecond = 0;
                        i.EtaSeconds = 0;
                    });
                    await RecordHistoryAsync(options, item, DownloadState.Failed, null, 0, result.ErrorMessage).ConfigureAwait(false);
                    break;
            }
        }
        catch (OperationCanceledException)
        {
            PublishUpdate(item, i =>
            {
                i.State = DownloadState.Cancelled;
                i.ErrorMessage = "The download was cancelled.";
                i.SpeedBytesPerSecond = 0;
                i.EtaSeconds = 0;
            });
            _logger.LogInformation("Download {Id} cancelled", item.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Download {Id} failed unexpectedly", item.Id);
            PublishUpdate(item, i =>
            {
                i.State = DownloadState.Failed;
                i.ErrorMessage = ex.Message;
                i.SpeedBytesPerSecond = 0;
                i.EtaSeconds = 0;
            });
        }
        finally
        {
            lock (_sync)
            {
                _active.Remove(item.Id);
                _cancellations.Remove(item.Id);
                _cancelRequested.Remove(item.Id);
            }

            cts?.Dispose();

            if (acquired)
            {
                try
                {
                    gate.Release();
                }
                catch (ObjectDisposedException)
                {
                }
            }
        }
    }

    private async Task RecordHistoryAsync(
        DownloadOptions options,
        DownloadItem item,
        DownloadState state,
        string? outputPath,
        long fileSizeBytes,
        string? errorMessage)
    {
        string title;
        lock (_sync)
        {
            title = item.Title;
        }

        var record = new DownloadHistoryRecord
        {
            Url = options.Url,
            Title = title,
            OutputPath = outputPath,
            Mode = options.Mode,
            State = state,
            StartedAt = item.CreatedAt,
            CompletedAt = state == DownloadState.Completed ? DateTime.UtcNow : null,
            FileSizeBytes = fileSizeBytes,
            ErrorMessage = errorMessage
        };

        await _historyService.AddAsync(record).ConfigureAwait(false);
    }

    /// <summary>Applies a mutation on the UI thread and raises the update event there.</summary>
    private void PublishUpdate(DownloadItem item, Action<DownloadItem> mutate)
    {
        _uiDispatcher.Post(() =>
        {
            if (_disposed)
            {
                return;
            }

            mutate(item);
            ItemUpdated?.Invoke(this, item);
        });
    }
}

/// <summary>Marshals calls onto the UI thread.</summary>
public interface IUiDispatcher
{
    bool HasThreadAccess { get; }

    void Post(Action action);
}
