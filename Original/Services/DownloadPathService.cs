using VideoDownloader.Configuration;
using VideoDownloader.Services;
namespace VideoDownloader.Services;

public interface IDownloadPathService
{
    string DefaultDirectory { get; }
    Task<string> EnsureDirectoryAsync(string path, CancellationToken ct = default);
}

public sealed class DownloadPathService : IDownloadPathService
{
    private readonly AppPaths _paths;
    public DownloadPathService(AppPaths paths) => _paths = paths;

    public string DefaultDirectory => _paths.DefaultDownloadDirectory;

    public async Task<string> EnsureDirectoryAsync(string path, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(path)) path = DefaultDirectory;
        await Task.Run(() => Directory.CreateDirectory(path), ct).ConfigureAwait(false);
        return path;
    }
}
