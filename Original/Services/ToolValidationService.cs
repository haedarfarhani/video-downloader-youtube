using Microsoft.Extensions.Logging;
using VideoDownloader.Helpers;
using VideoDownloader.Models;

namespace VideoDownloader.Services;

/// <summary>Validates that the bundled command line tools exist and respond to their version argument.</summary>
public interface IToolValidationService
{
    ToolValidationReport LastReport { get; }

    Task<ToolValidationReport> ValidateAsync(CancellationToken cancellationToken = default);

    /// <summary>Validates the tools and returns a report suitable for blocking a download with a clear message.</summary>
    Task<ToolValidationReport> EnsureReadyAsync(CancellationToken cancellationToken = default);
}

public sealed class ToolValidationService : IToolValidationService
{
    private static readonly TimeSpan VersionTimeout = TimeSpan.FromSeconds(20);
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(30);

    private readonly IProcessRunner _processRunner;
    private readonly IToolPathService _toolPaths;
    private readonly ILogger<ToolValidationService> _logger;
    private ToolValidationReport _lastReport = ToolValidationReport.Empty;
    private DateTimeOffset _lastValidatedAt = DateTimeOffset.MinValue;
    private readonly object _validationLock = new();

    public ToolValidationService(
        IProcessRunner processRunner,
        IToolPathService toolPaths,
        ILogger<ToolValidationService> logger)
    {
        _processRunner = processRunner;
        _toolPaths = toolPaths;
        _logger = logger;
    }

    public ToolValidationReport LastReport => _lastReport;

    public async Task<ToolValidationReport> ValidateAsync(CancellationToken cancellationToken = default)
    {
        var tools = new[]
        {
            await ValidateToolAsync(ToolNames.YtDlp, _toolPaths.YtDlpPath, "--version", cancellationToken),
            await ValidateToolAsync(ToolNames.FFmpeg, _toolPaths.FFmpegPath, "-version", cancellationToken),
            await ValidateToolAsync(ToolNames.FFprobe, _toolPaths.FFprobePath, "-version", cancellationToken)
        };

        _lastReport = new ToolValidationReport { Tools = tools };
        _lastValidatedAt = DateTimeOffset.UtcNow;

        foreach (var tool in tools)
        {
            if (tool.IsReady)
            {
                _logger.LogInformation("{Tool} is ready: {Version} ({Path})", tool.Name, tool.Version, tool.Path);
            }
            else
            {
                _logger.LogWarning("{Tool} is not ready ({Path}): {Status}", tool.Name, tool.Path, tool.StatusText);
            }
        }

        return _lastReport;
    }

    public async Task<ToolValidationReport> EnsureReadyAsync(CancellationToken cancellationToken = default)
    {
        // A download start performs this check right before yt-dlp runs; reuse a recent result so
        // queued downloads do not spawn three extra processes each time.
        lock (_validationLock)
        {
            if (DateTimeOffset.UtcNow - _lastValidatedAt < CacheDuration)
            {
                return _lastReport;
            }
        }

        var report = await ValidateAsync(cancellationToken).ConfigureAwait(false);
        if (!report.IsReady)
        {
            _logger.LogError("Tool validation failed. {Summary}", report.Summary);
        }

        return report;
    }

    private async Task<ToolStatus> ValidateToolAsync(
        string name,
        string path,
        string versionArgument,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
        {
            return new ToolStatus
            {
                Name = name,
                Path = path,
                Exists = false,
                Responds = false,
                Error = "Not installed. Run scripts\\Setup-Tools.ps1 to download it."
            };
        }

        try
        {
            var result = await _processRunner
                .RunAsync(path, [versionArgument], timeout: VersionTimeout, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            var output = string.IsNullOrWhiteSpace(result.StandardOutput) ? result.StandardError : result.StandardOutput;
            var version = FirstMeaningfulLine(output);

            if (!result.Started)
            {
                return new ToolStatus
                {
                    Name = name,
                    Path = path,
                    Exists = true,
                    Responds = false,
                    Error = result.StartError ?? "The executable could not be started."
                };
            }

            if (result.ExitCode != 0)
            {
                return new ToolStatus
                {
                    Name = name,
                    Path = path,
                    Exists = true,
                    Responds = true,
                    Version = version,
                    Error = $"Version check exited with code {result.ExitCode}. {result.Diagnostics}".Trim()
                };
            }

            return new ToolStatus
            {
                Name = name,
                Path = path,
                Exists = true,
                Responds = true,
                Version = version
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate {Tool}", name);
            return new ToolStatus
            {
                Name = name,
                Path = path,
                Exists = true,
                Responds = false,
                Error = ex.Message
            };
        }
    }

    private static string FirstMeaningfulLine(string? output)
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            return "Unknown version";
        }

        var line = output
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(line))
        {
            return "Unknown version";
        }

        return line.Length <= 200 ? line : line[..200];
    }
}
