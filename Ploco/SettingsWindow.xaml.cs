using System;
using System.Windows;

namespace Ploco
{
    public partial class SettingsWindow : Window
    {
        public int GridOffset { get; private set; }
        public int LocoWidth { get; private set; }
        public int LocoHeight { get; private set; }

        public SettingsWindow()
        {
            InitializeComponent();
            // Valeurs par défaut
            tbGridOffset.Text = "20";
            tbLocoWidth.Text = "42";
            tbLocoHeight.Text = "32";
        }

        private void Button_Validate_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(tbGridOffset.Text, out int gridOffset) &&
                int.TryParse(tbLocoWidth.Text, out int locoWidth) &&
                int.TryParse(tbLocoHeight.Text, out int locoHeight))
            {
                GridOffset = gridOffset;
                LocoWidth = locoWidth;
                LocoHeight = locoHeight;
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Veuillez entrer des valeurs numériques valides.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
