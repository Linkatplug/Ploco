using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Linq;
using Ploco.Models;

namespace Ploco
{
    public partial class PoolTransferWindow : Window
    {
        public ObservableCollection<Locomotive> LineasPool { get; set; }
        public ObservableCollection<Locomotive> SibelitPool { get; set; }

        public PoolTransferWindow(ObservableCollection<Locomotive> lineasPool, ObservableCollection<Locomotive> sibelitPool)
        {
            InitializeComponent();
            Owner = Application.Current.MainWindow; // Définit la fenêtre principale comme propriétaire
            WindowStartupLocation = WindowStartupLocation.CenterOwner; // Centre la fenêtre sur la principale
            LineasPool = lineasPool;
            SibelitPool = sibelitPool;
            ListBoxLineas.ItemsSource = LineasPool;
            ListBoxSibelit.ItemsSource = SibelitPool;
            ListBoxLineas.Items.Refresh();
            ListBoxSibelit.Items.Refresh();

            CollectionView viewLineas = (CollectionView)CollectionViewSource.GetDefaultView(LineasPool);
            viewLineas.SortDescriptions.Add(new SortDescription("NumeroSerie", ListSortDirection.Ascending));
            CollectionView viewSibelit = (CollectionView)CollectionViewSource.GetDefaultView(SibelitPool);
            viewSibelit.SortDescriptions.Add(new SortDescription("NumeroSerie", ListSortDirection.Ascending));
        }

        // Transfert de Sibelit vers Lineas : si la loco est sur le tapis, le transfert est refusé.
        private void BtnTransferToLineas_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = ListBoxSibelit.SelectedItems.Cast<Locomotive>().ToList();
            foreach (var loco in selectedItems)
            {
                if (loco.IsOnCanvas)
                {
                    MessageBox.Show($"La loco {loco} est sur le tapis et ne peut être transférée.", "Transfert impossible", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    SibelitPool.Remove(loco);
                    if (!LineasPool.Contains(loco))
                        LineasPool.Add(loco);
                }
            }
        }

        // Transfert de Lineas vers Sibelit.
        private void BtnTransferToSibelit_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = ListBoxLineas.SelectedItems.Cast<Locomotive>().ToList();
            foreach (var loco in selectedItems)
            {
                LineasPool.Remove(loco);
                if (!SibelitPool.Contains(loco))
                    SibelitPool.Add(loco);
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
