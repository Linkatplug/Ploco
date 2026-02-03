using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Data;
using System.Linq;
using Ploco.Models;

namespace Ploco
{
    public partial class SwapDialog : Window
    {
        public LocomotiveModel LocoFromSibelit { get; }
        public ObservableCollection<LocomotiveModel> LineasPool { get; }
        public LocomotiveModel? SelectedLoco { get; private set; }

        public SwapDialog(LocomotiveModel locoFromSibelit, ObservableCollection<LocomotiveModel> lineasPool)
        {
            InitializeComponent();
            Owner = Application.Current.MainWindow; // Définit la fenêtre principale comme propriétaire
            WindowStartupLocation = WindowStartupLocation.CenterOwner; // Centre la fenêtre sur la principale

            LocoFromSibelit = locoFromSibelit;
            LineasPool = lineasPool;

            // Afficher la loco de Sibelit dans un TextBlock pour information
            tbLocoSibelit.Text = LocoFromSibelit.Number.ToString();

            // Remplir le ComboBox avec la pool Lineas et trier par NumeroSerie
            cbLineas.ItemsSource = LineasPool;
            CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(LineasPool);
            view.SortDescriptions.Add(new System.ComponentModel.SortDescription(nameof(LocomotiveModel.Number), System.ComponentModel.ListSortDirection.Ascending));
            cbLineas.DisplayMemberPath = nameof(LocomotiveModel.DisplayName);

            if (cbLineas.Items.Count > 0)
                cbLineas.SelectedIndex = 0;

            // Pré-remplir le champ date/heure avec la date et l'heure actuelles
            tbDateTime.Text = DateTime.Now.ToString("G");
        }

        private void btnSwap_Click(object sender, RoutedEventArgs e)
        {
            if (cbLineas.SelectedItem == null)
            {
                MessageBox.Show("Veuillez sélectionner une loco dans la pool Lineas.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // La loco sélectionnée dans la pool Lineas est celle avec laquelle on souhaite effectuer le swap
            SelectedLoco = cbLineas.SelectedItem as LocomotiveModel;

            // Le swap effectif (mise à jour des pools et de CurrentPool) se fera dans le code appelant
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
