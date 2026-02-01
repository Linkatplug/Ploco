using System.Windows;

namespace Ploco.Dialogs
{
    public partial class LinePlaceDialog : Window
    {
        public LinePlaceDialog(string? placeName = null)
        {
            InitializeComponent();
            if (!string.IsNullOrWhiteSpace(placeName))
            {
                PlaceNameText.Text = placeName;
            }
        }

        public string PlaceName => PlaceNameText.Text.Trim();
        public string TrackName => TrackNameText.Text.Trim();
        public string LocomotiveNumbers => LocomotiveNumbersText.Text.Trim();
        public string IssueReason => IssueReasonText.Text.Trim();
        public bool IsOnTrain => OnTrainCheck.IsChecked == true;
        public string TrainNumber => TrainNumberText.Text.Trim();
        public string StopTime => StopTimeText.Text.Trim();
        public bool IsLocomotiveHs => IsHsCheck.IsChecked == true;
        public string HsLocomotiveNumbers => HsLocomotivesText.Text.Trim();

        private void OnTrainCheck_Checked(object sender, RoutedEventArgs e)
        {
            TrainDetailsPanel.Visibility = OnTrainCheck.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        }

        private void IsHsCheck_Checked(object sender, RoutedEventArgs e)
        {
            HsLocomotivesPanel.Visibility = IsHsCheck.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        }

        private void Validate_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(PlaceName))
            {
                MessageBox.Show("Veuillez saisir un nom de lieu.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(TrackName))
            {
                MessageBox.Show("Veuillez saisir un nom de voie.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(IssueReason))
            {
                MessageBox.Show("Veuillez saisir une raison d'incident.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (IsOnTrain && string.IsNullOrWhiteSpace(TrainNumber))
            {
                MessageBox.Show("Veuillez saisir le numéro du train.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (IsLocomotiveHs && string.IsNullOrWhiteSpace(HsLocomotiveNumbers))
            {
                MessageBox.Show("Veuillez saisir les locomotives HS.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
