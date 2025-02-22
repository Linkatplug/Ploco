using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Linq;
using Ploco.Models;

namespace Ploco
{
    public partial class SwapDialog : Window
    {
        public Locomotive LocoFromSibelit { get; set; }
        public ObservableCollection<Locomotive> LineasPool { get; set; }
        public Locomotive SelectedLoco { get; set; }  // La loco sélectionnée dans le ComboBox

        public SwapDialog(Locomotive locoFromSibelit, ObservableCollection<Locomotive> lineasPool)
        {
            InitializeComponent();
            Owner = Application.Current.MainWindow; // Définit la fenêtre principale comme propriétaire
            WindowStartupLocation = WindowStartupLocation.CenterOwner; // Centre la fenêtre sur la principale
            LocoFromSibelit = locoFromSibelit;
            LineasPool = lineasPool;
            tbLocoSibelit.Text = LocoFromSibelit.ToString();
            cbLineas.ItemsSource = LineasPool;
            // Tri automatique de la liste des locomotives de la pool Lineas.
            CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(LineasPool);
            view.SortDescriptions.Add(new SortDescription("NumeroSerie", ListSortDirection.Ascending));
            if (cbLineas.Items.Count > 0)
                cbLineas.SelectedIndex = 0;
            // Remplissage du champ Date/Heure avec la date et l'heure actuelles.
            tbDateTime.Text = DateTime.Now.ToString("G");
        }

        private void btnSwap_Click(object sender, RoutedEventArgs e)
        {
            if (cbLineas.SelectedItem == null)
            {
                MessageBox.Show("Veuillez sélectionner une loco dans la pool Lineas.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            SelectedLoco = cbLineas.SelectedItem as Locomotive;
            // Archive les informations de swap dans un fichier log.
            string logEntry = $"Action: Swap, Loc Sibelit: {LocoFromSibelit}, Loc Lineas: {SelectedLoco}, Date: {tbDateTime.Text}, Message: {tbMessage.Text}";
            try
            {
                System.IO.File.AppendAllText("SwapLog.txt", logEntry + Environment.NewLine);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors de l'enregistrement du swap : " + ex.Message);
            }
            this.DialogResult = true;
            this.Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void btnModifier_Click(object sender, RoutedEventArgs e)
        {
            // Permet de modifier manuellement la date/heure en rendant le champ éditable.
            tbDateTime.IsReadOnly = !tbDateTime.IsReadOnly;
            if (!tbDateTime.IsReadOnly)
            {
                tbDateTime.Focus();
                tbDateTime.SelectAll();
            }
        }
    }
}
