using System;
using System.IO;
using System.Text;
using Microsoft.Extensions.Logging;

namespace VideoDownloader.WinForms
{
    public class FileLoggerProvider : ILoggerFactory, IDisposable
    {
        private readonly string _logFilePath;
        private readonly object _lock = new();

        public FileLoggerProvider(string logDirectory)
        {
            var date = DateTime.Now.ToString("yyyy-MM-dd");
            _logFilePath = Path.Combine(logDirectory, $"log-{date}.txt");
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new FileLogger(categoryName, this);
        }

        public void AddProvider(ILoggerProvider provider)
        {
            // WinForms: no external logging providers needed
        }

        public void Dispose() { }

        private sealed class FileLogger : ILogger
        {
            private readonly string _categoryName;
            private readonly FileLoggerProvider _provider;

            public FileLogger(string categoryName, FileLoggerProvider provider)
            {
                _categoryName = categoryName;
                _provider = provider;
            }

            public IDisposable BeginScope<TState>(TState state) where TState : notnull 
                => new NullDisposer();

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(
                LogLevel logLevel, 
                EventId eventId, 
                TState state, 
                Exception? exception, 
                Func<TState, Exception?, string> formatter)
            {
                lock (_provider._lock)
                {
                    try
                    {
                        var message = formatter(state, exception);
                        var text = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{logLevel}] {_categoryName}: {message}";
                        if (exception != null)
                            text += Environment.NewLine + exception.ToString();
                        File.AppendAllText(_provider._logFilePath, text + Environment.NewLine);
                    }
                    catch { /* ignore logging errors */ }
                }
            }
        }

        private sealed class NullDisposer : IDisposable 
        {
            public void Dispose() { }
        }
    }
}