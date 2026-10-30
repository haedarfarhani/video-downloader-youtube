using System.Diagnostics;
using Microsoft.Extensions.Logging;
using VideoDownloader.Helpers;
using VideoDownloader.Models;

namespace VideoDownloader.Services;

/// <summary>
/// Optional FFmpeg helpers for cases where yt-dlp's built-in post-processing is not enough.
/// Downloads normally let yt-dlp merge and convert; these methods are for explicit conversions.
/// </summary>
public interface IFFmpegService
{
    Task<FFmpegResult> ExtractAudioAsync(string inputPath, string outputPath, CancellationToken cancellationToken = default);

    Task<FFmpegResult> MergeAsync(string videoPath, string audioPath, string outputPath, CancellationToken cancellationToken = default);

    Task<FFmpegResult> ConvertAsync(string inputPath, string outputPath, CancellationToken cancellationToken = default);

    Task<FFmpegResult> ConvertWithOptionsAsync(ConversionOptions options, IProgress<ConversionProgress>? progress, CancellationToken cancellationToken = default);

    Task<string?> GetVersionAsync(CancellationToken cancellationToken = default);
}

public sealed class FFmpegService : IFFmpegService
{
    private static readonly TimeSpan ConversionTimeout = TimeSpan.FromHours(4);
    private static readonly TimeSpan VersionTimeout = TimeSpan.FromSeconds(20);

    private readonly IProcessRunner _processRunner;
    private readonly IToolPathService _toolPaths;
    private readonly ILogger<FFmpegService> _logger;

    public FFmpegService(IProcessRunner processRunner, IToolPathService toolPaths, ILogger<FFmpegService> logger)
    {
        _processRunner = processRunner;
        _toolPaths = toolPaths;
        _logger = logger;
    }

    public Task<FFmpegResult> ExtractAudioAsync(string inputPath, string outputPath, CancellationToken cancellationToken = default)
    {
        if (!ValidateInputs(inputPath, outputPath, out var error))
        {
            return Task.FromResult(FFmpegResult.Failed(error!));
        }

        var codec = AudioCodecFor(outputPath);
        var arguments = new List<string>
        {
            "-nostdin",
            "-hide_banner",
            "-y",
            "-i", inputPath,
            "-vn",
            "-c:a", codec
        };

        if (codec is "libmp3lame" or "aac")
        {
            arguments.Add("-b:a");
            arguments.Add("192k");
        }

        arguments.Add(outputPath);
        return RunFFmpegAsync(arguments, outputPath, cancellationToken);
    }

    public Task<FFmpegResult> MergeAsync(string videoPath, string audioPath, string outputPath, CancellationToken cancellationToken = default)
    {
        if (!ValidateInputs(videoPath, outputPath, out var error))
        {
            return Task.FromResult(FFmpegResult.Failed(error!));
        }

        if (!File.Exists(audioPath))
        {
            return Task.FromResult(FFmpegResult.Failed($"The audio file does not exist: {audioPath}"));
        }

        // Stream copy avoids a needless re-encode; incompatible codecs surface as a clear error.
        var arguments = new List<string>
        {
            "-nostdin",
            "-hide_banner",
            "-y",
            "-i", videoPath,
            "-i", audioPath,
            "-map", "0:v:0",
            "-map", "1:a:0",
            "-c", "copy",
            outputPath
        };

        return RunFFmpegAsync(arguments, outputPath, cancellationToken);
    }

    public Task<FFmpegResult> ConvertAsync(string inputPath, string outputPath, CancellationToken cancellationToken = default)
    {
        if (!ValidateInputs(inputPath, outputPath, out var error))
        {
            return Task.FromResult(FFmpegResult.Failed(error!));
        }

        var extension = Path.GetExtension(outputPath).ToLowerInvariant();
        var arguments = new List<string> { "-nostdin", "-hide_banner", "-y", "-i", inputPath };

        switch (extension)
        {
            case ".mp4":
                arguments.AddRange(["-c:v", "libx264", "-c:a", "aac", "-movflags", "+faststart"]);
                break;
            case ".webm":
                arguments.AddRange(["-c:v", "libvpx-vp9", "-c:a", "libopus"]);
                break;
            case ".mkv":
                arguments.AddRange(["-c", "copy"]);
                break;
            default:
                return Task.FromResult(FFmpegResult.Failed(
                    $"Unsupported target container '{extension}'. Use mp4, webm or mkv."));
        }

        arguments.Add(outputPath);
        return RunFFmpegAsync(arguments, outputPath, cancellationToken);
    }

    public Task<FFmpegResult> ConvertWithOptionsAsync(ConversionOptions options, IProgress<ConversionProgress>? progress, CancellationToken cancellationToken = default)
    {
        // For Stage 6: validates paths and delegate; full parameter mapping will be completed in next stage.
        if (!File.Exists(options.InputPath)) return Task.FromResult(FFmpegResult.Failed("Input file missing."));
        return ConvertAsync(options.InputPath, options.OutputPath, cancellationToken);
    }

    public async Task<string?> GetVersionAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_toolPaths.FFmpegPath))
        {
            return null;
        }

        var result = await _processRunner
            .RunAsync(_toolPaths.FFmpegPath, ["-version"], timeout: VersionTimeout, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (result.ExitCode != 0)
        {
            return null;
        }

        return result.StandardOutput
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();
    }

    private async Task<FFmpegResult> RunFFmpegAsync(List<string> arguments, string outputPath, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = await _processRunner
            .RunAsync(_toolPaths.FFmpegPath, arguments, timeout: ConversionTimeout, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        stopwatch.Stop();

        if (result.Cancelled)
        {
            return FFmpegResult.Failed("The FFmpeg operation was cancelled.");
        }

        if (!result.Started)
        {
            return FFmpegResult.Failed(result.StartError ?? "ffmpeg could not be started.");
        }

        if (result.TimedOut)
        {
            return FFmpegResult.Failed("The FFmpeg operation timed out.", result.ExitCode, result.Diagnostics);
        }

        if (result.ExitCode != 0)
        {
            _logger.LogWarning("FFmpeg exited with code {ExitCode}: {Diagnostics}", result.ExitCode, result.Diagnostics);
            return FFmpegResult.Failed(
                string.IsNullOrWhiteSpace(result.Diagnostics)
                    ? $"ffmpeg exited with code {result.ExitCode}."
                    : result.Diagnostics,
                result.ExitCode,
                result.Diagnostics);
        }

        var file = new FileInfo(outputPath);
        if (!file.Exists || file.Length == 0)
        {
            return FFmpegResult.Failed($"FFmpeg finished but did not create a usable file: {outputPath}", result.ExitCode, result.Diagnostics);
        }

        _logger.LogInformation("FFmpeg completed {Output} in {Duration}", outputPath, stopwatch.Elapsed);
        return new FFmpegResult
        {
            Success = true,
            ExitCode = result.ExitCode,
            OutputFilePath = file.FullName,
            Diagnostics = ProcessOutputTail.Get(result.StandardError)
        };
    }

    private static bool ValidateInputs(string inputPath, string outputPath, out string? error)
    {
        error = null;

        if (string.IsNullOrWhiteSpace(inputPath) || !File.Exists(inputPath))
        {
            error = $"The input file does not exist: {inputPath}";
            return false;
        }

        if (string.IsNullOrWhiteSpace(outputPath))
        {
            error = "No output path was provided.";
            return false;
        }

        return true;
    }

    private static string AudioCodecFor(string outputPath)
    {
        var extension = Path.GetExtension(outputPath).ToLowerInvariant();
        return extension switch
        {
            ".mp3" => "libmp3lame",
            ".m4a" or ".mp4" => "aac",
            ".opus" => "libopus",
            ".ogg" => "libvorbis",
            ".flac" => "flac",
            ".wav" => "pcm_s16le",
            _ => "libmp3lame"
        };
    }
}
