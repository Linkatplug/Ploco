using System.Windows;
using Ploco.Models;

namespace Ploco.Dialogs
{
    public partial class LineTrackDialog : Window
    {
        public LineTrackDialog()
        {
            InitializeComponent();
        }

        public TrackModel BuildTrack()
        {
            var isOnTrain = OnTrainCheck.IsChecked == true;
            return new TrackModel
            {
                Name = TrackNameText.Text.Trim(),
                IsOnTrain = isOnTrain,
                TrainNumber = isOnTrain ? TrainNumberText.Text.Trim() : null,
                StopTime = isOnTrain ? StopTimeText.Text.Trim() : null,
                IssueReason = IssueReasonText.Text.Trim(),
                IsLocomotiveHs = IsHsCheck.IsChecked == true
            };
        }

        private void OnTrainCheck_Checked(object sender, RoutedEventArgs e)
        {
            TrainDetailsPanel.Visibility = OnTrainCheck.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        }

        private void Validate_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TrackNameText.Text))
            {
                MessageBox.Show("Veuillez saisir un nom de voie.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(IssueReasonText.Text))
            {
                MessageBox.Show("Veuillez saisir une raison.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (OnTrainCheck.IsChecked == true && string.IsNullOrWhiteSpace(TrainNumberText.Text))
            {
                MessageBox.Show("Veuillez saisir un numéro de train.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
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
