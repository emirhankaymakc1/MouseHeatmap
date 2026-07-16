using Microsoft.Data.Sqlite;
using MouseHeatmap.Core.Models;

namespace MouseHeatmap.Core.Data;

public sealed class EventRepository
{
    private readonly Database _database;

    public EventRepository(Database database) => _database = database;

    public void InsertBatch(IReadOnlyList<MouseEvent> events)
    {
        if (events.Count == 0) return;

        using var connection = _database.OpenConnection();
        using var transaction = connection.BeginTransaction();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO mouse_events
                (timestamp, x, y, event_type, button, scroll_x, scroll_y, monitor_index)
            VALUES ($ts, $x, $y, $type, $btn, $sx, $sy, $mon)
            """;

        var pTs = command.Parameters.Add("$ts", SqliteType.Real);
        var pX = command.Parameters.Add("$x", SqliteType.Integer);
        var pY = command.Parameters.Add("$y", SqliteType.Integer);
        var pType = command.Parameters.Add("$type", SqliteType.Integer);
        var pBtn = command.Parameters.Add("$btn", SqliteType.Integer);
        var pSx = command.Parameters.Add("$sx", SqliteType.Integer);
        var pSy = command.Parameters.Add("$sy", SqliteType.Integer);
        var pMon = command.Parameters.Add("$mon", SqliteType.Integer);

        foreach (var e in events)
        {
            pTs.Value = e.Timestamp;
            pX.Value = e.X;
            pY.Value = e.Y;
            pType.Value = (int)e.Type;
            pBtn.Value = (int)e.Button;
            pSx.Value = e.ScrollX;
            pSy.Value = e.ScrollY;
            pMon.Value = e.MonitorIndex;
            command.ExecuteNonQuery();
        }

        transaction.Commit();
    }

    public List<MouseEvent> Query(
        double? startTs = null,
        double? endTs = null,
        EventType? type = null,
        int? monitorIndex = null)
    {
        using var connection = _database.OpenConnection();
        using var command = connection.CreateCommand();

        var sql = "SELECT timestamp, x, y, event_type, button, scroll_x, scroll_y, monitor_index FROM mouse_events WHERE 1=1";
        if (startTs is not null) { sql += " AND timestamp >= $start"; command.Parameters.AddWithValue("$start", startTs); }
        if (endTs is not null) { sql += " AND timestamp <= $end"; command.Parameters.AddWithValue("$end", endTs); }
        if (type is not null) { sql += " AND event_type = $type"; command.Parameters.AddWithValue("$type", (int)type); }
        if (monitorIndex is not null) { sql += " AND monitor_index = $mon"; command.Parameters.AddWithValue("$mon", monitorIndex); }
        sql += " ORDER BY timestamp";
        command.CommandText = sql;

        var results = new List<MouseEvent>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            results.Add(new MouseEvent
            {
                Timestamp = reader.GetDouble(0),
                X = reader.GetInt32(1),
                Y = reader.GetInt32(2),
                Type = (EventType)reader.GetInt32(3),
                Button = (MouseButton)reader.GetInt32(4),
                ScrollX = reader.GetInt32(5),
                ScrollY = reader.GetInt32(6),
                MonitorIndex = reader.GetInt32(7)
            });
        }
        return results;
    }

    public long CountEvents()
    {
        using var connection = _database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM mouse_events";
        return (long)(command.ExecuteScalar() ?? 0L);
    }

    public void SaveMonitors(IReadOnlyList<MonitorInfo> monitors)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;
        using var connection = _database.OpenConnection();
        using var transaction = connection.BeginTransaction();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO monitors (monitor_index, left, top, width, height, is_primary, last_seen)
            VALUES ($i, $l, $t, $w, $h, $p, $s)
            ON CONFLICT(monitor_index) DO UPDATE SET
                left=$l, top=$t, width=$w, height=$h, is_primary=$p, last_seen=$s
            """;
        foreach (var m in monitors)
        {
            command.Parameters.Clear();
            command.Parameters.AddWithValue("$i", m.Index);
            command.Parameters.AddWithValue("$l", m.Left);
            command.Parameters.AddWithValue("$t", m.Top);
            command.Parameters.AddWithValue("$w", m.Width);
            command.Parameters.AddWithValue("$h", m.Height);
            command.Parameters.AddWithValue("$p", m.IsPrimary ? 1 : 0);
            command.Parameters.AddWithValue("$s", now);
            command.ExecuteNonQuery();
        }
        transaction.Commit();
    }

    public List<MonitorInfo> LoadMonitors()
    {
        using var connection = _database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT monitor_index, left, top, width, height, is_primary FROM monitors ORDER BY monitor_index";
        var results = new List<MonitorInfo>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            results.Add(new MonitorInfo(
                reader.GetInt32(0), reader.GetInt32(1), reader.GetInt32(2),
                reader.GetInt32(3), reader.GetInt32(4), reader.GetInt32(5) == 1));
        }
        return results;
    }
}
