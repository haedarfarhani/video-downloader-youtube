namespace VideoDownloader.Configuration;

/// <summary>
/// Centralizes every path the application uses. User data (database, logs, default download
/// folder) is always stored outside the installation directory.
/// </summary>
public sealed class AppPaths
{
    public AppPaths()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "VideoDownloader"))
    {
    }

    public AppPaths(string dataDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataDirectory);

        DataDirectory = dataDirectory;
        DatabasePath = Path.Combine(dataDirectory, "downloads.db");
        LogDirectory = Path.Combine(dataDirectory, "logs");
        DefaultDownloadDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Downloads",
            "VideoDownloader");
    }

    public string DataDirectory { get; }

    public string DatabasePath { get; }

    public string LogDirectory { get; }

    public string DefaultDownloadDirectory { get; }

    public string DatabaseConnectionString => $"Data Source={DatabasePath};Cache=Shared;Pooling=True";

    /// <summary>Creates the application data directories if they do not exist yet.</summary>
    public void EnsureCreated()
    {
        Directory.CreateDirectory(DataDirectory);
        Directory.CreateDirectory(LogDirectory);
        Directory.CreateDirectory(DefaultDownloadDirectory);
    }
}
