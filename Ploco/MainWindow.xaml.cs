using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
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
        private readonly List<LayoutPreset> _layoutPresets = new();
        private const string LayoutPresetFileName = "layout_presets.json";
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
            LoadLayoutPresets();
            RefreshPresetMenu();
            LocomotiveList.ItemsSource = _locomotives;
            TileCanvas.ItemsSource = _tiles;
            InitializeLocomotiveView();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var result = MessageBox.Show("Êtes-vous sûr de vouloir quitter ?", "Quitter", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
            {
                e.Cancel = true;
                return;
            }

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
                EnsureDefaultTracks(tile);
                _tiles.Add(tile);
            }

            UpdatePoolVisibility();
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

        private void InitializeLocomotiveView()
        {
            var view = CollectionViewSource.GetDefaultView(_locomotives);
            view.SortDescriptions.Clear();
            view.SortDescriptions.Add(new System.ComponentModel.SortDescription(nameof(LocomotiveModel.Number), System.ComponentModel.ListSortDirection.Ascending));
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
                TileType.Depot => new TrackModel { Name = "Locomotives", Kind = TrackKind.Main },
                TileType.VoieGarage => new TrackModel { Name = "Vrac", Kind = TrackKind.Main },
                _ => new TrackModel { Name = "Locomotives", Kind = TrackKind.Main }
            };
        }

        private void AddPlace_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new PlaceDialog { Owner = this };
            if (dialog.ShowDialog() == true)
            {
                if (dialog.SelectedType == TileType.ArretLigne)
                {
                    var lineDialog = new LinePlaceDialog(dialog.SelectedName) { Owner = this };
                    if (lineDialog.ShowDialog() == true)
                    {
                        AddLineTile(lineDialog);
                    }
                    return;
                }

                AddTile(dialog.SelectedType, dialog.SelectedName);
            }
        }

        private void AddTile(TileType type, string name)
        {
            var tile = new TileModel
            {
                Name = name,
                Type = type,
                X = 20 + _tiles.Count * 30,
                Y = 20 + _tiles.Count * 30
            };
            EnsureDefaultTracks(tile);
            ApplyGaragePresets(tile);
            _tiles.Add(tile);
            _repository.AddHistory("TileCreated", $"Création du lieu {tile.DisplayTitle}.");
            PersistState();
        }

        private void AddLineTile(LinePlaceDialog dialog)
        {
            var tile = new TileModel
            {
                Name = dialog.PlaceName,
                Type = TileType.ArretLigne,
                X = 20 + _tiles.Count * 30,
                Y = 20 + _tiles.Count * 30
            };

            var track = new TrackModel
            {
                Name = dialog.TrackName,
                Kind = TrackKind.Line,
                IsOnTrain = dialog.IsOnTrain,
                TrainNumber = dialog.IsOnTrain ? dialog.TrainNumber : null,
                StopTime = dialog.IsOnTrain ? dialog.StopTime : null,
                IssueReason = dialog.IssueReason,
                IsLocomotiveHs = dialog.IsLocomotiveHs
            };

            tile.Tracks.Add(track);
            tile.RefreshTrackCollections();
            _tiles.Add(tile);
            _repository.AddHistory("TileCreated", $"Création du lieu {tile.DisplayTitle}.");
            _repository.AddHistory("LineTrackAdded", $"Ajout de la voie {track.Name} dans {tile.DisplayTitle}.");

            var missingLocos = new List<string>();
            var unavailableLocos = new List<string>();
            var invalidLocos = new List<string>();
            var locomotiveNumbers = ParseLocomotiveNumbers(dialog.LocomotiveNumbers, invalidLocos);
            foreach (var number in locomotiveNumbers)
            {
                var loco = _locomotives.FirstOrDefault(l => l.Number == number);
                if (loco == null)
                {
                    missingLocos.Add(number.ToString());
                    continue;
                }

                if (loco.AssignedTrackId != null)
                {
                    unavailableLocos.Add(number.ToString());
                    continue;
                }

                MoveLocomotiveToTrack(loco, track, track.Locomotives.Count);
            }

            var missingHsLocos = new List<string>();
            var invalidHsLocos = new List<string>();
            if (dialog.IsLocomotiveHs)
            {
                var hsNumbers = ParseLocomotiveNumbers(dialog.HsLocomotiveNumbers, invalidHsLocos);
                foreach (var number in hsNumbers)
                {
                    var loco = _locomotives.FirstOrDefault(l => l.Number == number);
                    if (loco == null)
                    {
                        missingHsLocos.Add(number.ToString());
                        continue;
                    }

                    if (loco.Status != LocomotiveStatus.HS)
                    {
                        loco.Status = LocomotiveStatus.HS;
                        _repository.AddHistory("StatusChanged", $"Statut modifié pour {loco.Number} (HS).");
                    }
                }
            }

            if (invalidLocos.Any() || invalidHsLocos.Any())
            {
                MessageBox.Show($"Numéros invalides ignorés : {string.Join(", ", invalidLocos.Concat(invalidHsLocos))}.",
                    "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            if (missingLocos.Any() || missingHsLocos.Any())
            {
                var missingMessage = new List<string>();
                if (missingLocos.Any())
                {
                    missingMessage.Add($"Locomotives introuvables pour l'ajout : {string.Join(", ", missingLocos)}.");
                }
                if (missingHsLocos.Any())
                {
                    missingMessage.Add($"Locomotives introuvables pour HS : {string.Join(", ", missingHsLocos)}.");
                }
                MessageBox.Show(string.Join(Environment.NewLine, missingMessage), "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            if (unavailableLocos.Any())
            {
                MessageBox.Show($"Locomotives déjà affectées ignorées : {string.Join(", ", unavailableLocos)}.",
                    "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            UpdatePoolVisibility();
            PersistState();
        }

        private void DeleteTile_Click(object sender, RoutedEventArgs e)
        {
            var tile = GetTileFromSender(sender);
            if (tile == null)
            {
                return;
            }

            foreach (var track in tile.Tracks.ToList())
            {
                foreach (var loco in track.Locomotives.ToList())
                {
                    track.Locomotives.Remove(loco);
                    loco.AssignedTrackId = null;
                }
            }
            _tiles.Remove(tile);
            _repository.AddHistory("TileDeleted", $"Suppression du lieu {tile.DisplayTitle}.");
            UpdatePoolVisibility();
            PersistState();
        }

        private void RenameTile_Click(object sender, RoutedEventArgs e)
        {
            var tile = GetTileFromSender(sender);
            if (tile == null)
            {
                return;
            }

            var dialog = new SimpleTextDialog("Renommer la tuile", "Nom :", tile.Name) { Owner = this };
            if (dialog.ShowDialog() == true)
            {
                tile.Name = dialog.ResponseText;
                _repository.AddHistory("TileRenamed", $"Tuile renommée en {tile.Name}.");
                PersistState();
            }
        }

        private void AddDepotOutput_Click(object sender, RoutedEventArgs e)
        {
            var tile = GetTileFromSender(sender);
            if (tile == null)
            {
                return;
            }

            if (tile.OutputTracks.Any())
            {
                MessageBox.Show("Une seule voie de sortie est autorisée.", "Action impossible", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var track = new TrackModel
            {
                Name = "Voie de sortie",
                Kind = TrackKind.Output
            };
            tile.Tracks.Add(track);
            tile.RefreshTrackCollections();
            _repository.AddHistory("TrackAdded", $"Ajout de la voie de sortie dans {tile.DisplayTitle}.");
            PersistState();
        }

        private void AddGarageZone_Click(object sender, RoutedEventArgs e)
        {
            var tile = GetTileFromSender(sender);
            if (tile == null)
            {
                return;
            }

            var dialog = new SimpleTextDialog("Ajouter une zone", "Nom de zone :", "Zone 1") { Owner = this };
            if (dialog.ShowDialog() == true)
            {
                var track = new TrackModel
                {
                    Name = dialog.ResponseText,
                    Kind = TrackKind.Zone
                };
                tile.Tracks.Add(track);
                tile.RefreshTrackCollections();
                _repository.AddHistory("ZoneAdded", $"Ajout de la zone {track.Name} dans {tile.DisplayTitle}.");
                PersistState();
            }
        }

        private void RenameZone_Click(object sender, RoutedEventArgs e)
        {
            var track = GetTrackFromSender(sender);
            if (track != null)
            {
                var dialog = new SimpleTextDialog("Renommer la zone", "Nom de zone :", track.Name) { Owner = this };
                if (dialog.ShowDialog() == true)
                {
                    track.Name = dialog.ResponseText;
                    _repository.AddHistory("ZoneRenamed", $"Zone renommée en {track.Name}.");
                    PersistState();
                }
            }
        }

        private void RemoveZone_Click(object sender, RoutedEventArgs e)
        {
            var track = GetTrackFromSender(sender);
            if (track != null)
            {
                var tile = _tiles.FirstOrDefault(t => t.Tracks.Contains(track));
                if (tile == null)
                {
                    return;
                }

                foreach (var loco in track.Locomotives.ToList())
                {
                    track.Locomotives.Remove(loco);
                    loco.AssignedTrackId = null;
                }

                tile.Tracks.Remove(track);
                tile.RefreshTrackCollections();
                _repository.AddHistory("ZoneRemoved", $"Suppression de la zone {track.Name} dans {tile.DisplayTitle}.");
                UpdatePoolVisibility();
                PersistState();
            }
        }

        private void AddLineTrack_Click(object sender, RoutedEventArgs e)
        {
            var tile = GetTileFromSender(sender);
            if (tile == null)
            {
                return;
            }

            var dialog = new LineTrackDialog { Owner = this };
            if (dialog.ShowDialog() == true)
            {
                var track = dialog.BuildTrack();
                track.Kind = TrackKind.Line;
                tile.Tracks.Add(track);
                tile.RefreshTrackCollections();
                _repository.AddHistory("LineTrackAdded", $"Ajout de la voie {track.Name} dans {tile.DisplayTitle}.");
                PersistState();
            }
        }

        private void RemoveLineTrack_Click(object sender, RoutedEventArgs e)
        {
            var track = GetTrackFromSender(sender);
            if (track != null)
            {
                var tile = _tiles.FirstOrDefault(t => t.Tracks.Contains(track));
                if (tile == null)
                {
                    return;
                }

                foreach (var loco in track.Locomotives.ToList())
                {
                    track.Locomotives.Remove(loco);
                    loco.AssignedTrackId = null;
                }

                tile.Tracks.Remove(track);
                tile.RefreshTrackCollections();
                _repository.AddHistory("LineTrackRemoved", $"Suppression de la voie {track.Name} dans {tile.DisplayTitle}.");
                UpdatePoolVisibility();
                PersistState();
            }
        }

        private void RenameLineTrack_Click(object sender, RoutedEventArgs e)
        {
            var track = GetTrackFromSender(sender);
            if (track == null)
            {
                return;
            }

            var dialog = new SimpleTextDialog("Renommer la voie", "Nom de voie :", track.Name) { Owner = this };
            if (dialog.ShowDialog() == true)
            {
                track.Name = dialog.ResponseText;
                _repository.AddHistory("LineTrackRenamed", $"Voie renommée en {track.Name}.");
                PersistState();
            }
        }

        private void ToggleLeftBlocked_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is TrackModel track)
            {
                track.IsLeftBlocked = !track.IsLeftBlocked;
                _repository.AddHistory("ZoneBlockedUpdated", $"Mise à jour du remplissage BLOCK pour {track.Name}.");
                PersistState();
            }
        }

        private void ToggleRightBlocked_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is TrackModel track)
            {
                track.IsRightBlocked = !track.IsRightBlocked;
                _repository.AddHistory("ZoneBlockedUpdated", $"Mise à jour du remplissage BIF pour {track.Name}.");
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

        private void LocomotiveList_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(typeof(LocomotiveModel)))
            {
                return;
            }

            var loco = (LocomotiveModel)e.Data.GetData(typeof(LocomotiveModel))!;
            var track = _tiles.SelectMany(t => t.Tracks).FirstOrDefault(t => t.Locomotives.Contains(loco));
            if (track != null)
            {
                track.Locomotives.Remove(loco);
                loco.AssignedTrackId = null;
                _repository.AddHistory("LocomotiveRemoved", $"Loco {loco.Number} retirée de {track.Name}.");
            }

            UpdatePoolVisibility();
            PersistState();
            e.Handled = true;
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
                var targetTrack = GetDefaultDropTrack(tile);
                if (targetTrack != null)
                {
                    MoveLocomotiveToTrack(loco, targetTrack, targetTrack.Locomotives.Count);
                    PersistState();
                }
                else
                {
                    MessageBox.Show("Aucune voie disponible pour déposer une locomotive.", "Action impossible", MessageBoxButton.OK, MessageBoxImage.Warning);
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
            _repository.AddHistory("LocomotiveMoved", $"Loco {loco.Number} déplacée vers {targetTrack.Name}.");
            UpdatePoolVisibility();
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
                        _repository.AddHistory("StatusChanged", $"Statut modifié pour {loco.Number}.");
                        PersistState();
                    }
                }
            }
        }

        private void MenuItem_SwapPool_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Parent is ContextMenu contextMenu)
            {
                if (contextMenu.PlacementTarget is FrameworkElement element && element.DataContext is LocomotiveModel loco)
                {
                    SwapLocomotivePool(loco);
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
                        _repository.AddHistory("LocomotiveRemoved", $"Loco {loco.Number} retirée de {track.Name}.");
                        UpdatePoolVisibility();
                        PersistState();
                    }
                }
            }
        }

        private void Tile_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is DependencyObject source
                && (FindAncestor<Button>(source) != null
                    || FindAncestor<MenuItem>(source) != null
                    || FindAncestor<Menu>(source) != null))
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
                ResolveTileOverlap(_draggedTile);
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

        private void UpdatePoolVisibility()
        {
            foreach (var loco in _locomotives)
            {
                loco.IsVisibleInActivePool = string.Equals(loco.Pool, "Sibelit", StringComparison.OrdinalIgnoreCase)
                                             && loco.AssignedTrackId == null;
            }

            LocomotiveList.Items.Refresh();
        }

        private void SwapLocomotivePool(LocomotiveModel loco)
        {
            var previousPool = loco.Pool;
            loco.Pool = string.Equals(loco.Pool, "Lineas", StringComparison.OrdinalIgnoreCase) ? "Sibelit" : "Lineas";
            _repository.AddHistory("PoolSwapped", $"Loco {loco.Number} déplacée de {previousPool} vers {loco.Pool}.");
            UpdatePoolVisibility();
            PersistState();
        }

        private void MenuItem_Save_Click(object sender, RoutedEventArgs e)
        {
            PersistState();
            var dialog = new SaveFileDialog
            {
                Filter = "Base de données Ploco (*.db)|*.db|Tous les fichiers (*.*)|*.*",
                FileName = "ploco.db"
            };
            if (dialog.ShowDialog() == true)
            {
                _repository.CopyDatabaseTo(dialog.FileName);
            }
        }

        private void MenuItem_Load_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Base de données Ploco (*.db)|*.db|Tous les fichiers (*.*)|*.*"
            };
            if (dialog.ShowDialog() == true)
            {
                if (!_repository.ReplaceDatabaseWith(dialog.FileName))
                {
                    MessageBox.Show("Le fichier sélectionné n'est pas une base SQLite valide.", "Chargement", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _repository.Initialize();
                LoadState();
            }
        }

        private void MenuItem_PoolManagement_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new PoolTransferWindow(_locomotives)
            {
                Owner = this
            };
            dialog.ShowDialog();
            UpdatePoolVisibility();
            PersistState();
        }

        private void MenuItem_History_Click(object sender, RoutedEventArgs e)
        {
            var history = _repository.LoadHistory();
            var dialog = new HistoriqueWindow(history) { Owner = this };
            dialog.ShowDialog();
        }

        private void MenuItem_TapisT13_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new TapisT13Window(_locomotives, _tiles) { Owner = this };
            dialog.ShowDialog();
        }

        private void MenuItem_ResetLocomotives_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Réinitialiser toutes les locomotives ?", "Réinitialisation des locomotives", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            foreach (var track in _tiles.SelectMany(tile => tile.Tracks))
            {
                track.Locomotives.Clear();
            }

            foreach (var loco in _locomotives)
            {
                loco.AssignedTrackId = null;
                loco.Status = LocomotiveStatus.Ok;
                loco.TractionPercent = null;
                loco.HsReason = null;
                loco.Pool = "Lineas";
            }

            _repository.AddHistory("ResetLocomotives", "Réinitialisation des locomotives.");
            UpdatePoolVisibility();
            PersistState();
        }

        private void MenuItem_ResetTiles_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Supprimer toutes les tuiles ?", "Réinitialisation des tuiles", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            foreach (var tile in _tiles.ToList())
            {
                foreach (var track in tile.Tracks)
                {
                    foreach (var loco in track.Locomotives.ToList())
                    {
                        track.Locomotives.Remove(loco);
                        loco.AssignedTrackId = null;
                    }
                }
            }

            _tiles.Clear();
            _repository.AddHistory("ResetTiles", "Suppression de toutes les tuiles.");
            UpdatePoolVisibility();
            PersistState();
        }

        private void SaveLayoutPreset_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SimpleTextDialog("Enregistrer un preset", "Nom du preset :", "Nouveau preset") { Owner = this };
            if (dialog.ShowDialog() != true)
            {
                return;
            }

            var name = dialog.ResponseText.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Veuillez saisir un nom valide.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var preset = BuildLayoutPreset(name);
            var existing = _layoutPresets.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                _layoutPresets.Remove(existing);
            }
            _layoutPresets.Add(preset);
            SaveLayoutPresets();
            RefreshPresetMenu();
            _repository.AddHistory("LayoutPresetSaved", $"Preset enregistré : {name}.");
        }

        private void LoadLayoutPreset_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem menuItem || menuItem.Tag is not LayoutPreset preset)
            {
                return;
            }

            ApplyLayoutPreset(preset);
            _repository.AddHistory("LayoutPresetLoaded", $"Preset chargé : {preset.Name}.");
            UpdatePoolVisibility();
            PersistState();
        }

        private void DeleteLayoutPreset_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem menuItem || menuItem.Tag is not LayoutPreset preset)
            {
                return;
            }

            var result = MessageBox.Show($"Supprimer le preset \"{preset.Name}\" ?", "Supprimer preset",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            _layoutPresets.Remove(preset);
            SaveLayoutPresets();
            RefreshPresetMenu();
            _repository.AddHistory("LayoutPresetDeleted", $"Preset supprimé : {preset.Name}.");
        }

        private TrackModel? GetDefaultDropTrack(TileModel tile)
        {
            if (tile.Type == TileType.ArretLigne)
            {
                return tile.LineTracks.FirstOrDefault();
            }

            return tile.MainTrack;
        }

        private void EnsureDefaultTracks(TileModel tile)
        {
            if (!tile.Tracks.Any() && tile.Type != TileType.ArretLigne)
            {
                tile.Tracks.Add(CreateDefaultTrack(tile));
            }
            tile.RefreshTrackCollections();
        }

        private void ApplyGaragePresets(TileModel tile)
        {
            if (tile.Type != TileType.VoieGarage)
            {
                return;
            }

            if (string.Equals(tile.Name, "Zeebrugge", StringComparison.OrdinalIgnoreCase))
            {
                RemoveMainTrack(tile);
                AddGarageZone(tile, "BRAM");
                AddGarageZone(tile, "ZWAN");
            }

            if (string.Equals(tile.Name, "Anvers Nord", StringComparison.OrdinalIgnoreCase))
            {
                RemoveMainTrack(tile);
                var blockTrack = new TrackModel
                {
                    Name = "917",
                    Kind = TrackKind.Zone,
                    LeftLabel = "BLOCK",
                    RightLabel = "BIF"
                };
                tile.Tracks.Add(blockTrack);
                tile.RefreshTrackCollections();
            }
        }

        private void AddGarageZone(TileModel tile, string name)
        {
            var track = new TrackModel
            {
                Name = name,
                Kind = TrackKind.Zone
            };
            tile.Tracks.Add(track);
            tile.RefreshTrackCollections();
        }

        private static void RemoveMainTrack(TileModel tile)
        {
            var main = tile.MainTrack;
            if (main != null)
            {
                tile.Tracks.Remove(main);
                tile.RefreshTrackCollections();
            }
        }

        private static TrackModel? GetTrackFromSender(object sender)
        {
            if (sender is FrameworkElement element)
            {
                if (element.Tag is TrackModel trackFromTag)
                {
                    return trackFromTag;
                }

                if (element.DataContext is TrackModel trackFromContext)
                {
                    return trackFromContext;
                }
            }

            return null;
        }

        private TileModel? GetTileFromSender(object sender)
        {
            if (sender is FrameworkElement element)
            {
                if (element.Tag is TileModel tileFromTag)
                {
                    return tileFromTag;
                }

                if (element.DataContext is TileModel tileFromContext)
                {
                    return tileFromContext;
                }
            }

            return null;
        }

        private void ResolveTileOverlap(TileModel tile)
        {
            const double tileWidth = 360;
            const double tileHeight = 220;
            const double step = 20;

            var hasOverlap = true;
            while (hasOverlap)
            {
                hasOverlap = false;
                foreach (var other in _tiles)
                {
                    if (ReferenceEquals(other, tile))
                    {
                        continue;
                    }

                    if (IsOverlapping(tile, other, tileWidth, tileHeight))
                    {
                        tile.X += step;
                        tile.Y += step;
                        hasOverlap = true;
                        break;
                    }
                }
            }
        }

        private static bool IsOverlapping(TileModel first, TileModel second, double width, double height)
        {
            return first.X < second.X + width
                   && first.X + width > second.X
                   && first.Y < second.Y + height
                   && first.Y + height > second.Y;
        }

        private static List<int> ParseLocomotiveNumbers(string input, List<string> invalidTokens)
        {
            var numbers = new List<int>();
            if (string.IsNullOrWhiteSpace(input))
            {
                return numbers;
            }

            var tokens = input.Split(new[] { ' ', ',', ';', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var token in tokens)
            {
                if (int.TryParse(token, out var number))
                {
                    numbers.Add(number);
                }
                else
                {
                    invalidTokens.Add(token);
                }
            }

            return numbers;
        }

        private void LoadLayoutPresets()
        {
            _layoutPresets.Clear();
            if (File.Exists(LayoutPresetFileName))
            {
                try
                {
                    var json = File.ReadAllText(LayoutPresetFileName);
                    var presets = JsonSerializer.Deserialize<List<LayoutPreset>>(json, GetPresetSerializerOptions());
                    if (presets != null)
                    {
                        _layoutPresets.AddRange(presets);
                    }
                }
                catch (Exception)
                {
                    _layoutPresets.Clear();
                }
            }

            if (_layoutPresets.All(p => !string.Equals(p.Name, "Défaut", StringComparison.OrdinalIgnoreCase)))
            {
                _layoutPresets.Add(BuildDefaultPreset());
            }

            SaveLayoutPresets();
        }

        private void SaveLayoutPresets()
        {
            var json = JsonSerializer.Serialize(_layoutPresets, GetPresetSerializerOptions());
            File.WriteAllText(LayoutPresetFileName, json);
        }

        private void RefreshPresetMenu()
        {
            if (ViewPresetsMenu == null || ViewPresetsDeleteMenu == null)
            {
                return;
            }

            ViewPresetsMenu.Items.Clear();
            ViewPresetsDeleteMenu.Items.Clear();
            foreach (var preset in _layoutPresets.OrderBy(p => p.Name))
            {
                var item = new MenuItem
                {
                    Header = preset.Name,
                    Tag = preset
                };
                item.Click += LoadLayoutPreset_Click;
                ViewPresetsMenu.Items.Add(item);

                if (!string.Equals(preset.Name, "Défaut", StringComparison.OrdinalIgnoreCase))
                {
                    var deleteItem = new MenuItem
                    {
                        Header = preset.Name,
                        Tag = preset
                    };
                    deleteItem.Click += DeleteLayoutPreset_Click;
                    ViewPresetsDeleteMenu.Items.Add(deleteItem);
                }
            }
        }

        private LayoutPreset BuildLayoutPreset(string name)
        {
            var tiles = _tiles
                .Where(tile => tile.Type != TileType.ArretLigne)
                .Select(tile => new LayoutTile
                {
                    Name = tile.Name,
                    Type = tile.Type,
                    X = tile.X,
                    Y = tile.Y,
                    Tracks = tile.Tracks.Select(track => new LayoutTrack
                    {
                        Name = track.Name,
                        Kind = track.Kind,
                        LeftLabel = track.LeftLabel,
                        RightLabel = track.RightLabel,
                        IsLeftBlocked = track.IsLeftBlocked,
                        IsRightBlocked = track.IsRightBlocked
                    }).ToList()
                }).ToList();

            return new LayoutPreset
            {
                Name = name,
                Tiles = tiles
            };
        }

        private LayoutPreset BuildDefaultPreset()
        {
            var preset = new LayoutPreset
            {
                Name = "Défaut",
                Tiles = new List<LayoutTile>()
            };

            var defaultTiles = new List<(TileType type, string name)>
            {
                (TileType.Depot, "Thionville"),
                (TileType.Depot, "Mulhouse Nord"),
                (TileType.VoieGarage, "Zeebrugge"),
                (TileType.VoieGarage, "Anvers Nord"),
                (TileType.VoieGarage, "Bale")
            };

            const double startX = 20;
            const double startY = 20;
            const double stepX = 380;
            const double stepY = 260;

            for (var index = 0; index < defaultTiles.Count; index++)
            {
                var (type, name) = defaultTiles[index];
                var tile = new TileModel
                {
                    Name = name,
                    Type = type,
                    X = startX + (index % 2) * stepX,
                    Y = startY + (index / 2) * stepY
                };
                EnsureDefaultTracks(tile);
                ApplyGaragePresets(tile);

                preset.Tiles.Add(new LayoutTile
                {
                    Name = tile.Name,
                    Type = tile.Type,
                    X = tile.X,
                    Y = tile.Y,
                    Tracks = tile.Tracks.Select(track => new LayoutTrack
                    {
                        Name = track.Name,
                        Kind = track.Kind,
                        LeftLabel = track.LeftLabel,
                        RightLabel = track.RightLabel,
                        IsLeftBlocked = track.IsLeftBlocked,
                        IsRightBlocked = track.IsRightBlocked
                    }).ToList()
                });
            }

            return preset;
        }

        private void ApplyLayoutPreset(LayoutPreset preset)
        {
            var removableTiles = _tiles.Where(tile => tile.Type != TileType.ArretLigne).ToList();
            foreach (var tile in removableTiles)
            {
                foreach (var track in tile.Tracks)
                {
                    foreach (var loco in track.Locomotives.ToList())
                    {
                        track.Locomotives.Remove(loco);
                        loco.AssignedTrackId = null;
                    }
                }
                _tiles.Remove(tile);
            }

            foreach (var layoutTile in preset.Tiles.Where(t => t.Type != TileType.ArretLigne))
            {
                var tile = new TileModel
                {
                    Name = layoutTile.Name,
                    Type = layoutTile.Type,
                    X = layoutTile.X,
                    Y = layoutTile.Y
                };
                foreach (var layoutTrack in layoutTile.Tracks)
                {
                    tile.Tracks.Add(new TrackModel
                    {
                        Name = layoutTrack.Name,
                        Kind = layoutTrack.Kind,
                        LeftLabel = layoutTrack.LeftLabel,
                        RightLabel = layoutTrack.RightLabel,
                        IsLeftBlocked = layoutTrack.IsLeftBlocked,
                        IsRightBlocked = layoutTrack.IsRightBlocked
                    });
                }
                tile.RefreshTrackCollections();
                _tiles.Add(tile);
            }
        }

        private static JsonSerializerOptions GetPresetSerializerOptions()
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            options.Converters.Add(new JsonStringEnumConverter());
            return options;
        }

        private sealed class LayoutPreset
        {
            public string Name { get; set; } = string.Empty;
            public List<LayoutTile> Tiles { get; set; } = new();
        }

        private sealed class LayoutTile
        {
            public string Name { get; set; } = string.Empty;
            public TileType Type { get; set; }
            public double X { get; set; }
            public double Y { get; set; }
            public List<LayoutTrack> Tracks { get; set; } = new();
        }

        private sealed class LayoutTrack
        {
            public string Name { get; set; } = string.Empty;
            public TrackKind Kind { get; set; }
            public string? LeftLabel { get; set; }
            public string? RightLabel { get; set; }
            public bool IsLeftBlocked { get; set; }
            public bool IsRightBlocked { get; set; }
        }
    }
}
