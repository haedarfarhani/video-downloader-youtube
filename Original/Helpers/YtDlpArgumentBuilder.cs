using VideoDownloader.Models;

namespace VideoDownloader.Helpers;

/// <summary>
/// Builds yt-dlp command line arguments as a list. Arguments are never concatenated into a
/// single command string; the caller passes the list to <see cref="System.Diagnostics.ProcessStartInfo.ArgumentList"/>.
/// </summary>
public static class YtDlpArgumentBuilder
{
    public const string ProgressPrefix = "__VD_PROGRESS__";
    public const string FinalPathPrefix = "__VD_FILE__";

    /// <summary>Fields emitted for every progress update (see parser for the matching order).</summary>
    private const string ProgressTemplate =
        "download:" + ProgressPrefix +
        "%(progress.status)s|%(progress.downloaded_bytes)s|%(progress.total_bytes)s|" +
        "%(progress.total_bytes_estimate)s|%(progress._percent_str)s|%(progress._speed_str)s|%(progress._eta_str)s";

    public static IReadOnlyList<string> BuildMediaInfoArgs(string url, bool includePlaylist)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);

        var arguments = new List<string>
        {
            "--no-color",
            "--no-warnings",
            "--dump-single-json"
        };

        if (includePlaylist)
        {
            arguments.Add("--yes-playlist");
            arguments.Add("--flat-playlist");
        }
        else
        {
            arguments.Add("--no-playlist");
        }

        arguments.Add("--");
        arguments.Add(url);
        return arguments;
    }

    /// <summary>
    /// Builds the yt-dlp download arguments.
    /// </summary>
    /// <param name="options">The download request.</param>
    /// <param name="ffmpegDirectory">Directory that contains ffmpeg, passed as --ffmpeg-location.</param>
    /// <param name="finalPathFile">
    /// Optional file that receives the final media path after post-processing. yt-dlp writes it
    /// as UTF-8, which is the only reliable way to capture non-Latin file names on Windows
    /// (its console output drops characters the current code page cannot represent).
    /// </param>
    public static IReadOnlyList<string> BuildDownloadArgs(
        DownloadOptions options,
        string? ffmpegDirectory = null,
        string? finalPathFile = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.Url);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.OutputDirectory);

        var arguments = new List<string>
        {
            "--no-color",
            "--newline",
            "--no-simulate",
            "--no-warnings",
            "--windows-filenames",
            "--trim-filenames", "150",
            "--socket-timeout", "30",
            "--retries", Math.Clamp(options.MaxRetries, 1, 10).ToString(),
            "--fragment-retries", "5",
            "--concurrent-fragments", "4",
            "--progress-template", ProgressTemplate,
            "--print", "after_move:" + FinalPathPrefix + "%(filepath)s",
            "-P", options.OutputDirectory,
            "-o", "%(title)s [%(id)s].%(ext)s"
        };

        if (!string.IsNullOrWhiteSpace(finalPathFile))
        {
            arguments.Add("--print-to-file");
            arguments.Add("after_move:%(filepath)s");
            arguments.Add(finalPathFile);
        }

        if (!string.IsNullOrWhiteSpace(ffmpegDirectory))
        {
            arguments.Add("--ffmpeg-location");
            arguments.Add(ffmpegDirectory);
        }

        if (options.IncludePlaylist)
        {
            arguments.Add("--yes-playlist");
        }
        else
        {
            arguments.Add("--no-playlist");
        }

        if (options.Mode == DownloadMode.Audio)
        {
            arguments.Add("--format");
            arguments.Add("bestaudio/best");
            arguments.Add("--extract-audio");
            arguments.Add("--audio-format");
            arguments.Add(FormatSelector.BuildAudioFormat(options.AudioFormat));
            arguments.Add("--audio-quality");
            arguments.Add("0");
        }
        else
        {
            arguments.Add("--format");
            arguments.Add(FormatSelector.BuildVideoFormat(options.MaxVideoHeight, options.Container));

            var container = FormatSelector.NormalizeContainer(options.Container);
            if (container != FormatSelector.AutomaticContainer)
            {
                // Only a preference: yt-dlp falls back to the available container when the
                // selected one is incompatible with the chosen codecs.
                arguments.Add("--merge-output-format");
                arguments.Add(container);
            }

            arguments.Add("--embed-metadata");
        }

        arguments.Add("--");
        arguments.Add(options.Url);
        return arguments;
    }
}
