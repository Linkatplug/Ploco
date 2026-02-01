using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Ploco.Models;

namespace Ploco.Data
{
    public class PlocoRepository
    {
        private readonly string _connectionString;

        public PlocoRepository(string databasePath)
        {
            _connectionString = $"Data Source={databasePath}";
        }

        public void Initialize()
        {
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
                    FOREIGN KEY(tile_id) REFERENCES tiles(id)
                );",
                @"CREATE TABLE IF NOT EXISTS track_locomotives (
                    track_id INTEGER NOT NULL,
                    loco_id INTEGER NOT NULL,
                    position INTEGER NOT NULL,
                    PRIMARY KEY(track_id, loco_id),
                    FOREIGN KEY(track_id) REFERENCES tracks(id),
                    FOREIGN KEY(loco_id) REFERENCES locomotives(id)
                );",
                @"CREATE TABLE IF NOT EXISTS history (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    timestamp TEXT NOT NULL,
                    action TEXT NOT NULL,
                    details TEXT NOT NULL
                );"
            };

            foreach (var sql in commands)
            {
                using var command = connection.CreateCommand();
                command.CommandText = sql;
                command.ExecuteNonQuery();
            }
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
                command.CommandText = "SELECT id, series_id, number, status FROM locomotives;";
                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    var seriesId = reader.GetInt32(1);
                    var seriesName = series.TryGetValue(seriesId, out var serie) ? serie.Name : "Serie";
                    state.Locomotives.Add(new LocomotiveModel
                    {
                        Id = reader.GetInt32(0),
                        SeriesId = seriesId,
                        SeriesName = seriesName,
                        Number = reader.GetInt32(2),
                        Status = Enum.Parse<LocomotiveStatus>(reader.GetString(3))
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
                        }
                    }

                    tiles[tile.Id] = tile;
                    state.Tiles.Add(tile);
                }
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT id, tile_id, name, position FROM tracks ORDER BY position;";
                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    var track = new TrackModel
                    {
                        Id = reader.GetInt32(0),
                        TileId = reader.GetInt32(1),
                        Name = reader.GetString(2),
                        Position = reader.GetInt32(3)
                    };
                    if (tiles.TryGetValue(track.TileId, out var tile))
                    {
                        tile.Tracks.Add(track);
                    }
                }
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT track_id, loco_id, position FROM track_locomotives ORDER BY position;";
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
                    }
                }
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
                insertLoco.CommandText = "INSERT INTO locomotives (series_id, number, status) VALUES ($seriesId, $number, $status);";
                var seriesParam = insertLoco.CreateParameter();
                seriesParam.ParameterName = "$seriesId";
                insertLoco.Parameters.Add(seriesParam);
                var numberParam = insertLoco.CreateParameter();
                numberParam.ParameterName = "$number";
                insertLoco.Parameters.Add(numberParam);
                var statusParam = insertLoco.CreateParameter();
                statusParam.ParameterName = "$status";
                insertLoco.Parameters.Add(statusParam);

                for (var number = 1301; number <= 1349; number++)
                {
                    seriesParam.Value = seriesId;
                    numberParam.Value = number;
                    statusParam.Value = LocomotiveStatus.Ok.ToString();
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
                command.CommandText = "INSERT INTO locomotives (series_id, number, status) VALUES ($seriesId, $number, $status);";
                command.Parameters.AddWithValue("$seriesId", loco.SeriesId);
                command.Parameters.AddWithValue("$number", loco.Number);
                command.Parameters.AddWithValue("$status", loco.Status.ToString());
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
                    GarageTrackNumber = tile.GarageTrackNumber
                });
                command.Parameters.AddWithValue("$config", configJson);
                command.ExecuteNonQuery();
                tile.Id = GetLastInsertRowId(connection);

                var trackPosition = 0;
                foreach (var track in tile.Tracks)
                {
                    using var trackCommand = connection.CreateCommand();
                    trackCommand.CommandText = "INSERT INTO tracks (tile_id, name, position) VALUES ($tileId, $name, $position);";
                    trackCommand.Parameters.AddWithValue("$tileId", tile.Id);
                    trackCommand.Parameters.AddWithValue("$name", track.Name);
                    trackCommand.Parameters.AddWithValue("$position", trackPosition++);
                    trackCommand.ExecuteNonQuery();
                    track.Id = GetLastInsertRowId(connection);

                    var locoPosition = 0;
                    foreach (var loco in track.Locomotives)
                    {
                        using var assignCommand = connection.CreateCommand();
                        assignCommand.CommandText = "INSERT INTO track_locomotives (track_id, loco_id, position) VALUES ($trackId, $locoId, $position);";
                        assignCommand.Parameters.AddWithValue("$trackId", track.Id);
                        assignCommand.Parameters.AddWithValue("$locoId", loco.Id);
                        assignCommand.Parameters.AddWithValue("$position", locoPosition++);
                        assignCommand.ExecuteNonQuery();
                    }
                }
            }

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

        private static void ExecuteNonQuery(SqliteConnection connection, string sql)
        {
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }

        private static int GetLastInsertRowId(SqliteConnection connection)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT last_insert_rowid();";
            return Convert.ToInt32(command.ExecuteScalar());
        }

        private class TileConfig
        {
            public string? LocationPreset { get; set; }
            public int? GarageTrackNumber { get; set; }
        }
    }
}
