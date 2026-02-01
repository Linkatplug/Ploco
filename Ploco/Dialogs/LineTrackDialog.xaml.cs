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
            return new TrackModel
            {
                Name = TrackNameText.Text.Trim(),
                IsOnTrain = OnTrainCheck.IsChecked == true,
                StopTime = StopTimeText.Text.Trim(),
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

            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
