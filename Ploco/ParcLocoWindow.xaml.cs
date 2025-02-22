using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using Ploco.Models;

namespace Ploco
{
    public partial class ParcLocoWindow : Window
    {
        public ObservableCollection<Locomotive> SibelitPool { get; set; }
        public ObservableCollection<Locomotive> LineasPool { get; set; }

        public ParcLocoWindow(ObservableCollection<Locomotive> sibelitPool, ObservableCollection<Locomotive> lineasPool)
        {
            InitializeComponent();
            Owner = Application.Current.MainWindow; // Définit la fenêtre principale comme propriétaire
            WindowStartupLocation = WindowStartupLocation.CenterOwner; // Centre la fenêtre sur la principale
            SibelitPool = sibelitPool;
            LineasPool = lineasPool;
            ItemsControlSibelit.ItemsSource = SibelitPool;
            ItemsControlLineas.ItemsSource = LineasPool;

            CollectionView viewSibelit = (CollectionView)CollectionViewSource.GetDefaultView(SibelitPool);
            viewSibelit.SortDescriptions.Add(new SortDescription("NumeroSerie", ListSortDirection.Ascending));
            CollectionView viewLineas = (CollectionView)CollectionViewSource.GetDefaultView(LineasPool);
            viewLineas.SortDescriptions.Add(new SortDescription("NumeroSerie", ListSortDirection.Ascending));
        }

        private void MenuItem_Swap_Click(object sender, RoutedEventArgs e)
        {
            // Réutiliser la logique de MainWindow si souhaité.
        }

        private void MenuItem_ModifierStatut_Click(object sender, RoutedEventArgs e)
        {
            // Réutiliser la logique de MainWindow si souhaité.
        }
    }
}
