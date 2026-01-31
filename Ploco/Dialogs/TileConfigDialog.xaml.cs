using System.Collections.Generic;
using System.Windows;
using Ploco.Models;

namespace Ploco.Dialogs
{
    public partial class TileConfigDialog : Window
    {
        private readonly TileModel _tile;
        private readonly List<string> _presets = new()
        {
            "Garage A",
            "Garage B",
            "Garage C",
            "Dépôt Nord",
            "Dépôt Sud",
            "Personnalisé"
        };

        public TileConfigDialog(TileModel tile)
        {
            InitializeComponent();
            _tile = tile;
            TileNameText.Text = tile.Name;
            LocationPresetCombo.ItemsSource = _presets;
            LocationPresetCombo.SelectedItem = tile.LocationPreset ?? _presets[0];
            TrackNumberText.Text = tile.GarageTrackNumber?.ToString() ?? string.Empty;

            if (tile.Type == TileType.ArretLigne)
            {
                LocationPresetCombo.IsEnabled = false;
                TrackNumberText.IsEnabled = false;
                CustomLocationText.Visibility = Visibility.Collapsed;
            }
            else if (!string.IsNullOrWhiteSpace(tile.LocationPreset) && !_presets.Contains(tile.LocationPreset))
            {
                LocationPresetCombo.SelectedItem = "Personnalisé";
                CustomLocationText.Text = tile.LocationPreset;
                CustomLocationText.Visibility = Visibility.Visible;
            }
        }

        private void Validate_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TileNameText.Text))
            {
                MessageBox.Show("Le nom est obligatoire.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _tile.Name = TileNameText.Text.Trim();

            if (_tile.Type == TileType.VoieGarage)
            {
                var preset = LocationPresetCombo.SelectedItem?.ToString();
                if (preset == "Personnalisé")
                {
                    _tile.LocationPreset = string.IsNullOrWhiteSpace(CustomLocationText.Text)
                        ? "Personnalisé"
                        : CustomLocationText.Text.Trim();
                }
                else
                {
                    _tile.LocationPreset = preset;
                }
                if (int.TryParse(TrackNumberText.Text, out var trackNumber))
                {
                    _tile.GarageTrackNumber = trackNumber;
                }
                else
                {
                    _tile.GarageTrackNumber = null;
                }
            }

            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void LocationPresetCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (_tile.Type == TileType.ArretLigne)
            {
                return;
            }

            var preset = LocationPresetCombo.SelectedItem?.ToString();
            CustomLocationText.Visibility = preset == "Personnalisé" ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
