using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Ploco.Models;

namespace Ploco.Data
{
    public class PlocoRepository
    {
        private readonly string _databasePath;
        private readonly string _connectionString;

        public PlocoRepository(string databasePath)
        {
            _databasePath = databasePath;
            _connectionString = $"Data Source={databasePath}";
        }

        public void Initialize()
        {
            EnsureDatabaseFile();
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var commands = new[]
            {
                @"CREATE TABLE IF NOT EXISTS series (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL UNIQUE,
                    start_number INTEGER NOT NULL,
                    end_number INTEGER NOT NULL
                );",
                @"CREATE TABLE IF NOT EXISTS locomotives (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    series_id INTEGER NOT NULL,
                    number INTEGER NOT NULL,
                    status TEXT NOT NULL,
                    pool TEXT NOT NULL DEFAULT 'Lineas',
                    traction_percent INTEGER,
                    hs_reason TEXT,
                    FOREIGN KEY(series_id) REFERENCES series(id)
                );",
                @"CREATE TABLE IF NOT EXISTS tiles (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL,
                    type TEXT NOT NULL,
                    x REAL NOT NULL,
                    y REAL NOT NULL,
                    config_json TEXT
                );",
                @"CREATE TABLE IF NOT EXISTS tracks (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    tile_id INTEGER NOT NULL,
                    name TEXT NOT NULL,
                    position INTEGER NOT NULL,
                    type TEXT NOT NULL DEFAULT 'Main',
                    config_json TEXT,
                    FOREIGN KEY(tile_id) REFERENCES tiles(id)
                );",
                @"CREATE TABLE IF NOT EXISTS track_locomotives (
                    track_id INTEGER NOT NULL,
                    loco_id INTEGER NOT NULL,
                    position INTEGER NOT NULL,
                    offset_x REAL,
                    PRIMARY KEY(track_id, loco_id),
                    FOREIGN KEY(track_id) REFERENCES tracks(id),
                    FOREIGN KEY(loco_id) REFERENCES locomotives(id)
                );",
                @"CREATE TABLE IF NOT EXISTS history (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    timestamp TEXT NOT NULL,
                    action TEXT NOT NULL,
                    details TEXT NOT NULL
                );",
                @"CREATE TABLE IF NOT EXISTS places (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    type TEXT NOT NULL,
                    name TEXT NOT NULL,
                    UNIQUE(type, name)
                );"
            };

            foreach (var sql in commands)
            {
                using var command = connection.CreateCommand();
                command.CommandText = sql;
                command.ExecuteNonQuery();
            }

            EnsureColumn(connection, "locomotives", "pool", "TEXT NOT NULL DEFAULT 'Lineas'");
            EnsureColumn(connection, "locomotives", "traction_percent", "INTEGER");
            EnsureColumn(connection, "locomotives", "hs_reason", "TEXT");
            EnsureColumn(connection, "tracks", "type", "TEXT NOT NULL DEFAULT 'Main'");
            EnsureColumn(connection, "tracks", "config_json", "TEXT");
            EnsureColumn(connection, "track_locomotives", "offset_x", "REAL");
        }

        public AppState LoadState()
        {
            var state = new AppState();
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var series = new Dictionary<int, RollingStockSeries>();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT id, name, start_number, end_number FROM series;";
                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    var item = new RollingStockSeries
                    {
                        Id = reader.GetInt32(0),
                        Name = reader.GetString(1),
                        StartNumber = reader.GetInt32(2),
                        EndNumber = reader.GetInt32(3)
                    };
                    series[item.Id] = item;
                    state.Series.Add(item);
                }
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT id, series_id, number, status, pool, traction_percent, hs_reason FROM locomotives;";
                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    var seriesId = reader.GetInt32(1);
                    var seriesName = series.TryGetValue(seriesId, out var serie) ? serie.Name : "Serie";
                    var statusValue = reader.GetString(3);
                    var status = ParseLocomotiveStatus(statusValue);
                    state.Locomotives.Add(new LocomotiveModel
                    {
                        Id = reader.GetInt32(0),
                        SeriesId = seriesId,
                        SeriesName = seriesName,
                        Number = reader.GetInt32(2),
                        Status = status,
                        Pool = reader.IsDBNull(4) ? "Lineas" : reader.GetString(4),
                        TractionPercent = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                        HsReason = reader.IsDBNull(6) ? null : reader.GetString(6)
                    });
                }
            }

            var tiles = new Dictionary<int, TileModel>();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT id, name, type, x, y, config_json FROM tiles;";
                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    var tile = new TileModel
                    {
                        Id = reader.GetInt32(0),
                        Name = reader.GetString(1),
                        Type = Enum.Parse<TileType>(reader.GetString(2)),
                        X = reader.GetDouble(3),
                        Y = reader.GetDouble(4)
                    };

                    var configJson = reader.IsDBNull(5) ? null : reader.GetString(5);
                    if (!string.IsNullOrWhiteSpace(configJson))
                    {
                        var config = JsonSerializer.Deserialize<TileConfig>(configJson);
                        if (config != null)
                        {
                            tile.LocationPreset = config.LocationPreset;
                            tile.GarageTrackNumber = config.GarageTrackNumber;
                            if (config.Width.HasValue)
                            {
                                tile.Width = config.Width.Value;
                            }
                            if (config.Height.HasValue)
                            {
                                tile.Height = config.Height.Value;
                            }
                        }
                    }

                    tiles[tile.Id] = tile;
                    state.Tiles.Add(tile);
                }
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT id, tile_id, name, position, type, config_json FROM tracks ORDER BY position;";
                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    var track = new TrackModel
                    {
                        Id = reader.GetInt32(0),
                        TileId = reader.GetInt32(1),
                        Name = reader.GetString(2),
                        Position = reader.GetInt32(3),
                        Kind = Enum.TryParse(reader.GetString(4), out TrackKind kind) ? kind : TrackKind.Main
                    };
                    var configJson = reader.IsDBNull(5) ? null : reader.GetString(5);
                    if (!string.IsNullOrWhiteSpace(configJson))
                    {
                        var config = JsonSerializer.Deserialize<TrackConfig>(configJson);
                        if (config != null)
                        {
                            track.IsOnTrain = config.IsOnTrain;
                            track.TrainNumber = config.TrainNumber;
                            track.StopTime = config.StopTime;
                            track.IssueReason = config.IssueReason;
                            track.IsLocomotiveHs = config.IsLocomotiveHs;
                            track.LeftLabel = config.LeftLabel;
                            track.RightLabel = config.RightLabel;
                            track.IsLeftBlocked = config.IsLeftBlocked;
                            track.IsRightBlocked = config.IsRightBlocked;
                        }
                    }
                    if (tiles.TryGetValue(track.TileId, out var tile))
                    {
                        tile.Tracks.Add(track);
                    }
                }
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT track_id, loco_id, position, offset_x FROM track_locomotives ORDER BY position;";
                using var reader = command.ExecuteReader();
                var locosById = state.Locomotives.ToDictionary(l => l.Id);
                var tracksById = state.Tiles.SelectMany(t => t.Tracks).ToDictionary(t => t.Id);

                while (reader.Read())
                {
                    var trackId = reader.GetInt32(0);
                    var locoId = reader.GetInt32(1);
                    if (tracksById.TryGetValue(trackId, out var track) && locosById.TryGetValue(locoId, out var loco))
                    {
                        track.Locomotives.Add(loco);
                        loco.AssignedTrackId = trackId;
                        var offset = reader.IsDBNull(3) ? (double?)null : reader.GetDouble(3);
                        loco.AssignedTrackOffsetX = track.Kind == TrackKind.Line || track.Kind == TrackKind.Zone || track.Kind == TrackKind.Output
                            ? offset
                            : null;
                    }
                }
            }

            foreach (var tile in state.Tiles)
            {
                tile.RefreshTrackCollections();
            }
            return state;
        }

        public void SeedDefaultDataIfNeeded()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT COUNT(*) FROM series;";
                var count = (long)command.ExecuteScalar()!;
                if (count > 0)
                {
                    return;
                }
            }

            using var transaction = connection.BeginTransaction();
            var seriesId = InsertSeries(connection, "1300", 1301, 1349);
            InsertSeries(connection, "37000", 37001, 37040);

            using (var insertLoco = connection.CreateCommand())
            {
                insertLoco.CommandText = "INSERT INTO locomotives (series_id, number, status, pool) VALUES ($seriesId, $number, $status, $pool);";
                var seriesParam = insertLoco.CreateParameter();
                seriesParam.ParameterName = "$seriesId";
                insertLoco.Parameters.Add(seriesParam);
                var numberParam = insertLoco.CreateParameter();
                numberParam.ParameterName = "$number";
                insertLoco.Parameters.Add(numberParam);
                var statusParam = insertLoco.CreateParameter();
                statusParam.ParameterName = "$status";
                insertLoco.Parameters.Add(statusParam);
                var poolParam = insertLoco.CreateParameter();
                poolParam.ParameterName = "$pool";
                insertLoco.Parameters.Add(poolParam);

                for (var number = 1301; number <= 1349; number++)
                {
                    seriesParam.Value = seriesId;
                    numberParam.Value = number;
                    statusParam.Value = LocomotiveStatus.Ok.ToString();
                    poolParam.Value = "Lineas";
                    insertLoco.ExecuteNonQuery();
                }
            }

            transaction.Commit();
        }

        public void SaveState(AppState state)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();

            ExecuteNonQuery(connection, "DELETE FROM track_locomotives;");
            ExecuteNonQuery(connection, "DELETE FROM tracks;");
            ExecuteNonQuery(connection, "DELETE FROM tiles;");
            ExecuteNonQuery(connection, "DELETE FROM locomotives;");
            ExecuteNonQuery(connection, "DELETE FROM series;");

            var seriesIdMap = new Dictionary<int, int>();
            foreach (var series in state.Series)
            {
                var newId = InsertSeries(connection, series.Name, series.StartNumber, series.EndNumber);
                seriesIdMap[series.Id] = newId;
                series.Id = newId;
            }

            foreach (var loco in state.Locomotives)
            {
                if (seriesIdMap.TryGetValue(loco.SeriesId, out var newSeriesId))
                {
                    loco.SeriesId = newSeriesId;
                }
                using var command = connection.CreateCommand();
                command.CommandText = "INSERT INTO locomotives (series_id, number, status, pool, traction_percent, hs_reason) VALUES ($seriesId, $number, $status, $pool, $traction, $reason);";
                command.Parameters.AddWithValue("$seriesId", loco.SeriesId);
                command.Parameters.AddWithValue("$number", loco.Number);
                command.Parameters.AddWithValue("$status", loco.Status.ToString());
                command.Parameters.AddWithValue("$pool", loco.Pool);
                command.Parameters.AddWithValue("$traction", (object?)loco.TractionPercent ?? DBNull.Value);
                command.Parameters.AddWithValue("$reason", string.IsNullOrWhiteSpace(loco.HsReason) ? DBNull.Value : loco.HsReason);
                command.ExecuteNonQuery();
                loco.Id = GetLastInsertRowId(connection);
            }

            foreach (var tile in state.Tiles)
            {
                using var command = connection.CreateCommand();
                command.CommandText = "INSERT INTO tiles (name, type, x, y, config_json) VALUES ($name, $type, $x, $y, $config);";
                command.Parameters.AddWithValue("$name", tile.Name);
                command.Parameters.AddWithValue("$type", tile.Type.ToString());
                command.Parameters.AddWithValue("$x", tile.X);
                command.Parameters.AddWithValue("$y", tile.Y);
                var configJson = JsonSerializer.Serialize(new TileConfig
                {
                    LocationPreset = tile.LocationPreset,
                    GarageTrackNumber = tile.GarageTrackNumber,
                    Width = tile.Width,
                    Height = tile.Height
                });
                command.Parameters.AddWithValue("$config", configJson);
                command.ExecuteNonQuery();
                tile.Id = GetLastInsertRowId(connection);

                var trackPosition = 0;
                foreach (var track in tile.Tracks)
                {
                    using var trackCommand = connection.CreateCommand();
                    trackCommand.CommandText = "INSERT INTO tracks (tile_id, name, position, type, config_json) VALUES ($tileId, $name, $position, $type, $config);";
                    trackCommand.Parameters.AddWithValue("$tileId", tile.Id);
                    trackCommand.Parameters.AddWithValue("$name", track.Name);
                    trackCommand.Parameters.AddWithValue("$position", trackPosition++);
                    trackCommand.Parameters.AddWithValue("$type", track.Kind.ToString());
                    var trackConfigJson = JsonSerializer.Serialize(new TrackConfig
                    {
                        IsOnTrain = track.IsOnTrain,
                        TrainNumber = track.TrainNumber,
                        StopTime = track.StopTime,
                        IssueReason = track.IssueReason,
                        IsLocomotiveHs = track.IsLocomotiveHs,
                        LeftLabel = track.LeftLabel,
                        RightLabel = track.RightLabel,
                        IsLeftBlocked = track.IsLeftBlocked,
                        IsRightBlocked = track.IsRightBlocked
                    });
                    object configValue = track.Kind == TrackKind.Line
                        || !string.IsNullOrWhiteSpace(track.TrainNumber)
                        || !string.IsNullOrWhiteSpace(track.LeftLabel)
                        || !string.IsNullOrWhiteSpace(track.RightLabel)
                        || track.IsLeftBlocked
                        || track.IsRightBlocked
                        ? trackConfigJson
                        : DBNull.Value;
                    trackCommand.Parameters.AddWithValue("$config", configValue);
                    trackCommand.ExecuteNonQuery();
                    track.Id = GetLastInsertRowId(connection);

                    var locoPosition = 0;
                    foreach (var loco in track.Locomotives)
                    {
                        using var assignCommand = connection.CreateCommand();
                        assignCommand.CommandText = "INSERT INTO track_locomotives (track_id, loco_id, position, offset_x) VALUES ($trackId, $locoId, $position, $offsetX);";
                        assignCommand.Parameters.AddWithValue("$trackId", track.Id);
                        assignCommand.Parameters.AddWithValue("$locoId", loco.Id);
                        assignCommand.Parameters.AddWithValue("$position", locoPosition++);
                        object offsetValue = track.Kind == TrackKind.Line || track.Kind == TrackKind.Zone || track.Kind == TrackKind.Output
                            ? (object?)loco.AssignedTrackOffsetX ?? DBNull.Value
                            : DBNull.Value;
                        assignCommand.Parameters.AddWithValue("$offsetX", offsetValue);
                        assignCommand.ExecuteNonQuery();
                    }
                }
            }

            SavePlaces(connection, state.Tiles);
            transaction.Commit();
        }

        public void AddHistory(string action, string details)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "INSERT INTO history (timestamp, action, details) VALUES ($timestamp, $action, $details);";
            command.Parameters.AddWithValue("$timestamp", DateTime.UtcNow.ToString("O"));
            command.Parameters.AddWithValue("$action", action);
            command.Parameters.AddWithValue("$details", details);
            command.ExecuteNonQuery();
        }

        private static int InsertSeries(SqliteConnection connection, string name, int startNumber, int endNumber)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "INSERT INTO series (name, start_number, end_number) VALUES ($name, $start, $end);";
            command.Parameters.AddWithValue("$name", name);
            command.Parameters.AddWithValue("$start", startNumber);
            command.Parameters.AddWithValue("$end", endNumber);
            command.ExecuteNonQuery();
            return GetLastInsertRowId(connection);
        }

        private static LocomotiveStatus ParseLocomotiveStatus(string value)
        {
            if (Enum.TryParse(value, out LocomotiveStatus status))
            {
                return status;
            }

            if (Enum.TryParse(value, out StatutLocomotive legacy))
            {
                return legacy switch
                {
                    StatutLocomotive.HS => LocomotiveStatus.HS,
                    StatutLocomotive.DefautMineur => LocomotiveStatus.ManqueTraction,
                    StatutLocomotive.AControler => LocomotiveStatus.ManqueTraction,
                    _ => LocomotiveStatus.Ok
                };
            }

            return LocomotiveStatus.Ok;
        }

        private static void ExecuteNonQuery(SqliteConnection connection, string sql)
        {
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }

        public List<HistoryEntry> LoadHistory()
        {
            var history = new List<HistoryEntry>();
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT timestamp, action, details FROM history ORDER BY timestamp DESC;";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                history.Add(new HistoryEntry
                {
                    Timestamp = DateTime.Parse(reader.GetString(0)),
                    Action = reader.GetString(1),
                    Details = reader.GetString(2)
                });
            }

            return history;
        }

        public Dictionary<string, int> GetTableCounts()
        {
            var tables = new[]
            {
                "series",
                "locomotives",
                "tiles",
                "tracks",
                "track_locomotives",
                "history",
                "places"
            };

            var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            foreach (var table in tables)
            {
                using var command = connection.CreateCommand();
                command.CommandText = $"SELECT COUNT(*) FROM {table};";
                var count = Convert.ToInt32(command.ExecuteScalar());
                result[table] = count;
            }

            return result;
        }

        public Dictionary<TrackKind, int> GetTrackKindCounts()
        {
            var result = new Dictionary<TrackKind, int>();
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT type, COUNT(*) FROM tracks GROUP BY type;";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var kindText = reader.GetString(0);
                if (Enum.TryParse(kindText, out TrackKind kind))
                {
                    result[kind] = reader.GetInt32(1);
                }
            }

            return result;
        }

        public void ClearHistory()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            ExecuteNonQuery(connection, "DELETE FROM history;");
        }

        public void ResetOperationalState()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "UPDATE locomotives SET traction_percent = NULL, hs_reason = NULL;";
                command.ExecuteNonQuery();
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "UPDATE tracks SET config_json = NULL WHERE type = 'Line';";
                command.ExecuteNonQuery();
            }

            transaction.Commit();
        }

        public void CopyDatabaseTo(string destinationPath)
        {
            if (string.IsNullOrWhiteSpace(destinationPath))
            {
                return;
            }

            System.IO.File.Copy(_databasePath, destinationPath, true);
        }

        public bool ReplaceDatabaseWith(string sourcePath)
        {
            if (string.IsNullOrWhiteSpace(sourcePath))
            {
                return false;
            }

            if (!IsSqliteDatabase(sourcePath))
            {
                return false;
            }

            File.Copy(sourcePath, _databasePath, true);
            return true;
        }

        private static int GetLastInsertRowId(SqliteConnection connection)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT last_insert_rowid();";
            return Convert.ToInt32(command.ExecuteScalar());
        }

        private static void EnsureColumn(SqliteConnection connection, string tableName, string columnName, string columnDefinition)
        {
            using var command = connection.CreateCommand();
            command.CommandText = $"PRAGMA table_info({tableName});";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }

            using var alterCommand = connection.CreateCommand();
            alterCommand.CommandText = $"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnDefinition};";
            alterCommand.ExecuteNonQuery();
        }

        private void EnsureDatabaseFile()
        {
            if (!File.Exists(_databasePath))
            {
                return;
            }

            if (IsSqliteDatabase(_databasePath))
            {
                return;
            }

            File.Delete(_databasePath);
        }

        private static bool IsSqliteDatabase(string path)
        {
            if (!File.Exists(path))
            {
                return false;
            }

            var header = new byte[16];
            using var stream = File.OpenRead(path);
            if (stream.Read(header, 0, header.Length) < header.Length)
            {
                return false;
            }

            var headerText = Encoding.ASCII.GetString(header);
            return headerText.StartsWith("SQLite format 3");
        }

        private static void SavePlaces(SqliteConnection connection, IEnumerable<TileModel> tiles)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "INSERT OR IGNORE INTO places (type, name) VALUES ($type, $name);";
            var typeParameter = command.CreateParameter();
            typeParameter.ParameterName = "$type";
            command.Parameters.Add(typeParameter);
            var nameParameter = command.CreateParameter();
            nameParameter.ParameterName = "$name";
            command.Parameters.Add(nameParameter);

            foreach (var tile in tiles)
            {
                typeParameter.Value = tile.Type.ToString();
                nameParameter.Value = tile.Name;
                command.ExecuteNonQuery();
            }
        }

        private class TileConfig
        {
            public string? LocationPreset { get; set; }
            public int? GarageTrackNumber { get; set; }
            public double? Width { get; set; }
            public double? Height { get; set; }
        }

        private class TrackConfig
        {
            public bool IsOnTrain { get; set; }
            public string? TrainNumber { get; set; }
            public string? StopTime { get; set; }
            public string? IssueReason { get; set; }
            public bool IsLocomotiveHs { get; set; }
            public string? LeftLabel { get; set; }
            public string? RightLabel { get; set; }
            public bool IsLeftBlocked { get; set; }
            public bool IsRightBlocked { get; set; }
        }
    }
}
