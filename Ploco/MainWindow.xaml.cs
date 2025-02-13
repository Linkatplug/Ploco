using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Newtonsoft.Json;
using Ploco.Models;

namespace Ploco
{
    // Classe pour transporter les données de drag-drop.
    public class LocoDragData
    {
        public Locomotive Loco { get; set; }
        public Border SourceElement { get; set; }
    }

    // Classe pour sauvegarder/charger l'état de l'application.
    public class AppState
    {
        public ObservableCollection<Locomotive> LineasPool { get; set; }
        public ObservableCollection<Locomotive> SibelitPool { get; set; }
    }

    public partial class MainWindow : Window
    {
        private Point _dragStartPoint;
        private ObservableCollection<Locomotive> lineasPool = new ObservableCollection<Locomotive>();
        private ObservableCollection<Locomotive> sibelitPool = new ObservableCollection<Locomotive>();

        public MainWindow()
        {
            InitializeComponent();

            // Charger l'état sauvegardé
            ChargerEtat();

            // Si les collections sont vides, initialiser par défaut.
            if (lineasPool.Count == 0 || sibelitPool.Count == 0)
            {
                for (int i = 1301; i <= 1320; i++)
                {
                    lineasPool.Add(new Locomotive { NumeroSerie = i, IsOnCanvas = false, Statut = StatutLocomotive.Ok });
                }
                // Par défaut, on remet toutes les locomotives dans la pool Lineas (aucun dans Sibelit)
                // Vous pouvez ajuster ceci selon vos besoins
                sibelitPool.Clear();
            }

            // Lier l'ItemsControl à la pool Sibelit.
            PoolItemsControl.ItemsSource = sibelitPool;
            sibelitPool.CollectionChanged += PoolSibelit_CollectionChanged;
            CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(sibelitPool);
            view.SortDescriptions.Add(new SortDescription("NumeroSerie", ListSortDirection.Ascending));
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            SauvegarderEtat();
        }

        private void SauvegarderEtat()
        {
            var state = new AppState
            {
                LineasPool = lineasPool,
                SibelitPool = sibelitPool
            };
            string json = JsonConvert.SerializeObject(state, Newtonsoft.Json.Formatting.Indented);
            System.IO.File.WriteAllText("etat.json", json);
        }

        private void ChargerEtat()
        {
            if (System.IO.File.Exists("etat.json"))
            {
                string json = System.IO.File.ReadAllText("etat.json");
                AppState state = JsonConvert.DeserializeObject<AppState>(json);
                if (state != null)
                {
                    lineasPool.Clear();
                    sibelitPool.Clear();
                    foreach (var loco in state.LineasPool)
                        lineasPool.Add(loco);
                    foreach (var loco in state.SibelitPool)
                        sibelitPool.Add(loco);
                }
            }
        }

        private void PoolSibelit_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add ||
                e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (var item in (e.NewItems ?? e.OldItems))
                {
                    if (item is Locomotive loco)
                    {
                        Border canvasItem = FindCanvasItemForLoco(loco);
                        if (canvasItem != null)
                        {
                            MonCanvas.Children.Remove(canvasItem);
                            loco.IsOnCanvas = false;
                        }
                    }
                }
            }
        }

        private Border FindCanvasItemForLoco(Locomotive loco)
        {
            foreach (UIElement element in MonCanvas.Children)
            {
                if (element is Border b && b.Tag is Locomotive tagLoco && tagLoco.NumeroSerie == loco.NumeroSerie)
                {
                    return b;
                }
            }
            return null;
        }

        // --- Gestion du drag depuis la pool ---
        private void PoolItemsControl_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(null);
        }

        private void PoolItemsControl_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Point currentPos = e.GetPosition(null);
                Vector diff = _dragStartPoint - currentPos;
                if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    DependencyObject obj = e.OriginalSource as DependencyObject;
                    while (obj != null && !(obj is Border))
                    {
                        obj = VisualTreeHelper.GetParent(obj);
                    }
                    if (obj is Border border && border.DataContext is Locomotive loco)
                    {
                        LocoDragData dragData = new LocoDragData { Loco = loco, SourceElement = null };
                        DragDrop.DoDragDrop(border, dragData, DragDropEffects.Move);
                    }
                }
            }
        }

        // --- Gestion du drag-over et du drop sur le tapis (Canvas) ---
        private void MonCanvas_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(LocoDragData)))
                e.Effects = DragDropEffects.Move;
            else
                e.Effects = DragDropEffects.None;
            e.Handled = true;
        }

        private void MonCanvas_Drop(object sender, DragEventArgs e)
        {
            Point dropPosition = e.GetPosition(MonCanvas);
            if (e.Data.GetDataPresent(typeof(LocoDragData)))
            {
                LocoDragData dragData = e.Data.GetData(typeof(LocoDragData)) as LocoDragData;
                if (dragData != null)
                {
                    Locomotive loco = dragData.Loco;
                    if (loco != null)
                    {
                        if (dragData.SourceElement != null)
                        {
                            Border canvasItem = dragData.SourceElement;
                            if (!MonCanvas.Children.Contains(canvasItem))
                                MonCanvas.Children.Add(canvasItem);
                            Canvas.SetLeft(canvasItem, dropPosition.X - canvasItem.Width / 2);
                            Canvas.SetTop(canvasItem, dropPosition.Y - canvasItem.Height / 2);
                        }
                        else
                        {
                            Border existing = FindCanvasItemForLoco(loco);
                            if (existing != null)
                            {
                                Canvas.SetLeft(existing, dropPosition.X - existing.Width / 2);
                                Canvas.SetTop(existing, dropPosition.Y - existing.Height / 2);
                            }
                            else
                            {
                                Border canvasItem = new Border
                                {
                                    Width = 40,
                                    Height = 30,
                                    Tag = loco,
                                    ContextMenu = (ContextMenu)this.Resources["LocomotivesContextMenu"]
                                };
                                // Lier le Background au statut via le convertisseur.
                                var converter = this.Resources["StatutToBrushConverter"] as IValueConverter;
                                Binding binding = new Binding("Statut")
                                {
                                    Source = loco,
                                    Converter = converter
                                };
                                canvasItem.SetBinding(Border.BackgroundProperty, binding);

                                TextBlock tb = new TextBlock
                                {
                                    Text = loco.NumeroSerie.ToString(),
                                    Foreground = Brushes.White,
                                    VerticalAlignment = VerticalAlignment.Center,
                                    HorizontalAlignment = HorizontalAlignment.Center,
                                    FontWeight = FontWeights.Bold
                                };
                                canvasItem.Child = tb;
                                canvasItem.MouseLeftButtonDown += CanvasItem_MouseLeftButtonDown;
                                canvasItem.MouseMove += CanvasItem_MouseMove;
                                Canvas.SetLeft(canvasItem, dropPosition.X - canvasItem.Width / 2);
                                Canvas.SetTop(canvasItem, dropPosition.Y - canvasItem.Height / 2);
                                MonCanvas.Children.Add(canvasItem);
                                loco.IsOnCanvas = true;
                            }
                        }
                    }
                }
            }
        }

        // --- Gestion du drag depuis le tapis (Canvas) ---
        private void CanvasItem_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(null);
        }

        private void CanvasItem_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Point currentPos = e.GetPosition(null);
                Vector diff = _dragStartPoint - currentPos;
                if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    Border border = sender as Border;
                    if (border != null && border.Tag is Locomotive loco)
                    {
                        LocoDragData dragData = new LocoDragData { Loco = loco, SourceElement = border };
                        DragDrop.DoDragDrop(border, dragData, DragDropEffects.Move);
                    }
                }
            }
        }

        // --- Gestion du drag-over et du drop sur la zone du pool (pool Sibelit) ---
        private void Pool_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(LocoDragData)))
                e.Effects = DragDropEffects.Move;
            else
                e.Effects = DragDropEffects.None;
            e.Handled = true;
        }

        private void Pool_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(LocoDragData)))
            {
                LocoDragData dragData = e.Data.GetData(typeof(LocoDragData)) as LocoDragData;
                if (dragData != null)
                {
                    Locomotive loco = dragData.Loco;
                    if (loco != null)
                    {
                        if (!sibelitPool.Contains(loco))
                            sibelitPool.Add(loco);
                        Border canvasItem = FindCanvasItemForLoco(loco);
                        if (canvasItem != null)
                        {
                            MonCanvas.Children.Remove(canvasItem);
                            loco.IsOnCanvas = false;
                        }
                    }
                }
            }
        }

        // --- Gestion du clic droit pour Swap et Modifier statut ---
        private void MenuItem_Swap_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;
            if (menuItem != null)
            {
                ContextMenu contextMenu = menuItem.Parent as ContextMenu;
                if (contextMenu != null)
                {
                    // Le PlacementTarget du ContextMenu est l'élément visuel sur lequel on a cliqué.
                    Border border = contextMenu.PlacementTarget as Border;
                    if (border != null)
                    {
                        // Si l'élément provient du pool, il doit avoir son DataContext, sinon s'il provient du Canvas, utilisez Tag.
                        Locomotive locoFromSibelit = border.DataContext as Locomotive ?? border.Tag as Locomotive;
                        if (locoFromSibelit != null)
                        {
                            // Ouvrir la fenêtre de swap en passant la loco et le pool Lineas.
                            SwapDialog dialog = new SwapDialog(locoFromSibelit, lineasPool);
                            bool? result = dialog.ShowDialog();
                            if (result == true)
                            {
                                // Récupérer la loco sélectionnée dans le ComboBox du dialog.
                                Locomotive locoFromLineas = dialog.SelectedLoco;
                                if (locoFromLineas != null)
                                {
                                    // Si la loco de Sibelit est sur le tapis, on la retire (mettre IsOnCanvas à false).
                                    Border canvasItem = FindCanvasItemForLoco(locoFromSibelit);
                                    if (canvasItem != null)
                                    {
                                        MonCanvas.Children.Remove(canvasItem);
                                        locoFromSibelit.IsOnCanvas = false;
                                    }
                                    // Échanger les locomotives entre les pools.
                                    if (sibelitPool.Contains(locoFromSibelit))
                                        sibelitPool.Remove(locoFromSibelit);
                                    if (!lineasPool.Contains(locoFromSibelit))
                                        lineasPool.Add(locoFromSibelit);

                                    if (lineasPool.Contains(locoFromLineas))
                                        lineasPool.Remove(locoFromLineas);
                                    if (!sibelitPool.Contains(locoFromLineas))
                                        sibelitPool.Add(locoFromLineas);
                                }
                            }
                        }
                    }
                }
            }
        }

        private void MenuItem_ModifierStatut_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;
            if (menuItem != null)
            {
                ContextMenu contextMenu = menuItem.Parent as ContextMenu;
                if (contextMenu != null)
                {
                    Border border = contextMenu.PlacementTarget as Border;
                    if (border != null)
                    {
                        Locomotive loco = border.DataContext as Locomotive ?? border.Tag as Locomotive;
                        if (loco != null)
                        {
                            ModifierStatutDialog dialog = new ModifierStatutDialog(loco);
                            bool? result = dialog.ShowDialog();
                            if (result == true)
                            {
                                loco.Statut = dialog.NewStatut;
                            }
                        }
                    }
                }
            }
        }

        // --- Gestion des menus du haut ---
        private void MenuItem_Sauvegarder_Click(object sender, RoutedEventArgs e)
        {
            SauvegarderEtat();
            MessageBox.Show("Etat sauvegardé", "Sauvegarder", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void MenuItem_Charger_Click(object sender, RoutedEventArgs e)
        {
            ChargerEtat();
            MessageBox.Show("Etat chargé", "Charger", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void MenuItem_GererLocomotives_Click(object sender, RoutedEventArgs e)
        {
            PoolTransferWindow transferWindow = new PoolTransferWindow(lineasPool, sibelitPool);
            transferWindow.ShowDialog();
        }

        private void MenuItem_ParcLoco_Click(object sender, RoutedEventArgs e)
        {
            ParcLocoWindow parcWindow = new ParcLocoWindow(sibelitPool, lineasPool);
            parcWindow.ShowDialog();
        }

        private void MenuItem_Historique_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Fonction Historique en cours de développement.", "Historique", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void MenuItem_Reset_Click(object sender, RoutedEventArgs e)
        {
            // Réinitialiser : vider les deux pools et recréer toutes les locomotives dans lineasPool uniquement.
            lineasPool.Clear();
            sibelitPool.Clear();
            for (int i = 1301; i <= 1320; i++)
            {
                lineasPool.Add(new Locomotive { NumeroSerie = i, IsOnCanvas = false, Statut = StatutLocomotive.Ok });
            }
            MessageBox.Show("Réinitialisation effectuée. Toutes les locomotives sont dans la pool Lineas.", "Reset", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // --- Boutons supplémentaires (appelés via le menu "Fichier", "Gestion" et "Option") ---
        private void ParcLoco_Click(object sender, RoutedEventArgs e)
        {
            ParcLocoWindow parcWindow = new ParcLocoWindow(sibelitPool, lineasPool);
            parcWindow.ShowDialog();
        }

        private void Historique_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Fonction Historique en cours de développement.", "Historique", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OuvrirFenetreTransfert_Click(object sender, RoutedEventArgs e)
        {
            PoolTransferWindow transferWindow = new PoolTransferWindow(lineasPool, sibelitPool);
            transferWindow.ShowDialog();
        }
    }
}
