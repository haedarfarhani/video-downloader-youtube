using Microsoft.Extensions.Logging;
using VideoDownloader.Configuration;
using VideoDownloader.Data.Repositories;

namespace VideoDownloader.Services;

/// <summary>User configurable application settings.</summary>
public sealed class AppSettings
{
    public const int MinConcurrency = 1;
    public const int MaxConcurrency = 5;

    public string DefaultOutputDirectory { get; init; } = string.Empty;

    public int MaxConcurrentDownloads { get; init; } = 2;

    public static AppSettings CreateDefault(string defaultDirectory) => new()
    {
        DefaultOutputDirectory = defaultDirectory,
        MaxConcurrentDownloads = 2
    };

    public AppSettings Normalize(string fallbackDirectory) => new()
    {
        DefaultOutputDirectory = string.IsNullOrWhiteSpace(DefaultOutputDirectory)
            ? fallbackDirectory
            : DefaultOutputDirectory.Trim(),
        MaxConcurrentDownloads = Math.Clamp(MaxConcurrentDownloads, MinConcurrency, MaxConcurrency)
    };
}

public interface ISettingsService
{
    AppSettings Current { get; }

    Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default);
}

public sealed class SettingsService : ISettingsService
{
    private const string OutputDirectoryKey = "DefaultOutputDirectory";
    private const string ConcurrencyKey = "MaxConcurrentDownloads";

    private readonly ISettingsRepository _repository;
    private readonly AppPaths _paths;
    private readonly ILogger<SettingsService> _logger;

    public SettingsService(ISettingsRepository repository, AppPaths paths, ILogger<SettingsService> logger)
    {
        _repository = repository;
        _paths = paths;
        _logger = logger;
        Current = AppSettings.CreateDefault(_paths.DefaultDownloadDirectory);
    }

    public AppSettings Current { get; private set; }

    public async Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var values = await _repository.GetAllAsync(cancellationToken).ConfigureAwait(false);

            var directory = values.TryGetValue(OutputDirectoryKey, out var storedDirectory)
                ? storedDirectory
                : _paths.DefaultDownloadDirectory;

            var concurrency = values.TryGetValue(ConcurrencyKey, out var storedConcurrency)
                              && int.TryParse(storedConcurrency, out var parsed)
                ? parsed
                : 2;

            Current = new AppSettings
            {
                DefaultOutputDirectory = directory,
                MaxConcurrentDownloads = concurrency
            }.Normalize(_paths.DefaultDownloadDirectory);

            Directory.CreateDirectory(Current.DefaultOutputDirectory);
            return Current;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not load settings; falling back to defaults");
            Current = AppSettings.CreateDefault(_paths.DefaultDownloadDirectory);
            return Current;
        }
    }

    public async Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var normalized = settings.Normalize(_paths.DefaultDownloadDirectory);

        if (string.IsNullOrWhiteSpace(normalized.DefaultOutputDirectory))
        {
            normalized = AppSettings.CreateDefault(_paths.DefaultDownloadDirectory);
        }

        Directory.CreateDirectory(normalized.DefaultOutputDirectory);

        await _repository.SetAsync(OutputDirectoryKey, normalized.DefaultOutputDirectory, cancellationToken).ConfigureAwait(false);
        await _repository.SetAsync(ConcurrencyKey, normalized.MaxConcurrentDownloads.ToString(), cancellationToken).ConfigureAwait(false);

        Current = normalized;
        _logger.LogInformation(
            "Settings saved. Output directory: {Directory}, concurrency: {Concurrency}",
            normalized.DefaultOutputDirectory,
            normalized.MaxConcurrentDownloads);
    }
}
