using System;
using System.Windows;
using System.Windows.Controls;
using Ploco.Models;

namespace Ploco.Dialogs
{
    public partial class PdfPlacementDialog : Window
    {
        private readonly PlanningPdfWindow.PdfPlacementViewModel _placement;

        public PdfPlacementDialog(PlanningPdfWindow.PdfPlacementViewModel placement)
        {
            InitializeComponent();
            _placement = placement;
            StatusCombo.SelectedIndex = placement.Status switch
            {
                LocomotiveStatus.ManqueTraction => 1,
                LocomotiveStatus.HS => 2,
                _ => 0
            };
            TractionText.Text = placement.TractionPercent?.ToString() ?? string.Empty;
            MotorsHsText.Text = placement.MotorsHsCount?.ToString() ?? string.Empty;
            HsReasonText.Text = placement.HsReason ?? string.Empty;
            OnTrainCheck.IsChecked = placement.OnTrain;
            TrainNumberText.Text = placement.TrainNumber ?? string.Empty;
            TrainStopText.Text = placement.TrainStopTime ?? string.Empty;
            CommentText.Text = placement.Comment ?? string.Empty;
        }

        private void Validate_Click(object sender, RoutedEventArgs e)
        {
            _placement.Status = StatusCombo.SelectedIndex switch
            {
                1 => LocomotiveStatus.ManqueTraction,
                2 => LocomotiveStatus.HS,
                _ => LocomotiveStatus.Ok
            };

            _placement.TractionPercent = int.TryParse(TractionText.Text, out var traction) ? traction : null;
            _placement.MotorsHsCount = int.TryParse(MotorsHsText.Text, out var motors) ? motors : null;
            _placement.HsReason = string.IsNullOrWhiteSpace(HsReasonText.Text) ? null : HsReasonText.Text.Trim();
            _placement.OnTrain = OnTrainCheck.IsChecked == true;
            _placement.TrainNumber = string.IsNullOrWhiteSpace(TrainNumberText.Text) ? null : TrainNumberText.Text.Trim();
            _placement.TrainStopTime = string.IsNullOrWhiteSpace(TrainStopText.Text) ? null : TrainStopText.Text.Trim();
            _placement.Comment = string.IsNullOrWhiteSpace(CommentText.Text) ? null : CommentText.Text.Trim();
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
