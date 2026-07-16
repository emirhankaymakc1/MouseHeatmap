using Microsoft.Data.Sqlite;

namespace MouseHeatmap.Core.Data;

public sealed class Database
{
    private const string Schema = """
        CREATE TABLE IF NOT EXISTS mouse_events (
            id            INTEGER PRIMARY KEY AUTOINCREMENT,
            timestamp     REAL    NOT NULL,
            x             INTEGER NOT NULL,
            y             INTEGER NOT NULL,
            event_type    INTEGER NOT NULL,
            button        INTEGER NOT NULL DEFAULT 0,
            scroll_x      INTEGER NOT NULL DEFAULT 0,
            scroll_y      INTEGER NOT NULL DEFAULT 0,
            monitor_index INTEGER NOT NULL DEFAULT 0
        );

        CREATE INDEX IF NOT EXISTS idx_events_timestamp ON mouse_events (timestamp);
        CREATE INDEX IF NOT EXISTS idx_events_type      ON mouse_events (event_type);

        CREATE TABLE IF NOT EXISTS monitors (
            monitor_index INTEGER PRIMARY KEY,
            left          INTEGER NOT NULL,
            top           INTEGER NOT NULL,
            width         INTEGER NOT NULL,
            height        INTEGER NOT NULL,
            is_primary    INTEGER NOT NULL,
            last_seen     REAL    NOT NULL
        );
        """;

    private readonly string _connectionString;

    public Database(string dbPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = dbPath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Pooling = true
        }.ToString();

        Initialize();
    }

    public SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var pragma = connection.CreateCommand();
        pragma.CommandText = "PRAGMA journal_mode=WAL; PRAGMA synchronous=NORMAL; PRAGMA busy_timeout=5000;";
        pragma.ExecuteNonQuery();
        return connection;
    }

    private void Initialize()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = Schema;
        command.ExecuteNonQuery();
    }
}
