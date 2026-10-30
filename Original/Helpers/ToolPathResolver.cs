namespace VideoDownloader.Helpers;

/// <summary>The resolved locations of the bundled command line tools.</summary>
public sealed record ToolSet(string ToolsDirectory, string YtDlpPath, string FFmpegPath, string FFprobePath);

/// <summary>
/// Resolves the tools directory without hardcoding absolute paths:
/// 1. an explicitly configured directory (environment variable or settings),
/// 2. the application directory (<c>Tools</c> next to the executable / published layout),
/// 3. the application directory itself (legacy flat layout),
/// 4. ancestor directories of the executable (development builds inside the repository).
/// </summary>
public static class ToolPathResolver
{
    public const string EnvironmentVariableName = "VIDEO_DOWNLOADER_TOOLS_DIR";

    public const int MaxAncestorDepth = 8;

    public static ToolSet Resolve(string baseDirectory, string? configuredDirectory = null, string? environmentValue = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseDirectory);

        var basePath = Path.GetFullPath(baseDirectory);
        var candidates = new List<string>();

        if (!string.IsNullOrWhiteSpace(configuredDirectory))
        {
            candidates.Add(configuredDirectory!);
        }

        var environment = environmentValue ?? Environment.GetEnvironmentVariable(EnvironmentVariableName);
        if (!string.IsNullOrWhiteSpace(environment))
        {
            candidates.Add(environment!);
        }

        candidates.Add(Path.Combine(basePath, "Tools"));
        candidates.Add(basePath);

        var current = new DirectoryInfo(basePath);
        for (var depth = 0; depth < MaxAncestorDepth && current?.Parent is not null; depth++)
        {
            current = current.Parent;
            candidates.Add(Path.Combine(current.FullName, "Tools"));
        }

        foreach (var candidate in candidates)
        {
            var full = Path.GetFullPath(candidate);
            if (ContainsTools(full))
            {
                return CreateToolSet(full);
            }
        }

        // Nothing found: report the conventional location so error messages are actionable.
        return CreateToolSet(Path.Combine(basePath, "Tools"));
    }

    public static bool ContainsTools(string directory)
    {
        if (!Directory.Exists(directory))
        {
            return false;
        }

        return File.Exists(Path.Combine(directory, "yt-dlp", "yt-dlp.exe"))
               || File.Exists(Path.Combine(directory, "yt-dlp.exe"))
               || File.Exists(Path.Combine(directory, "ffmpeg", "ffmpeg.exe"));
    }

    public static ToolSet CreateToolSet(string toolsDirectory)
    {
        var directory = Path.GetFullPath(toolsDirectory);

        var ytDlp = FirstExisting(
            Path.Combine(directory, "yt-dlp", "yt-dlp.exe"),
            Path.Combine(directory, "yt-dlp.exe"));

        var ffmpeg = FirstExisting(
            Path.Combine(directory, "ffmpeg", "ffmpeg.exe"),
            Path.Combine(directory, "ffmpeg.exe"));

        var ffprobe = FirstExisting(
            Path.Combine(directory, "ffmpeg", "ffprobe.exe"),
            Path.Combine(directory, "ffprobe.exe"));

        return new ToolSet(directory, ytDlp, ffmpeg, ffprobe);
    }

    private static string FirstExisting(params string[] candidates)
    {
        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate))
            {
                return Path.GetFullPath(candidate);
            }
        }

        return Path.GetFullPath(candidates[0]);
    }
}
