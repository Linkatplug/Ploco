using System;
using System.Windows;
using System.Windows.Controls;
using Ploco.Models;

namespace Ploco.Dialogs
{
    public partial class StatusDialog : Window
    {
        private readonly LocomotiveModel _locomotive;

        public StatusDialog(LocomotiveModel locomotive, LocomotiveStatus? forcedStatus = null)
        {
            InitializeComponent();
            _locomotive = locomotive;
            LocoLabel.Text = locomotive.DisplayName;
            StatusCombo.SelectedIndex = (int)(forcedStatus ?? locomotive.Status);
            TractionMotorsText.Text = locomotive.TractionPercent.HasValue
                ? TractionPercentToMotors(locomotive.TractionPercent.Value).ToString()
                : string.Empty;
            HsReasonText.Text = locomotive.HsReason ?? string.Empty;
            DefautInfoText.Text = locomotive.DefautInfo ?? string.Empty;
            if (forcedStatus.HasValue)
            {
                StatusCombo.IsEnabled = false;
            }
            UpdatePanels();
        }

        private void StatusCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdatePanels();
        }

        private void Validate_Click(object sender, RoutedEventArgs e)
        {
            if (StatusCombo.SelectedItem is ComboBoxItem item && item.Tag is string tag &&
                Enum.TryParse(tag, out LocomotiveStatus status))
            {
                if (status == LocomotiveStatus.ManqueTraction)
                {
                    if (!int.TryParse(TractionMotorsText.Text.Trim(), out var motorsHs) || motorsHs < 1 || motorsHs > 4)
                    {
                        MessageBox.Show("Saisissez un nombre de moteurs HS entre 1 et 4.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (motorsHs == 4)
                    {
                        MessageBox.Show("4 moteurs HS : passez la locomotive en statut HS.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    _locomotive.TractionPercent = MotorsToTractionPercent(motorsHs);
                    _locomotive.HsReason = null;
                    _locomotive.DefautInfo = null;
                }
                else
                {
                    _locomotive.TractionPercent = null;
                }

                if (status == LocomotiveStatus.HS)
                {
                    if (string.IsNullOrWhiteSpace(HsReasonText.Text))
                    {
                        MessageBox.Show("Veuillez renseigner une raison HS.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    _locomotive.HsReason = HsReasonText.Text.Trim();
                    _locomotive.DefautInfo = null;
                }
                else
                {
                    _locomotive.HsReason = null;
                }

                if (status == LocomotiveStatus.DefautMineur)
                {
                    if (string.IsNullOrWhiteSpace(DefautInfoText.Text))
                    {
                        MessageBox.Show("Veuillez renseigner la description du problème.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    _locomotive.DefautInfo = DefautInfoText.Text.Trim();
                }
                else
                {
                    _locomotive.DefautInfo = null;
                }

                _locomotive.Status = status;
                DialogResult = true;
                return;
            }

            MessageBox.Show("Sélectionnez un statut valide.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void UpdatePanels()
        {
            var status = GetSelectedStatus();
            TractionPanel.Visibility = status == LocomotiveStatus.ManqueTraction ? Visibility.Visible : Visibility.Collapsed;
            HsPanel.Visibility = status == LocomotiveStatus.HS ? Visibility.Visible : Visibility.Collapsed;
            DefautPanel.Visibility = status == LocomotiveStatus.DefautMineur ? Visibility.Visible : Visibility.Collapsed;
            TractionHint.Text = status == LocomotiveStatus.ManqueTraction
                ? "1 moteur HS = 75% · 2 moteurs HS = 50% · 3 moteurs HS = 25%"
                : string.Empty;
        }

        private LocomotiveStatus? GetSelectedStatus()
        {
            if (StatusCombo.SelectedItem is ComboBoxItem item && item.Tag is string tag &&
                Enum.TryParse(tag, out LocomotiveStatus status))
            {
                return status;
            }

            return null;
        }

        private static int MotorsToTractionPercent(int motorsHs)
        {
            return motorsHs switch
            {
                1 => 75,
                2 => 50,
                3 => 25,
                _ => 0
            };
        }

        private static int TractionPercentToMotors(int percent)
        {
            return percent switch
            {
                75 => 1,
                50 => 2,
                25 => 3,
                _ => 0
            };
        }
    }
}
