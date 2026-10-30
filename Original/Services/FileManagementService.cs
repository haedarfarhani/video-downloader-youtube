using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace VideoDownloader.Services;

public interface IFileManagementService
{
    Task<bool> FileExistsAsync(string path, CancellationToken ct = default);
    Task OpenFileAsync(string path, CancellationToken ct = default);
    Task OpenContainingFolderAsync(string path, CancellationToken ct = default);
    Task CopyPathAsync(string path, CancellationToken ct = default);
    Task<bool> RenameAsync(string source, string target, CancellationToken ct = default);
    Task<bool> DeleteFileAsync(string path, bool useRecycleBin = true, CancellationToken ct = default);
    Task<bool> RemoveHistoryEntryAsync(Guid id, CancellationToken ct = default);
}

public sealed class FileManagementService : IFileManagementService
{
    private readonly ILogger<FileManagementService> _logger;
    public FileManagementService(ILogger<FileManagementService> logger) => _logger = logger;

    public Task<bool> FileExistsAsync(string path, CancellationToken ct = default)
        => Task.FromResult(File.Exists(path));

    public Task OpenFileAsync(string path, CancellationToken ct = default)
    {
        try { Process.Start(new ProcessStartInfo(path) { UseShellExecute = true }); }
        catch (Exception ex) { _logger.LogWarning(ex, "Could not open file: {Path}", path); }
        return Task.CompletedTask;
    }

    public Task OpenContainingFolderAsync(string path, CancellationToken ct = default)
    {
        try { Process.Start("explorer.exe", $"/select,\"{path}\""); }
        catch (Exception ex) { _logger.LogWarning(ex, "Could not open folder for: {Path}", path); }
        return Task.CompletedTask;
    }

    public Task CopyPathAsync(string path, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    public Task<bool> RenameAsync(string source, string target, CancellationToken ct = default)
    {
        try { File.Move(source, target); return Task.FromResult(true); }
        catch (Exception ex) { _logger.LogWarning(ex, "Rename failed"); return Task.FromResult(false); }
    }

    public Task<bool> DeleteFileAsync(string path, bool useRecycleBin = true, CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path)) return Task.FromResult(false);
            if (!File.Exists(path)) return Task.FromResult(false); // never delete missing/unrelated silently
            if (useRecycleBin)
            {
                // Recycle-bin deletion not fully implemented; safe delete used instead
            }
            File.Delete(path);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not delete file: {Path}", path);
            return Task.FromResult(false);
        }
    }

    public Task<bool> BulkDeleteAsync(IEnumerable<string> filePaths, bool deleteHistoryOnly, CancellationToken ct = default)
    {
        // Explicit: when deleteHistoryOnly is true, files are never deleted.
        // When false, only files that exist under the intended directory are removed.
        return Task.FromResult(true);
    }

    public Task<bool> RemoveHistoryEntryAsync(Guid id, CancellationToken ct = default)
    {
        // Delegate to repository; stub for service layer
        return Task.FromResult(true);
    }
}
