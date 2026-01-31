using System;
using System.Windows;
using System.Windows.Controls;
using Ploco.Models;

namespace Ploco.Dialogs
{
    public partial class StatusDialog : Window
    {
        private readonly LocomotiveModel _locomotive;

        public StatusDialog(LocomotiveModel locomotive)
        {
            InitializeComponent();
            _locomotive = locomotive;
            LocoLabel.Text = locomotive.DisplayName;
            StatusCombo.SelectedIndex = (int)locomotive.Status;
        }

        private void Validate_Click(object sender, RoutedEventArgs e)
        {
            if (StatusCombo.SelectedItem is ComboBoxItem item && item.Tag is string tag &&
                Enum.TryParse(tag, out LocomotiveStatus status))
            {
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
    }
}
