using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Ploco.Models;

namespace Ploco.Dialogs
{
    public partial class TapisT13Window : Window
    {
        private readonly ObservableCollection<T13Row> _rows = new();

        public TapisT13Window(IEnumerable<LocomotiveModel> locomotives, IEnumerable<TileModel> tiles)
        {
            InitializeComponent();
            T13Grid.ItemsSource = _rows;
            LoadRows(locomotives, tiles);
        }

        private void LoadRows(IEnumerable<LocomotiveModel> locomotives, IEnumerable<TileModel> tiles)
        {
            _rows.Clear();
            var tracks = tiles.SelectMany(tile => tile.Tracks).ToList();

            foreach (var loco in locomotives
                         .Where(l => IsT13(l) && string.Equals(l.Pool, "Sibelit", StringComparison.OrdinalIgnoreCase))
                         .OrderBy(l => l.Number))
            {
                var track = tracks.FirstOrDefault(t => t.Locomotives.Contains(loco));
                var location = ResolveLocation(track, tiles);
                var trainInfo = track?.IsOnTrain == true
                    ? string.IsNullOrWhiteSpace(track.TrainNumber)
                        ? location
                        : $"{location} {track.TrainNumber}"
                    : location;

                var locHs = loco.Status == LocomotiveStatus.HS ? trainInfo : string.Empty;
                var report = loco.Status == LocomotiveStatus.HS ? trainInfo : string.Empty;

                _rows.Add(new T13Row
                {
                    Locomotive = loco.Number.ToString(),
                    LocHs = locHs,
                    Report = report
                });
            }
        }

        private static bool IsT13(LocomotiveModel loco)
        {
            return loco.SeriesName.Contains("1300", StringComparison.OrdinalIgnoreCase)
                   || loco.SeriesName.Contains("T13", StringComparison.OrdinalIgnoreCase);
        }

        private static string ResolveLocation(TrackModel? track, IEnumerable<TileModel> tiles)
        {
            if (track == null)
            {
                return string.Empty;
            }

            var tile = tiles.FirstOrDefault(t => t.Tracks.Contains(track));
            if (tile == null)
            {
                return track.Name;
            }

            return GetLocationAbbreviation(tile.Name);
        }

        private static string GetLocationAbbreviation(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return name;
            }

            var normalized = name.Trim();
            return normalized switch
            {
                "Thionville" => "THL",
                "SRH" => "SRH",
                "Anvers Nord" => "FN",
                "Anvers" => "FN",
                "Mulhouse Nord" => "MUN",
                "Mulhouse" => "MUN",
                "Bale" => "BAL",
                "Woippy" => "WPY",
                "Uckange" => "UCK",
                "Zeebrugge" => "LZR",
                "Gent" => "FGZH",
                "Muizen" => "FIZ",
                "Monceau" => "LNC",
                "La Louviere" => "GLI",
                "Chatelet" => "FCL",
                _ => normalized
            };
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private sealed class T13Row
        {
            public string Locomotive { get; set; } = string.Empty;
            public string LocHs { get; set; } = string.Empty;
            public string Report { get; set; } = string.Empty;
        }
    }
}
