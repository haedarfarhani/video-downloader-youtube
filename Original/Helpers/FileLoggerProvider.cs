using System.Text;
using Microsoft.Extensions.Logging;

namespace VideoDownloader.Helpers;

/// <summary>
/// Minimal, dependency free file logger. Writes one line per entry into a daily log file inside
/// the application data directory so issues can be diagnosed without a debugger.
/// </summary>
public sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly string _directory;
    private readonly object _sync = new();
    private readonly LogLevel _minimumLevel;
    private bool _disposed;

    public FileLoggerProvider(string directory, LogLevel minimumLevel = LogLevel.Information)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        _directory = directory;
        _minimumLevel = minimumLevel;
        Directory.CreateDirectory(_directory);
    }

    public ILogger CreateLogger(string categoryName) => new FileLogger(this, categoryName);

    public void Dispose() => _disposed = true;

    private void Write(string categoryName, LogLevel level, EventId eventId, string message, Exception? exception)
    {
        if (_disposed || level < _minimumLevel)
        {
            return;
        }

        var builder = new StringBuilder();
        builder.Append(DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
        builder.Append(" [").Append(level.ToString().ToUpperInvariant()).Append("] ");
        builder.Append(categoryName).Append(": ").Append(message);

        if (eventId.Id != 0)
        {
            builder.Append(" (event ").Append(eventId.Id).Append(')');
        }

        if (exception is not null)
        {
            builder.AppendLine();
            builder.Append(exception);
        }

        builder.AppendLine();

        try
        {
            lock (_sync)
            {
                var path = Path.Combine(_directory, $"app-{DateTime.Now:yyyyMMdd}.log");
                File.AppendAllText(path, builder.ToString(), Encoding.UTF8);
            }
        }
        catch (IOException)
        {
            // Logging must never crash the application.
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private sealed class FileLogger(FileLoggerProvider provider, string categoryName) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= provider._minimumLevel && !provider._disposed;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel) || formatter is null)
            {
                return;
            }

            provider.Write(categoryName, logLevel, eventId, formatter(state, exception), exception);
        }
    }
}
