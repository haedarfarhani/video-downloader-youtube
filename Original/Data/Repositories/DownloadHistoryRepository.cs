using System.Globalization;
using Microsoft.Data.Sqlite;
using VideoDownloader.Models;

namespace VideoDownloader.Data.Repositories;

public interface IDownloadHistoryRepository
{
    Task AddAsync(DownloadHistoryRecord record, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DownloadHistoryRecord>> GetAllAsync(int limit, int offset, CancellationToken cancellationToken = default);

    Task<int> GetCountAsync(CancellationToken cancellationToken = default);

    Task DeleteAsync(string id, CancellationToken cancellationToken = default);

    Task ClearAsync(CancellationToken cancellationToken = default);
}

/// <summary>Parameterized SQLite access for download history. Media contents are never stored.</summary>
public sealed class DownloadHistoryRepository : IDownloadHistoryRepository
{
    private const string SelectColumns =
        "Id, Url, Title, OutputPath, Mode, State, StartedAt, CompletedAt, FileSizeBytes, ErrorMessage";

    private readonly AppDatabase _database;

    public DownloadHistoryRepository(AppDatabase database)
    {
        _database = database;
    }

    public async Task AddAsync(DownloadHistoryRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        await using var connection = _database.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO DownloadHistory
                (Id, Url, Title, OutputPath, Mode, State, StartedAt, CompletedAt, FileSizeBytes, ErrorMessage)
            VALUES
                ($id, $url, $title, $outputPath, $mode, $state, $startedAt, $completedAt, $fileSizeBytes, $errorMessage);
            """;
        command.Parameters.AddWithValue("$id", record.Id);
        command.Parameters.AddWithValue("$url", record.Url);
        command.Parameters.AddWithValue("$title", record.Title);
        command.Parameters.AddWithValue("$outputPath", record.OutputPath ?? string.Empty);
        command.Parameters.AddWithValue("$mode", record.Mode.ToString());
        command.Parameters.AddWithValue("$state", record.State.ToString());
        command.Parameters.AddWithValue("$startedAt", record.StartedAt.ToString("O", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$completedAt", record.CompletedAt?.ToString("O", CultureInfo.InvariantCulture) ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("$fileSizeBytes", record.FileSizeBytes);
        command.Parameters.AddWithValue("$errorMessage", record.ErrorMessage ?? (object)DBNull.Value);

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<DownloadHistoryRecord>> GetAllAsync(
        int limit,
        int offset,
        CancellationToken cancellationToken = default)
    {
        await using var connection = _database.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            SELECT {SelectColumns}
            FROM DownloadHistory
            ORDER BY StartedAt DESC
            LIMIT $limit OFFSET $offset;
            """;
        command.Parameters.AddWithValue("$limit", limit);
        command.Parameters.AddWithValue("$offset", offset);

        var records = new List<DownloadHistoryRecord>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            records.Add(ReadRecord(reader));
        }

        return records;
    }

    public async Task<int> GetCountAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = _database.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM DownloadHistory;";
        var value = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return Convert.ToInt32(value, CultureInfo.InvariantCulture);
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        await using var connection = _database.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM DownloadHistory WHERE Id = $id;";
        command.Parameters.AddWithValue("$id", id);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = _database.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM DownloadHistory;";
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static DownloadHistoryRecord ReadRecord(SqliteDataReader reader)
    {
        var startedAt = ParseDate(reader.GetString(6)) ?? DateTime.UtcNow;

        return new DownloadHistoryRecord
        {
            Id = reader.GetString(0),
            Url = reader.GetString(1),
            Title = reader.GetString(2),
            OutputPath = reader.IsDBNull(3) ? null : reader.GetString(3),
            Mode = Enum.TryParse<DownloadMode>(reader.GetString(4), out var mode) ? mode : DownloadMode.Video,
            State = Enum.TryParse<DownloadState>(reader.GetString(5), out var state) ? state : DownloadState.Completed,
            StartedAt = startedAt,
            CompletedAt = reader.IsDBNull(7) ? null : ParseDate(reader.GetString(7)),
            FileSizeBytes = reader.IsDBNull(8) ? 0 : reader.GetInt64(8),
            ErrorMessage = reader.IsDBNull(9) ? null : reader.GetString(9)
        };
    }

    private static DateTime? ParseDate(string value)
        => DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed)
            ? parsed
            : null;
}
