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

        public string ActivePool { get; private set; }
        public bool HideNonActivePool { get; private set; }

        public PoolTransferWindow(ObservableCollection<LocomotiveModel> locomotives, string activePool, bool hideNonActivePool)
        {
            InitializeComponent();
            Owner = Application.Current.MainWindow; // Définit la fenêtre principale comme propriétaire
            WindowStartupLocation = WindowStartupLocation.CenterOwner; // Centre la fenêtre sur la principale

            _locomotives = locomotives;
            ActivePool = activePool;
            HideNonActivePool = hideNonActivePool;

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

            ActivePoolCombo.SelectedIndex = string.Equals(ActivePool, "Sibelit") ? 1 : 0;
            HideNonActiveCheckBox.IsChecked = HideNonActivePool;
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
        }

        private void ActivePoolCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (ActivePoolCombo.SelectedItem is System.Windows.Controls.ComboBoxItem item && item.Content is string poolName)
            {
                ActivePool = poolName;
            }
        }

        private void HideNonActiveCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            HideNonActivePool = HideNonActiveCheckBox.IsChecked == true;
        }
    }
}
