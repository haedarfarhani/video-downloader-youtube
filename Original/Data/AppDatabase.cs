using Microsoft.Data.Sqlite;
using VideoDownloader.Configuration;

namespace VideoDownloader.Data;

/// <summary>Owns the local SQLite database used for settings and download history.</summary>
public sealed class AppDatabase
{
    private readonly string _connectionString;

    public AppDatabase(AppPaths paths)
    {
        ArgumentNullException.ThrowIfNull(paths);

        paths.EnsureCreated();
        _connectionString = paths.DatabaseConnectionString;
        Initialize();
    }

    public SqliteConnection CreateConnection()
        => new(_connectionString);

    private void Initialize()
    {
        using var connection = CreateConnection();
        connection.Open();

        using (var pragma = connection.CreateCommand())
        {
            pragma.CommandText = "PRAGMA journal_mode=WAL; PRAGMA synchronous=NORMAL; PRAGMA busy_timeout=5000;";
            pragma.ExecuteNonQuery();
        }

        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS DownloadHistory (
                Id            TEXT PRIMARY KEY NOT NULL,
                Url           TEXT NOT NULL,
                Title         TEXT NOT NULL,
                OutputPath    TEXT NOT NULL,
                Mode          TEXT NOT NULL,
                State         TEXT NOT NULL,
                StartedAt     TEXT NOT NULL,
                CompletedAt   TEXT,
                FileSizeBytes INTEGER NOT NULL DEFAULT 0,
                ErrorMessage  TEXT
            );

            CREATE INDEX IF NOT EXISTS IX_DownloadHistory_StartedAt
                ON DownloadHistory (StartedAt DESC);

            CREATE TABLE IF NOT EXISTS Settings (
                Key   TEXT PRIMARY KEY NOT NULL,
                Value TEXT NOT NULL
            );
            """;
        command.ExecuteNonQuery();
    }
}
