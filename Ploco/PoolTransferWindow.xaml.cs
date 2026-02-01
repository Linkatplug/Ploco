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
        private readonly ObservableCollection<LocomotiveModel> _locomotives;
        private readonly CollectionViewSource _lineasSource;
        private readonly CollectionViewSource _sibelitSource;

        public PoolTransferWindow(ObservableCollection<LocomotiveModel> locomotives)
        {
            InitializeComponent();
            Owner = Application.Current.MainWindow; // Définit la fenêtre principale comme propriétaire
            WindowStartupLocation = WindowStartupLocation.CenterOwner; // Centre la fenêtre sur la principale

            _locomotives = locomotives;

            _lineasSource = new CollectionViewSource { Source = _locomotives };
            _lineasSource.SortDescriptions.Add(new SortDescription(nameof(LocomotiveModel.Number), ListSortDirection.Ascending));
            _lineasSource.Filter += (_, args) =>
            {
                if (args.Item is LocomotiveModel loco)
                {
                    args.Accepted = string.Equals(loco.Pool, "Lineas");
                }
            };

            _sibelitSource = new CollectionViewSource { Source = _locomotives };
            _sibelitSource.SortDescriptions.Add(new SortDescription(nameof(LocomotiveModel.Number), ListSortDirection.Ascending));
            _sibelitSource.Filter += (_, args) =>
            {
                if (args.Item is LocomotiveModel loco)
                {
                    args.Accepted = string.Equals(loco.Pool, "Sibelit");
                }
            };

            ListBoxLineas.ItemsSource = _lineasSource.View;
            ListBoxSibelit.ItemsSource = _sibelitSource.View;
            UpdateCounts();
        }

        private void BtnTransferToLineas_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = ListBoxSibelit.SelectedItems.Cast<LocomotiveModel>().ToList();
            foreach (var loco in selectedItems)
            {
                loco.Pool = "Lineas";
            }

            RefreshViews();
        }

        private void BtnTransferToSibelit_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = ListBoxLineas.SelectedItems.Cast<LocomotiveModel>().ToList();
            foreach (var loco in selectedItems)
            {
                loco.Pool = "Sibelit";
            }

            RefreshViews();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void RefreshViews()
        {
            _lineasSource.View.Refresh();
            _sibelitSource.View.Refresh();
            UpdateCounts();
        }

        private void UpdateCounts()
        {
            var sibelitCount = _locomotives.Count(loco => string.Equals(loco.Pool, "Sibelit", System.StringComparison.OrdinalIgnoreCase));
            var lineasCount = _locomotives.Count(loco => string.Equals(loco.Pool, "Lineas", System.StringComparison.OrdinalIgnoreCase));
            SibelitCountText.Text = $"Nombre de locomotives : {sibelitCount}";
            LineasCountText.Text = $"Nombre de locomotives : {lineasCount}";
        }
    }
}
