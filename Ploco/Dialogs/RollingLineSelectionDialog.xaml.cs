using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Ploco.Models;

namespace Ploco.Dialogs
{
    public partial class RollingLineSelectionDialog : Window
    {
        public class RollingLineItem
        {
            public TrackModel Track { get; set; } = null!;
            public string Name { get; set; } = string.Empty;
            public string TileName { get; set; } = string.Empty;
            public bool IsOccupied { get; set; }
        }

        public TrackModel? SelectedTrack { get; private set; }

        public RollingLineSelectionDialog(IEnumerable<TileModel> tiles)
        {
            InitializeComponent();

            var rollingLines = new List<RollingLineItem>();

            foreach (var tile in tiles.Where(t => t.Type == TileType.RollingLine))
            {
                foreach (var track in tile.RollingLineTracks)
                {
                    rollingLines.Add(new RollingLineItem
                    {
                        Track = track,
                        Name = track.Name,
                        TileName = tile.Name,
                        IsOccupied = track.Locomotives.Any()
                    });
                }
            }

            // Sort: non-occupied first, then by name
            rollingLines = rollingLines.OrderBy(x => x.IsOccupied).ThenBy(x => x.Name).ToList();

            RollingLineListBox.ItemsSource = rollingLines;
            
            if (rollingLines.Any())
            {
                RollingLineListBox.SelectedIndex = 0;
            }
        }

        private void Validate_Click(object sender, RoutedEventArgs e)
        {
            if (RollingLineListBox.SelectedItem is RollingLineItem item)
            {
                SelectedTrack = item.Track;
                DialogResult = true;
            }
            else
            {
                MessageBox.Show("Veuillez sélectionner une ligne de roulement.", "Validation", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void ListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (RollingLineListBox.SelectedItem is RollingLineItem item)
            {
                SelectedTrack = item.Track;
                DialogResult = true;
            }
        }
    }
}
