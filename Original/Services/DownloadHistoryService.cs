using Microsoft.Extensions.Logging;
using VideoDownloader.Data.Repositories;
using VideoDownloader.Models;

namespace VideoDownloader.Services;

/// <summary>Stores completed, failed and cancelled downloads in the local SQLite database.</summary>
public interface IDownloadHistoryService
{
    Task AddAsync(DownloadHistoryRecord record, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DownloadHistoryRecord>> GetAsync(int limit = 200, int offset = 0, CancellationToken cancellationToken = default);

    Task<int> GetCountAsync(CancellationToken cancellationToken = default);

    Task DeleteAsync(string id, CancellationToken cancellationToken = default);

    Task ClearAsync(CancellationToken cancellationToken = default);
}

public sealed class DownloadHistoryService : IDownloadHistoryService
{
    private readonly IDownloadHistoryRepository _repository;
    private readonly ILogger<DownloadHistoryService> _logger;

    public DownloadHistoryService(IDownloadHistoryRepository repository, ILogger<DownloadHistoryService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task AddAsync(DownloadHistoryRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        try
        {
            await _repository.AddAsync(record, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not persist a history record for {Url}", record.Url);
        }
    }

    public async Task<IReadOnlyList<DownloadHistoryRecord>> GetAsync(
        int limit = 200,
        int offset = 0,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _repository.GetAllAsync(Math.Clamp(limit, 1, 1000), Math.Max(0, offset), cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not read the download history");
            return Array.Empty<DownloadHistoryRecord>();
        }
    }

    public async Task<int> GetCountAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _repository.GetCountAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not count the download history");
            return 0;
        }
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            await _repository.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not delete history record {Id}", id);
        }
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _repository.ClearAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not clear the download history");
        }
    }
}
