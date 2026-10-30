using System.Diagnostics;
using Microsoft.Extensions.Logging;
using VideoDownloader.Helpers;
using VideoDownloader.Models;

namespace VideoDownloader.Services;

/// <summary>Result of a metadata query.</summary>
public sealed class MediaInfoResult
{
    public MediaInfo? Media { get; init; }

    public string? ErrorMessage { get; init; }

    public bool Success => Media is not null;

    public static MediaInfoResult Ok(MediaInfo media) => new() { Media = media };

    public static MediaInfoResult Failed(string error) => new() { ErrorMessage = error };
}

public interface IYtDlpService
{
    Task<MediaInfoResult> GetMediaInfoAsync(string url, bool includePlaylist, CancellationToken cancellationToken = default);

    Task<DownloadResult> DownloadAsync(
        DownloadOptions options,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default);

    Task<string?> GetVersionAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Wraps the bundled yt-dlp executable. All arguments are passed through
/// <see cref="ProcessStartInfo.ArgumentList"/>; no command line string is ever built from user input.
/// </summary>
public sealed class YtDlpService : IYtDlpService
{
    private static readonly TimeSpan MediaInfoTimeout = TimeSpan.FromMinutes(3);
    private static readonly TimeSpan VersionTimeout = TimeSpan.FromSeconds(20);

    /// <summary>
    /// yt-dlp embeds CPython, which falls back to the system code page when its output is
    /// redirected. Titles and file paths with non-Latin characters would otherwise arrive as
    /// question marks and the final file could not be located. Force UTF-8 on the child process.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, string> Utf8Environment = new Dictionary<string, string>
    {
        ["PYTHONUTF8"] = "1",
        ["PYTHONIOENCODING"] = "utf-8"
    };

    private static readonly string[] MediaExtensions =
        [".mp4", ".mkv", ".webm", ".mov", ".avi", ".mp3", ".m4a", ".opus", ".flac", ".wav"];

    private readonly IProcessRunner _processRunner;
    private readonly IToolPathService _toolPaths;
    private readonly IToolValidationService _toolValidation;
    private readonly ILogger<YtDlpService> _logger;

    public YtDlpService(
        IProcessRunner processRunner,
        IToolPathService toolPaths,
        IToolValidationService toolValidation,
        ILogger<YtDlpService> logger)
    {
        _processRunner = processRunner;
        _toolPaths = toolPaths;
        _toolValidation = toolValidation;
        _logger = logger;
    }

    public async Task<MediaInfoResult> GetMediaInfoAsync(string url, bool includePlaylist, CancellationToken cancellationToken = default)
    {
        if (!UrlValidator.TryValidate(url, out _, out var urlError))
        {
            return MediaInfoResult.Failed(urlError ?? "The URL is not valid.");
        }

        if (!File.Exists(_toolPaths.YtDlpPath))
        {
            return MediaInfoResult.Failed(MissingToolMessage());
        }

        var arguments = YtDlpArgumentBuilder.BuildMediaInfoArgs(url, includePlaylist);
        var result = await _processRunner
            .RunAsync(
                _toolPaths.YtDlpPath,
                arguments,
                timeout: MediaInfoTimeout,
                cancellationToken: cancellationToken,
                environment: Utf8Environment)
            .ConfigureAwait(false);

        if (result.Cancelled)
        {
            return MediaInfoResult.Failed("The metadata request was cancelled.");
        }

        if (!result.Started)
        {
            return MediaInfoResult.Failed(result.StartError ?? "yt-dlp could not be started.");
        }

        if (result.ExitCode != 0)
        {
            var diagnostics = result.Diagnostics;
            _logger.LogWarning("yt-dlp metadata query failed for {Url} with exit code {ExitCode}: {Diagnostics}", url, result.ExitCode, diagnostics);
            return MediaInfoResult.Failed(
                string.IsNullOrWhiteSpace(diagnostics)
                    ? $"yt-dlp could not analyse the URL (exit code {result.ExitCode})."
                    : $"yt-dlp could not analyse the URL: {diagnostics}");
        }

        try
        {
            var media = MediaInfoParser.Parse(result.StandardOutput);
            return MediaInfoResult.Ok(media);
        }
        catch (Exception ex) when (ex is FormatException or System.Text.Json.JsonException)
        {
            _logger.LogError(ex, "Failed to parse yt-dlp metadata for {Url}", url);
            return MediaInfoResult.Failed("yt-dlp returned metadata that could not be parsed.");
        }
    }

    public async Task<DownloadResult> DownloadAsync(
        DownloadOptions options,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!UrlValidator.TryValidate(options.Url, out _, out var urlError))
        {
            return DownloadResult.Failed(urlError ?? "The URL is not valid.");
        }

        if (string.IsNullOrWhiteSpace(options.OutputDirectory))
        {
            return DownloadResult.Failed("No output directory was selected.");
        }

        if (!File.Exists(_toolPaths.YtDlpPath))
        {
            return DownloadResult.Failed(MissingToolMessage());
        }

        var report = await _toolValidation.EnsureReadyAsync(cancellationToken).ConfigureAwait(false);
        if (!report.IsReady)
        {
            return DownloadResult.Failed(report.Summary);
        }

        try
        {
            Directory.CreateDirectory(options.OutputDirectory);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return DownloadResult.Failed($"The output directory cannot be used: {ex.Message}");
        }

        var finalPathFile = Path.Combine(Path.GetTempPath(), $"video-downloader-{Guid.NewGuid():N}.path");
        var arguments = YtDlpArgumentBuilder.BuildDownloadArgs(
            options,
            Path.GetDirectoryName(_toolPaths.FFmpegPath),
            finalPathFile);
        string? finalPath = null;
        var startedAt = Stopwatch.StartNew();
        var startedAtWallClock = DateTime.UtcNow;

        var runResult = await _processRunner
            .RunAsync(
                _toolPaths.YtDlpPath,
                arguments,
                line =>
                {
                    if (YtDlpProgressParser.TryParseFinalPath(line, out var parsedPath))
                    {
                        finalPath = parsedPath;
                        return;
                    }

                    if (progress is not null && YtDlpProgressParser.TryParseProgress(line, out var update))
                    {
                        progress.Report(new DownloadProgress(
                            update.Percent,
                            update.DownloadedBytes,
                            update.TotalBytes,
                            update.SpeedBytesPerSecond,
                            update.EtaSeconds));
                    }
                },
                cancellationToken: cancellationToken,
                environment: Utf8Environment)
            .ConfigureAwait(false);

        startedAt.Stop();

        // Prefer the UTF-8 path written to the side file; the console line is only a fallback.
        TryReadFinalPathFile(finalPathFile, ref finalPath);
        TryDeleteFile(finalPathFile);

        if (runResult.Cancelled)
        {
            _logger.LogInformation("Download cancelled for {Url}", options.Url);
            return DownloadResult.Cancelled();
        }

        if (!runResult.Started)
        {
            return DownloadResult.Failed(runResult.StartError ?? "yt-dlp could not be started.");
        }

        if (runResult.TimedOut)
        {
            return DownloadResult.Failed("yt-dlp did not finish in time.", runResult.ExitCode, runResult.Diagnostics);
        }

        if (runResult.ExitCode != 0)
        {
            var diagnostics = runResult.Diagnostics;
            _logger.LogWarning("Download failed for {Url} with exit code {ExitCode}: {Diagnostics}", options.Url, runResult.ExitCode, diagnostics);
            return DownloadResult.Failed(
                string.IsNullOrWhiteSpace(diagnostics)
                    ? $"yt-dlp exited with code {runResult.ExitCode}."
                    : diagnostics,
                runResult.ExitCode,
                diagnostics);
        }

        var file = ResolveOutputFile(finalPath, options.OutputDirectory, startedAtWallClock);
        if (file is null)
        {
            _logger.LogWarning(
                "yt-dlp reported success but no output file could be located for {Url}. Reported path: {ReportedPath}",
                options.Url,
                finalPath ?? "(none)");
            return DownloadResult.Failed(
                string.IsNullOrWhiteSpace(finalPath)
                    ? "yt-dlp finished but did not report the downloaded file path. The file may have been skipped."
                    : $"The download finished but the output file is missing: {finalPath}",
                runResult.ExitCode,
                runResult.Diagnostics);
        }

        if (file.Length <= 0)
        {
            return DownloadResult.Failed($"The downloaded file is empty: {file.FullName}", runResult.ExitCode, runResult.Diagnostics);
        }

        _logger.LogInformation(
            "Download completed: {Title} -> {Path} ({Size} bytes) in {Duration}",
            options.Title ?? options.Url,
            file.FullName,
            file.Length,
            startedAt.Elapsed);

        return DownloadResult.Succeeded(file.FullName, file.Length, ProcessOutputTail.Get(runResult.StandardError), startedAt.Elapsed);
    }

    public async Task<string?> GetVersionAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_toolPaths.YtDlpPath))
        {
            return null;
        }

        var result = await _processRunner
            .RunAsync(
                _toolPaths.YtDlpPath,
                ["--version"],
                timeout: VersionTimeout,
                cancellationToken: cancellationToken,
                environment: Utf8Environment)
            .ConfigureAwait(false);

        return result.ExitCode == 0 ? result.StandardOutput.Trim() : null;
    }

    private void TryReadFinalPathFile(string pathFile, ref string? finalPath)
    {
        try
        {
            if (!File.Exists(pathFile))
            {
                return;
            }

            var lines = File.ReadAllLines(pathFile, System.Text.Encoding.UTF8)
                .Select(line => line.Trim().Trim('"'))
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .ToList();

            if (lines.Count > 0)
            {
                finalPath = lines[^1];
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(ex, "Could not read the final path file {Path}", pathFile);
        }
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Best effort: the file lives in the temporary directory.
        }
    }

    /// <summary>
    /// Returns the file yt-dlp produced. The path printed by <c>--print after_move:filepath</c> is
    /// used when it exists; otherwise the newest media file in the output directory that was
    /// written during this run is used, which covers post-processing that changed the extension.
    /// </summary>
    private FileInfo? ResolveOutputFile(string? reportedPath, string outputDirectory, DateTime startedAtUtc)
    {
        if (!string.IsNullOrWhiteSpace(reportedPath))
        {
            try
            {
                var reported = new FileInfo(reportedPath);
                if (reported.Exists)
                {
                    return reported;
                }
            }
            catch (Exception ex) when (ex is ArgumentException or IOException or UnauthorizedAccessException or NotSupportedException)
            {
                _logger.LogWarning(ex, "The output path reported by yt-dlp is invalid: {Path}", reportedPath);
            }
        }

        try
        {
            var directory = new DirectoryInfo(outputDirectory);
            if (!directory.Exists)
            {
                return null;
            }

            var earliest = startedAtUtc.AddMinutes(-1);
            var candidate = directory
                .EnumerateFiles("*", SearchOption.TopDirectoryOnly)
                .Where(f => MediaExtensions.Contains(f.Extension, StringComparer.OrdinalIgnoreCase))
                .Where(f => f.LastWriteTimeUtc >= earliest)
                .OrderByDescending(f => f.LastWriteTimeUtc)
                .FirstOrDefault();

            if (candidate is not null)
            {
                _logger.LogInformation(
                    "Resolved the output file by scanning {Directory}: {Path}",
                    outputDirectory,
                    candidate.FullName);
            }

            return candidate;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(ex, "Could not scan {Directory} for the downloaded file", outputDirectory);
            return null;
        }
    }

    private string MissingToolMessage()
        => $"yt-dlp was not found at {_toolPaths.YtDlpPath}. Run scripts\\Setup-Tools.ps1 from the repository root to install it.";
}
