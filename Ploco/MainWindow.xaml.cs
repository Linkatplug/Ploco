using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Ploco.Data;
using Ploco.Dialogs;
using Ploco.Models;

namespace Ploco
{
    public partial class MainWindow : Window
    {
        private readonly PlocoRepository _repository;
        private readonly ObservableCollection<LocomotiveModel> _locomotives = new();
        private readonly ObservableCollection<TileModel> _tiles = new();
        private Point _dragStartPoint;
        private TileModel? _draggedTile;
        private Point _tileDragStart;

        public MainWindow()
        {
            InitializeComponent();
            _repository = new PlocoRepository("ploco.db");
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _repository.Initialize();
            _repository.SeedDefaultDataIfNeeded();

            LoadState();
            LocomotiveList.ItemsSource = _locomotives;
            TileCanvas.ItemsSource = _tiles;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            PersistState();
        }

        private void LoadState()
        {
            _locomotives.Clear();
            _tiles.Clear();

            var state = _repository.LoadState();
            foreach (var loco in state.Locomotives.OrderBy(l => l.SeriesName).ThenBy(l => l.Number))
            {
                _locomotives.Add(loco);
            }

            foreach (var tile in state.Tiles)
            {
                if (!tile.Tracks.Any())
                {
                    tile.Tracks.Add(CreateDefaultTrack(tile));
                }
                _tiles.Add(tile);
            }
        }

        private void PersistState()
        {
            var state = new AppState
            {
                Series = BuildSeriesState(),
                Locomotives = _locomotives.ToList(),
                Tiles = _tiles.ToList()
            };
            _repository.SaveState(state);
        }

        private List<RollingStockSeries> BuildSeriesState()
        {
            var series = new Dictionary<int, RollingStockSeries>();
            foreach (var loco in _locomotives)
            {
                if (!series.ContainsKey(loco.SeriesId))
                {
                    series[loco.SeriesId] = new RollingStockSeries
                    {
                        Id = loco.SeriesId,
                        Name = loco.SeriesName,
                        StartNumber = loco.Number,
                        EndNumber = loco.Number
                    };
                }
                var item = series[loco.SeriesId];
                item.StartNumber = Math.Min(item.StartNumber, loco.Number);
                item.EndNumber = Math.Max(item.EndNumber, loco.Number);
            }

            return series.Values.ToList();
        }

        private TrackModel CreateDefaultTrack(TileModel tile)
        {
            return tile.Type switch
            {
                TileType.Depot => new TrackModel { Name = "Sortie 1" },
                TileType.VoieGarage => new TrackModel { Name = "Voie 1" },
                _ => new TrackModel { Name = "Locomotives" }
            };
        }

        private void AddDepot_Click(object sender, RoutedEventArgs e)
        {
            AddTile(TileType.Depot, "Dépôt");
        }

        private void AddLineStop_Click(object sender, RoutedEventArgs e)
        {
            AddTile(TileType.ArretLigne, "Arrêt en ligne");
        }

        private void AddGarage_Click(object sender, RoutedEventArgs e)
        {
            AddTile(TileType.VoieGarage, "Voie de garage");
        }

        private void AddTile(TileType type, string defaultName)
        {
            var tile = new TileModel
            {
                Name = defaultName,
                Type = type,
                X = 20 + _tiles.Count * 30,
                Y = 20 + _tiles.Count * 30,
                LocationPreset = type == TileType.VoieGarage ? "Garage A" : null,
                GarageTrackNumber = type == TileType.VoieGarage ? 1 : null
            };
            tile.Tracks.Add(CreateDefaultTrack(tile));
            _tiles.Add(tile);
            _repository.AddHistory("TileCreated", $"Création de la tuile {tile.Name} ({tile.Type}).");
            PersistState();
        }

        private void DeleteTile_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is TileModel tile)
            {
                foreach (var track in tile.Tracks)
                {
                    foreach (var loco in track.Locomotives.ToList())
                    {
                        loco.AssignedTrackId = null;
                    }
                }
                _tiles.Remove(tile);
                _repository.AddHistory("TileDeleted", $"Suppression de la tuile {tile.Name}.");
                PersistState();
            }
        }

        private void RenameTile_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is TileModel tile)
            {
                var dialog = new SimpleTextDialog("Renommer la tuile", "Nom :", tile.Name) { Owner = this };
                if (dialog.ShowDialog() == true)
                {
                    tile.Name = dialog.ResponseText;
                    _repository.AddHistory("TileRenamed", $"Tuile renommée en {tile.Name}.");
                    PersistState();
                }
            }
        }

        private void ConfigureTile_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is TileModel tile)
            {
                var dialog = new TileConfigDialog(tile) { Owner = this };
                if (dialog.ShowDialog() == true)
                {
                    _repository.AddHistory("TileConfigured", $"Configuration mise à jour pour {tile.Name}.");
                    PersistState();
                }
            }
        }

        private void AddTrack_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is TileModel tile)
            {
                var trackName = tile.Type switch
                {
                    TileType.Depot => $"Sortie {tile.Tracks.Count + 1}",
                    TileType.VoieGarage => $"Voie {tile.Tracks.Count + 1}",
                    _ => $"Zone {tile.Tracks.Count + 1}"
                };
                tile.Tracks.Add(new TrackModel { Name = trackName });
                _repository.AddHistory("TrackAdded", $"Ajout de {trackName} dans {tile.Name}.");
                PersistState();
            }
        }

        private void RemoveTrack_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is TrackModel track)
            {
                var tile = _tiles.FirstOrDefault(t => t.Tracks.Contains(track));
                if (tile == null || tile.Tracks.Count <= 1)
                {
                    MessageBox.Show("Une tuile doit conserver au moins une voie.", "Action impossible", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                foreach (var loco in track.Locomotives.ToList())
                {
                    loco.AssignedTrackId = null;
                }

                tile.Tracks.Remove(track);
                _repository.AddHistory("TrackRemoved", $"Suppression de {track.Name} dans {tile.Name}.");
                PersistState();
            }
        }

        private void LocomotiveList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(null);
        }

        private void LocomotiveList_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }

            Point currentPos = e.GetPosition(null);
            Vector diff = _dragStartPoint - currentPos;
            if (Math.Abs(diff.X) <= SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(diff.Y) <= SystemParameters.MinimumVerticalDragDistance)
            {
                return;
            }

            if (LocomotiveList.SelectedItem is LocomotiveModel loco)
            {
                DragDrop.DoDragDrop(LocomotiveList, loco, DragDropEffects.Move);
            }
        }

        private void TrackLocomotives_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(null);
        }

        private void TrackLocomotives_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }

            Point currentPos = e.GetPosition(null);
            Vector diff = _dragStartPoint - currentPos;
            if (Math.Abs(diff.X) <= SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(diff.Y) <= SystemParameters.MinimumVerticalDragDistance)
            {
                return;
            }

            if (sender is ListBox listBox && listBox.SelectedItem is LocomotiveModel loco)
            {
                DragDrop.DoDragDrop(listBox, loco, DragDropEffects.Move);
            }
        }

        private void TrackLocomotives_Drop(object sender, DragEventArgs e)
        {
            if (sender is not ListBox listBox || listBox.DataContext is not TrackModel track)
            {
                return;
            }

            if (e.Data.GetDataPresent(typeof(LocomotiveModel)))
            {
                var loco = (LocomotiveModel)e.Data.GetData(typeof(LocomotiveModel))!;
                var insertIndex = GetInsertIndex(listBox, e.GetPosition(listBox));
                MoveLocomotiveToTrack(loco, track, insertIndex);
                PersistState();
                e.Handled = true;
            }
        }

        private void Tile_Drop(object sender, DragEventArgs e)
        {
            if (sender is not Border border || border.DataContext is not TileModel tile)
            {
                return;
            }

            if (e.Data.GetDataPresent(typeof(LocomotiveModel)))
            {
                var loco = (LocomotiveModel)e.Data.GetData(typeof(LocomotiveModel))!;
                var targetTrack = tile.Tracks.FirstOrDefault();
                if (targetTrack != null)
                {
                    MoveLocomotiveToTrack(loco, targetTrack, targetTrack.Locomotives.Count);
                    PersistState();
                }
            }
        }

        private void MoveLocomotiveToTrack(LocomotiveModel loco, TrackModel targetTrack, int insertIndex)
        {
            var currentTrack = _tiles.SelectMany(t => t.Tracks).FirstOrDefault(t => t.Locomotives.Contains(loco));
            if (currentTrack != null)
            {
                var currentIndex = currentTrack.Locomotives.IndexOf(loco);
                currentTrack.Locomotives.Remove(loco);
                if (currentTrack == targetTrack && insertIndex > currentIndex)
                {
                    insertIndex--;
                }
            }

            if (!targetTrack.Locomotives.Contains(loco))
            {
                if (insertIndex < 0 || insertIndex > targetTrack.Locomotives.Count)
                {
                    targetTrack.Locomotives.Add(loco);
                }
                else
                {
                    targetTrack.Locomotives.Insert(insertIndex, loco);
                }
            }

            loco.AssignedTrackId = targetTrack.Id;
            _repository.AddHistory("LocomotiveMoved", $"Loco {loco.DisplayName} déplacée vers {targetTrack.Name}.");
        }

        private static int GetInsertIndex(ListBox listBox, Point dropPosition)
        {
            var element = listBox.InputHitTest(dropPosition) as DependencyObject;
            while (element != null && element is not ListBoxItem)
            {
                element = VisualTreeHelper.GetParent(element);
            }

            if (element is ListBoxItem item)
            {
                return listBox.ItemContainerGenerator.IndexFromContainer(item);
            }

            return listBox.Items.Count;
        }

        private void MenuItem_ModifierStatut_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Parent is ContextMenu contextMenu)
            {
                if (contextMenu.PlacementTarget is FrameworkElement element && element.DataContext is LocomotiveModel loco)
                {
                    var dialog = new StatusDialog(loco) { Owner = this };
                    if (dialog.ShowDialog() == true)
                    {
                        _repository.AddHistory("StatusChanged", $"Statut modifié pour {loco.DisplayName}.");
                        PersistState();
                    }
                }
            }
        }

        private void MenuItem_RemoveFromTile_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Parent is ContextMenu contextMenu)
            {
                if (contextMenu.PlacementTarget is FrameworkElement element && element.DataContext is LocomotiveModel loco)
                {
                    var track = _tiles.SelectMany(t => t.Tracks).FirstOrDefault(t => t.Locomotives.Contains(loco));
                    if (track != null)
                    {
                        track.Locomotives.Remove(loco);
                        loco.AssignedTrackId = null;
                        _repository.AddHistory("LocomotiveRemoved", $"Loco {loco.DisplayName} retirée de {track.Name}.");
                        PersistState();
                    }
                }
            }
        }

        private void Tile_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is DependencyObject source && FindAncestor<Button>(source) != null)
            {
                return;
            }

            if (sender is Border border && border.DataContext is TileModel tile)
            {
                _draggedTile = tile;
                _tileDragStart = e.GetPosition(TileCanvas);
                border.CaptureMouse();
            }
        }

        private void Tile_MouseMove(object sender, MouseEventArgs e)
        {
            if (_draggedTile == null || e.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }

            var currentPosition = e.GetPosition(TileCanvas);
            var offset = currentPosition - _tileDragStart;
            _draggedTile.X = Math.Max(0, _draggedTile.X + offset.X);
            _draggedTile.Y = Math.Max(0, _draggedTile.Y + offset.Y);
            _tileDragStart = currentPosition;
        }

        private void Tile_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_draggedTile != null)
            {
                _repository.AddHistory("TileMoved", $"Tuile {_draggedTile.Name} déplacée.");
                PersistState();
            }
            if (sender is Border border)
            {
                border.ReleaseMouseCapture();
            }
            _draggedTile = null;
        }

        private static T? FindAncestor<T>(DependencyObject child) where T : DependencyObject
        {
            while (child != null)
            {
                if (child is T match)
                {
                    return match;
                }
                child = VisualTreeHelper.GetParent(child);
            }
            return null;
        }
    }
}
