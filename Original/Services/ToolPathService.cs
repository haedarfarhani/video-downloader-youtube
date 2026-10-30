using Microsoft.Extensions.Logging;
using VideoDownloader.Helpers;

namespace VideoDownloader.Services;

/// <summary>Provides the resolved locations of the bundled yt-dlp and FFmpeg executables.</summary>
public interface IToolPathService
{
    ToolSet ToolSet { get; }

    string ToolsDirectory { get; }

    string YtDlpPath { get; }

    string FFmpegPath { get; }

    string FFprobePath { get; }
}

public sealed class ToolPathService : IToolPathService
{
    private readonly ILogger<ToolPathService> _logger;

    public ToolPathService(ILogger<ToolPathService> logger)
    {
        _logger = logger;
        ToolSet = ToolPathResolver.Resolve(AppContext.BaseDirectory);
        _logger.LogInformation("Resolved tools directory: {ToolsDirectory}", ToolSet.ToolsDirectory);
    }

    public ToolSet ToolSet { get; }

    public string ToolsDirectory => ToolSet.ToolsDirectory;

    public string YtDlpPath => ToolSet.YtDlpPath;

    public string FFmpegPath => ToolSet.FFmpegPath;

    public string FFprobePath => ToolSet.FFprobePath;
}
