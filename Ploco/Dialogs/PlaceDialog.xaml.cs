using System;
using System.Windows;
using System.Windows.Controls;
using Ploco.Models;

namespace Ploco.Dialogs
{
    public partial class PlaceDialog : Window
    {
        private static readonly string[] DepotNames =
        {
            "Hausbergen",
            "Mulhouse Nord",
            "Thionville",
            "Woippy"
        };

        private static readonly string[] GarageNames =
        {
            "Anvers Nord",
            "Bale",
            "Bettembourg",
            "Chatelet",
            "Gent",
            "La Louviere",
            "Luxembourg",
            "Monceau",
            "Ronet",
            "SRH",
            "Uckange",
            "Zeebrugge",
            "Autres"
        };

        public TileType SelectedType { get; private set; }
        public string SelectedName { get; private set; } = string.Empty;

        public PlaceDialog()
        {
            InitializeComponent();
            DepotNameCombo.ItemsSource = DepotNames;
            GarageNameCombo.ItemsSource = GarageNames;
            TypeCombo.SelectedIndex = 0;
        }

        private void TypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DepotPanel.Visibility = Visibility.Collapsed;
            GaragePanel.Visibility = Visibility.Collapsed;
            RollingLinePanel.Visibility = Visibility.Collapsed;
            if (TypeCombo.SelectedItem is ComboBoxItem item && item.Tag is string tag)
            {
                SelectedType = Enum.Parse<TileType>(tag);
            }

            switch (SelectedType)
            {
                case TileType.Depot:
                    DepotPanel.Visibility = Visibility.Visible;
                    DepotNameCombo.SelectedIndex = 0;
                    break;
                case TileType.VoieGarage:
                    GaragePanel.Visibility = Visibility.Visible;
                    GarageNameCombo.SelectedIndex = 0;
                    break;
                case TileType.ArretLigne:
                    break;
                case TileType.LigneRoulement:
                    RollingLinePanel.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void GarageNameCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = GarageNameCombo.SelectedItem as string;
            var showCustom = string.Equals(selected, "Autres");
            CustomGarageLabel.Visibility = showCustom ? Visibility.Visible : Visibility.Collapsed;
            CustomGarageName.Visibility = showCustom ? Visibility.Visible : Visibility.Collapsed;
        }

        private void Validate_Click(object sender, RoutedEventArgs e)
        {
            SelectedName = SelectedType switch
            {
                TileType.Depot => DepotNameCombo.SelectedItem as string ?? string.Empty,
                TileType.VoieGarage => ResolveGarageName(),
                TileType.ArretLigne => string.Empty,
                TileType.LigneRoulement => RollingLineName.Text.Trim(),
                _ => string.Empty
            };

            if (SelectedType != TileType.ArretLigne && string.IsNullOrWhiteSpace(SelectedName))
            {
                MessageBox.Show("Veuillez saisir un nom valide.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DialogResult = true;
        }

        private string ResolveGarageName()
        {
            var selected = GarageNameCombo.SelectedItem as string ?? string.Empty;
            if (string.Equals(selected, "Autres"))
            {
                return CustomGarageName.Text.Trim();
            }

            return selected;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
