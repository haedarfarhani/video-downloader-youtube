using System.Diagnostics;
using System.Text;

namespace VideoDownloader.Helpers;

/// <summary>Outcome of a command line tool invocation.</summary>
public sealed class ProcessRunResult
{
    public int ExitCode { get; init; }

    public bool Started { get; init; }

    public bool Cancelled { get; init; }

    public bool TimedOut { get; init; }

    public string StandardOutput { get; init; } = string.Empty;

    public string StandardError { get; init; } = string.Empty;

    public string? StartError { get; init; }

    /// <summary>Trimmed tail of stderr (or stdout when stderr is empty) for user facing errors.</summary>
    public string Diagnostics
    {
        get
        {
            var text = string.IsNullOrWhiteSpace(StandardError) ? StandardOutput : StandardError;
            return ProcessOutputTail.Get(text);
        }
    }
}

/// <summary>
/// Runs external tools with <see cref="ProcessStartInfo.ArgumentList"/> (never a concatenated
/// command line), drains stdout/stderr concurrently, and terminates the process tree safely
/// when cancelled.
/// </summary>
public interface IProcessRunner
{
    Task<ProcessRunResult> RunAsync(
        string fileName,
        IReadOnlyList<string> arguments,
        Action<string>? onStandardOutputLine = null,
        Action<string>? onStandardErrorLine = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default,
        IReadOnlyDictionary<string, string>? environment = null);
}

public sealed class ProcessRunner : IProcessRunner
{
    private const int MaxCapturedCharacters = 200_000;

    public async Task<ProcessRunResult> RunAsync(
        string fileName,
        IReadOnlyList<string> arguments,
        Action<string>? onStandardOutputLine = null,
        Action<string>? onStandardErrorLine = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default,
        IReadOnlyDictionary<string, string>? environment = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentNullException.ThrowIfNull(arguments);

        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        if (environment is not null)
        {
            foreach (var (key, value) in environment)
            {
                startInfo.Environment[key] = value;
            }
        }

        using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        var stdout = new StringBuilder();
        var stderr = new StringBuilder();

        try
        {
            if (!process.Start())
            {
                return new ProcessRunResult { StartError = $"Could not start {Path.GetFileName(fileName)}." };
            }
        }
        catch (Exception ex)
        {
            return new ProcessRunResult
            {
                StartError = $"Could not start {Path.GetFileName(fileName)}: {ex.Message}"
            };
        }

        using var timeoutSource = timeout is null ? null : new CancellationTokenSource(timeout.Value);
        using var linkedSource = timeoutSource is null
            ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)
            : CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutSource.Token);

        var linkedToken = linkedSource.Token;

        // Both streams are drained concurrently so the child process can never block on a full pipe.
        var stdoutTask = ReadStreamAsync(process.StandardOutput, stdout, onStandardOutputLine, linkedToken);
        var stderrTask = ReadStreamAsync(process.StandardError, stderr, onStandardErrorLine, linkedToken);

        using var registration = linkedToken.Register(() => TryKillProcessTree(process));

        try
        {
            await process.WaitForExitAsync(linkedToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            TryKillProcessTree(process);

            // Give the process a bounded amount of time to actually terminate.
            var exitTask = process.WaitForExitAsync(CancellationToken.None);
            var completed = await Task.WhenAny(exitTask, Task.Delay(TimeSpan.FromSeconds(10))).ConfigureAwait(false);
            if (completed != exitTask)
            {
                TryKillProcessTree(process);
            }
        }
        catch (Exception ex)
        {
            TryKillProcessTree(process);
            return new ProcessRunResult
            {
                Started = false,
                StartError = $"Failed while running {Path.GetFileName(fileName)}: {ex.Message}"
            };
        }

        await AwaitReaderAsync(stdoutTask).ConfigureAwait(false);
        await AwaitReaderAsync(stderrTask).ConfigureAwait(false);

        var timedOut = timeoutSource?.IsCancellationRequested == true && !cancellationToken.IsCancellationRequested;
        var userCancelled = cancellationToken.IsCancellationRequested;

        int exitCode;
        try
        {
            exitCode = process.HasExited ? process.ExitCode : -1;
        }
        catch (InvalidOperationException)
        {
            exitCode = -1;
        }

        return new ProcessRunResult
        {
            Started = true,
            ExitCode = exitCode,
            Cancelled = userCancelled,
            TimedOut = timedOut,
            StandardOutput = stdout.ToString(),
            StandardError = stderr.ToString()
        };
    }

    private static async Task ReadStreamAsync(
        StreamReader reader,
        StringBuilder buffer,
        Action<string>? lineHandler,
        CancellationToken cancellationToken)
    {
        try
        {
            while (true)
            {
                var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                if (line is null)
                {
                    break;
                }

                if (buffer.Length < MaxCapturedCharacters)
                {
                    buffer.AppendLine(line);
                }

                if (lineHandler is not null)
                {
                    try
                    {
                        lineHandler(line);
                    }
                    catch
                    {
                        // A faulty progress handler must never tear down the download.
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when the process is being terminated.
        }
        catch (ObjectDisposedException)
        {
            // Stream was closed because the process exited.
        }
        catch (IOException)
        {
            // Stream was closed because the process exited.
        }
    }

    private static async Task AwaitReaderAsync(Task readerTask)
    {
        try
        {
            await readerTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
        catch (IOException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private static void TryKillProcessTree(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
            // Process already exited.
        }
        catch (NotSupportedException)
        {
            try
            {
                process.Kill();
            }
            catch
            {
                // Ignore: the process is already gone.
            }
        }
        catch (System.ComponentModel.Win32Exception)
        {
            // Access denied or already terminated; nothing else can be done.
        }
    }
}

/// <summary>Extracts a short, useful tail from tool diagnostics for display and logging.</summary>
public static class ProcessOutputTail
{
    private const int MaxLines = 12;
    private const int MaxCharacters = 1500;

    public static string Get(string? output)
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            return string.Empty;
        }

        var lines = output
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .TakeLast(MaxLines)
            .ToList();

        var text = string.Join(Environment.NewLine, lines);
        return text.Length <= MaxCharacters ? text : text[^MaxCharacters..];
    }
}
