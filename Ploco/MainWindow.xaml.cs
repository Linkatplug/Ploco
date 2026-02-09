using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using Ploco.Data;
using Ploco.Dialogs;
using Ploco.Helpers;
using Ploco.Models;

namespace Ploco
{
    public partial class MainWindow : Window
    {
        public static readonly RoutedUICommand LocomotiveHsCommand = new("Loc HS", "LocomotiveHsCommand", typeof(MainWindow));
        private readonly PlocoRepository _repository;
        private readonly ObservableCollection<LocomotiveModel> _locomotives = new();
        private readonly ObservableCollection<TileModel> _tiles = new();
        private readonly List<LayoutPreset> _layoutPresets = new();
        private const string LayoutPresetFileName = "layout_presets.json";
        private const double MinTileWidth = 260;
        private const double MinTileHeight = 180;
        private const double CanvasPadding = 80;
        private const int RollingLineStartNumber = 1101;
        private const int DefaultRollingLineCount = 23;
        private bool _isDarkMode;
        private Point _dragStartPoint;
        private TileModel? _draggedTile;
        private Point _tileDragStart;
        private bool _isResizingTile;
        private readonly Dictionary<Type, Window> _modelessWindows = new();
        public MainWindow()
        {
            InitializeComponent();
            _repository = new PlocoRepository("ploco.db");
            InputBindings.Add(new KeyBinding(LocomotiveHsCommand, new KeyGesture(Key.H, ModifierKeys.Control)));
            CommandBindings.Add(new CommandBinding(LocomotiveHsCommand, LocomotiveHsCommand_Executed, LocomotiveHsCommand_CanExecute));
            
            // Initialize logging system
            Logger.Initialize();
            Logger.Info("Application starting", "Application");
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Logger.Info("Main window loaded", "Application");
            
            // Restore window settings
            WindowSettingsHelper.RestoreWindowSettings(this, "MainWindow");
            
            _repository.Initialize();
            _repository.SeedDefaultDataIfNeeded();

            LoadState();
            LoadLayoutPresets();
            RefreshPresetMenu();
            ApplyTheme(false);
            LocomotiveList.ItemsSource = _locomotives;
            TileCanvas.ItemsSource = _tiles;
            InitializeLocomotiveView();
            UpdateTileCanvasExtent();
            
            Logger.Info($"Loaded {_locomotives.Count} locomotives and {_tiles.Count} tiles", "Application");
        }

        private void TileScrollViewer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateTileCanvasExtent();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var result = MessageBox.Show("Êtes-vous sûr de vouloir quitter ?", "Quitter", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
            {
                e.Cancel = true;
                Logger.Info("Application close cancelled by user", "Application");
                return;
            }

            Logger.Info("Saving state before closing", "Application");
            PersistState();
            
            // Save window settings
            WindowSettingsHelper.SaveWindowSettings(this, "MainWindow");
            
            Logger.Shutdown();
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
                foreach (var track in tile.Tracks)
                {
                    EnsureTrackOffsets(track);
                }
                _tiles.Add(tile);
            }

            UpdatePoolVisibility();
            UpdateTileCanvasExtent();
        }

        private void PersistState()
        {
            // Filter out ghost locomotives from tracks before saving
            var locomotivesToSave = _locomotives.Where(l => !l.IsForecastGhost).ToList();
            
            // Create a clean copy of tiles without ghosts
            var tilesToSave = new List<TileModel>();
            foreach (var tile in _tiles)
            {
                var tileCopy = new TileModel
                {
                    Id = tile.Id,
                    Type = tile.Type,
                    Name = tile.Name,
                    X = tile.X,
                    Y = tile.Y,
                    Width = tile.Width,
                    Height = tile.Height,
                    LocationPreset = tile.LocationPreset,
                    GarageTrackNumber = tile.GarageTrackNumber,
                    RollingLineCount = tile.RollingLineCount
                };

                foreach (var track in tile.Tracks)
                {
                    var trackCopy = new TrackModel
                    {
                        Id = track.Id,
                        TileId = track.TileId,
                        Position = track.Position,
                        Kind = track.Kind,
                        Name = track.Name,
                        IsOnTrain = track.IsOnTrain,
                        StopTime = track.StopTime,
                        IssueReason = track.IssueReason,
                        IsLocomotiveHs = track.IsLocomotiveHs,
                        LeftLabel = track.LeftLabel,
                        RightLabel = track.RightLabel,
                        IsLeftBlocked = track.IsLeftBlocked,
                        IsRightBlocked = track.IsRightBlocked,
                        TrainNumber = track.TrainNumber
                    };

                    // Only add non-ghost locomotives to the track
                    foreach (var loco in track.Locomotives.Where(l => !l.IsForecastGhost))
                    {
                        trackCopy.Locomotives.Add(loco);
                    }

                    tileCopy.Tracks.Add(trackCopy);
                }

                tileCopy.RefreshTrackCollections();
                tilesToSave.Add(tileCopy);
            }

            var state = new AppState
            {
                Series = BuildSeriesState(),
                Locomotives = locomotivesToSave,
                Tiles = tilesToSave
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

                if (dialog.SelectedType == TileType.RollingLine)
                {
                    AddRollingLineTile(dialog.SelectedName);
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
            UpdateTileCanvasExtent();
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
            UpdateTileCanvasExtent();
        }

        private void AddRollingLineTile(string name)
        {
            var tile = new TileModel
            {
                Name = name,
                Type = TileType.RollingLine,
                X = 20 + _tiles.Count * 30,
                Y = 20 + _tiles.Count * 30
            };
            var numbers = PromptRollingLineNumbers(DefaultRollingLineCount);
            if (numbers == null)
            {
                return;
            }

            tile.RollingLineCount = numbers.Count;
            NormalizeRollingLineTracks(tile, numbers);
            _tiles.Add(tile);
            _repository.AddHistory("TileCreated", $"Création du lieu {tile.DisplayTitle}.");
            _repository.AddHistory("RollingLineAdded", $"Lignes {FormatRollingLineRange(numbers)} ajoutées dans {tile.DisplayTitle}.");
            PersistState();
            UpdateTileCanvasExtent();
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
            UpdateTileCanvasExtent();
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

        private void AddRollingLineTrack_Click(object sender, RoutedEventArgs e)
        {
            var tile = GetTileFromSender(sender);
            if (tile == null || tile.Type != TileType.RollingLine)
            {
                return;
            }

            var currentNumbers = ResolveRollingLineNumbers(tile);
            var defaultCount = currentNumbers.Count;
            var numbers = PromptRollingLineNumbers(defaultCount);
            if (numbers == null)
            {
                return;
            }

            var adjustedNumbers = EnsureRollingLineNumbersWithinAssignments(tile, numbers);
            if (adjustedNumbers.Count != numbers.Count)
            {
                MessageBox.Show($"La configuration a été ajustée pour conserver les locomotives déjà affectées.",
                    "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            tile.RollingLineCount = adjustedNumbers.Count;
            NormalizeRollingLineTracks(tile, adjustedNumbers);
            _repository.AddHistory("RollingLineAdded", $"Configuration des lignes ({FormatRollingLineRange(adjustedNumbers)}) dans {tile.DisplayTitle}.");
            PersistState();
            UpdateTileCanvasExtent();
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
                // Prevent dragging ghost locomotives (safety check - they shouldn't be in the list)
                if (loco.IsForecastGhost)
                {
                    return;
                }
                
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
                loco.AssignedTrackOffsetX = null;
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
                // Prevent dragging ghost locomotives
                if (loco.IsForecastGhost)
                {
                    return;
                }
                
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
                if (track.Kind == TrackKind.RollingLine && track.Locomotives.Any() && !track.Locomotives.Contains(loco))
                {
                    MessageBox.Show("Une seule locomotive est autorisée par ligne de roulement.", "Action impossible",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                var insertIndex = GetInsertIndex(listBox, e.GetPosition(listBox));
                MoveLocomotiveToTrack(loco, track, insertIndex);
                UpdateLocomotiveDropOffset(loco, track, listBox, e.GetPosition(listBox));
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
                if (tile.Type == TileType.RollingLine)
                {
                    return;
                }
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
            Logger.Debug($"Moving locomotive Id={loco.Id} Number={loco.Number} to track {targetTrack.Name} at index {insertIndex}", "Movement");
            
            // For rolling lines, check if occupied by REAL locomotives (ignore ghosts)
            if (targetTrack.Kind == TrackKind.RollingLine)
            {
                var realLocosInTarget = targetTrack.Locomotives
                    .Where(l => !l.IsForecastGhost)
                    .ToList();
                
                if (realLocosInTarget.Any() && !targetTrack.Locomotives.Contains(loco))
                {
                    Logger.Warning($"Cannot move loco Id={loco.Id} Number={loco.Number} to occupied rolling line {targetTrack.Name} (occupied by {realLocosInTarget.Count} real loco(s))", "Movement");
                    MessageBox.Show("Une seule locomotive est autorisée par ligne de roulement.", "Action impossible",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
            }

            var currentTrack = _tiles.SelectMany(t => t.Tracks).FirstOrDefault(t => t.Locomotives.Contains(loco));
            if (currentTrack != null)
            {
                var currentIndex = currentTrack.Locomotives.IndexOf(loco);
                currentTrack.Locomotives.Remove(loco);
                Logger.Info($"Removed loco Id={loco.Id} Number={loco.Number} from {currentTrack.Name} (index {currentIndex})", "Movement");
                
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
                    Logger.Info($"Added loco Id={loco.Id} Number={loco.Number} to {targetTrack.Name} (end)", "Movement");
                }
                else
                {
                    targetTrack.Locomotives.Insert(insertIndex, loco);
                    Logger.Info($"Inserted loco Id={loco.Id} Number={loco.Number} to {targetTrack.Name} at index {insertIndex}", "Movement");
                }
            }

            loco.AssignedTrackId = targetTrack.Id;
            loco.AssignedTrackOffsetX = null;
            EnsureTrackOffsets(targetTrack);
            _repository.AddHistory("LocomotiveMoved", $"Loco {loco.Number} déplacée vers {targetTrack.Name}.");
            UpdatePoolVisibility();
            RefreshTapisT13();
            
            Logger.Info($"Successfully moved loco Id={loco.Id} Number={loco.Number} to {targetTrack.Name}", "Movement");
        }

        private void SwapLocomotivesBetweenTracks(LocomotiveModel loco1, TrackModel track1, LocomotiveModel loco2, TrackModel track2)
        {
            // Retirer les deux locomotives de leurs tracks actuels
            track1.Locomotives.Remove(loco1);
            track2.Locomotives.Remove(loco2);
            
            // Les échanger
            track1.Locomotives.Add(loco2);
            track2.Locomotives.Add(loco1);
            
            // Mettre à jour les assignations
            loco1.AssignedTrackId = track2.Id;
            loco1.AssignedTrackOffsetX = null;
            loco2.AssignedTrackId = track1.Id;
            loco2.AssignedTrackOffsetX = null;
            
            EnsureTrackOffsets(track1);
            EnsureTrackOffsets(track2);
            UpdatePoolVisibility();
            RefreshTapisT13();
        }

        private void UpdateLocomotiveDropOffset(LocomotiveModel loco, TrackModel track, ListBox listBox, Point dropPosition)
        {
            if (track.Kind != TrackKind.Line && track.Kind != TrackKind.Zone && track.Kind != TrackKind.Output)
            {
                loco.AssignedTrackOffsetX = null;
                return;
            }

            const double slotWidth = 44;
            var maxX = Math.Max(0, listBox.ActualWidth - slotWidth);
            var clampedX = Math.Max(0, Math.Min(dropPosition.X, maxX));
            var desiredSlot = (int)Math.Round(clampedX / slotWidth);
            var occupiedSlots = track.Locomotives
                .Where(item => !ReferenceEquals(item, loco))
                .Select(item => item.AssignedTrackOffsetX.HasValue ? (int)Math.Round(item.AssignedTrackOffsetX.Value / slotWidth) : -1)
                .Where(slot => slot >= 0)
                .ToHashSet();

            var slot = desiredSlot;
            while (occupiedSlots.Contains(slot))
            {
                slot++;
            }

            loco.AssignedTrackOffsetX = slot * slotWidth;
            EnsureTrackOffsets(track);
        }

        private static void EnsureTrackOffsets(TrackModel track)
        {
            if (track.Kind != TrackKind.Line && track.Kind != TrackKind.Zone && track.Kind != TrackKind.Output)
            {
                foreach (var loco in track.Locomotives)
                {
                    loco.AssignedTrackOffsetX = null;
                }
                return;
            }

            const double slotWidth = 44;
            var occupiedSlots = new HashSet<int>();
            foreach (var loco in track.Locomotives)
            {
                if (loco.AssignedTrackOffsetX.HasValue)
                {
                    var slot = (int)Math.Round(loco.AssignedTrackOffsetX.Value / slotWidth);
                    if (!occupiedSlots.Contains(slot))
                    {
                        occupiedSlots.Add(slot);
                        loco.AssignedTrackOffsetX = slot * slotWidth;
                        continue;
                    }
                }

                var fallbackSlot = 0;
                while (occupiedSlots.Contains(fallbackSlot))
                {
                    fallbackSlot++;
                }

                occupiedSlots.Add(fallbackSlot);
                loco.AssignedTrackOffsetX = fallbackSlot * slotWidth;
            }
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
            var loco = GetLocomotiveFromMenuItem(sender);
            if (loco == null)
            {
                return;
            }

            var oldStatus = loco.Status;
            Logger.Debug($"Opening status dialog for loco {loco.Number} (current status: {oldStatus})", "Status");
            
            var dialog = new StatusDialog(loco) { Owner = this };
            if (dialog.ShowDialog() == true)
            {
                Logger.Info($"Status changed for loco {loco.Number}: {oldStatus} -> {loco.Status}", "Status");
                _repository.AddHistory("StatusChanged", $"Statut modifié pour {loco.Number}.");
                PersistState();
                RefreshTapisT13();
            }
            else
            {
                Logger.Debug($"Status change cancelled for loco {loco.Number}", "Status");
            }
        }

        private void MenuItem_SwapPool_Click(object sender, RoutedEventArgs e)
        {
            var loco = GetLocomotiveFromMenuItem(sender);
            if (loco != null)
            {
                OpenSwapDialog(loco);
            }
        }

        private void MenuItem_LocHs_Click(object sender, RoutedEventArgs e)
        {
            var loco = GetLocomotiveFromMenuItem(sender);
            if (loco != null)
            {
                MarkLocomotiveHs(loco);
            }
        }

        private void MenuItem_RemoveFromTile_Click(object sender, RoutedEventArgs e)
        {
            var loco = GetLocomotiveFromMenuItem(sender);
            if (loco == null)
            {
                return;
            }

            var track = _tiles.SelectMany(t => t.Tracks).FirstOrDefault(t => t.Locomotives.Contains(loco));
            if (track != null)
            {
                track.Locomotives.Remove(loco);
                loco.AssignedTrackId = null;
                loco.AssignedTrackOffsetX = null;
                _repository.AddHistory("LocomotiveRemoved", $"Loco {loco.Number} retirée de {track.Name}.");
                UpdatePoolVisibility();
                PersistState();
                RefreshTapisT13();
            }
        }

        private void LocomotiveTileContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            if (sender is not ContextMenu contextMenu)
            {
                return;
            }

            // Get the locomotive from the context menu's placement target
            LocomotiveModel? loco = null;
            if (contextMenu.PlacementTarget is FrameworkElement element && element.DataContext is LocomotiveModel l)
            {
                loco = l;
            }
            else if (contextMenu.DataContext is LocomotiveModel l2)
            {
                loco = l2;
            }

            if (loco == null)
            {
                return;
            }

            // Log context menu opened on this locomotive
            Logger.Debug($"Opened context menu on loco Id={loco.Id} Number={loco.Number} IsForecastOrigin={loco.IsForecastOrigin} IsForecastGhost={loco.IsForecastGhost}", "ContextMenu");

            // Find menu items by name - we need to search through the Items collection
            MenuItem? placementItem = null;
            MenuItem? annulerItem = null;
            MenuItem? validerItem = null;

            foreach (var item in contextMenu.Items)
            {
                if (item is MenuItem menuItem)
                {
                    if (menuItem.Header?.ToString() == "Placement prévisionnel")
                        placementItem = menuItem;
                    else if (menuItem.Header?.ToString() == "Annuler le placement prévisionnel")
                        annulerItem = menuItem;
                    else if (menuItem.Header?.ToString() == "Valider le placement prévisionnel")
                        validerItem = menuItem;
                }
            }

            // Show/hide based on forecast state
            if (placementItem != null)
            {
                placementItem.Visibility = loco.IsForecastOrigin ? Visibility.Collapsed : Visibility.Visible;
            }

            if (annulerItem != null)
            {
                annulerItem.Visibility = loco.IsForecastOrigin ? Visibility.Visible : Visibility.Collapsed;
            }

            if (validerItem != null)
            {
                validerItem.Visibility = loco.IsForecastOrigin ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Removes all forecast ghosts linked to the specified locomotive from ALL tracks.
        /// Uses robust matching to handle cases where WPF provides different instances.
        /// Strategy: Search by Number first (most reliable), then verify it's a ghost.
        /// </summary>
        private int RemoveForecastGhostsFor(LocomotiveModel loco)
        {
            int ghostsRemoved = 0;
            var tracksWithGhosts = new List<string>();

            // Log what we're searching for
            Logger.Debug($"RemoveForecastGhostsFor: searching for ghosts of loco Id={loco.Id} Number={loco.Number} SeriesId={loco.SeriesId} ForecastTargetRollingLineTrackId={loco.ForecastTargetRollingLineTrackId}", "Forecast");

            // Try to find the real source locomotive if the passed instance doesn't have forecast info
            int? targetTrackId = loco.ForecastTargetRollingLineTrackId;
            
            if (targetTrackId == null && loco.IsForecastOrigin)
            {
                // This is the origin but ForecastTargetRollingLineTrackId is null, shouldn't happen
                Logger.Warning($"IsForecastOrigin=true but ForecastTargetRollingLineTrackId=null for loco Id={loco.Id} Number={loco.Number}", "Forecast");
            }
            
            // If we don't have target track info, try to find it from any origin loco with same number
            if (targetTrackId == null)
            {
                var originLocoWithSameNumber = _locomotives.FirstOrDefault(l => 
                    l.IsForecastOrigin && 
                    l.Number == loco.Number && 
                    l.ForecastTargetRollingLineTrackId != null);
                
                if (originLocoWithSameNumber != null)
                {
                    targetTrackId = originLocoWithSameNumber.ForecastTargetRollingLineTrackId;
                    Logger.Debug($"Found origin loco with same number: Id={originLocoWithSameNumber.Id} Number={originLocoWithSameNumber.Number}, using its ForecastTargetRollingLineTrackId={targetTrackId}", "Forecast");
                }
            }

            // Get all tracks, prioritizing target track if known
            var allTracks = _tiles.SelectMany(t => t.Tracks).ToList();
            IEnumerable<TrackModel> orderedTracks;
            
            if (targetTrackId != null)
            {
                var targetTracks = allTracks.Where(t => t.Id == targetTrackId);
                var otherTracks = allTracks.Where(t => t.Id != targetTrackId);
                orderedTracks = targetTracks.Concat(otherTracks);
                Logger.Debug($"Prioritizing target track Id={targetTrackId}", "Forecast");
            }
            else
            {
                orderedTracks = allTracks;
                Logger.Debug($"No target track known, searching all tracks", "Forecast");
            }

            // Scan tracks for ghosts - use multiple matching strategies
            foreach (var track in orderedTracks)
            {
                // Strategy: Find ghosts by Number (most reliable) and verify they're ghosts
                var ghostsToRemove = track.Locomotives
                    .Where(l => l.IsForecastGhost && l.Number == loco.Number)
                    .ToList();

                if (ghostsToRemove.Any())
                {
                    Logger.Debug($"Found {ghostsToRemove.Count} ghost(s) in track {track.Name} with Number={loco.Number}", "Forecast");
                }

                foreach (var ghost in ghostsToRemove)
                {
                    // Verify this is really a match
                    bool isMatch = false;
                    string matchReason = "";
                    
                    // Check 1: Exact Id match
                    if (ghost.ForecastSourceLocomotiveId == loco.Id)
                    {
                        isMatch = true;
                        matchReason = "SourceIdMatch";
                    }
                    // Check 2: Number match (already verified above, so this is our fallback)
                    else if (ghost.Number == loco.Number)
                    {
                        isMatch = true;
                        matchReason = "NumberFallback";
                    }
                    
                    if (isMatch)
                    {
                        track.Locomotives.Remove(ghost);
                        ghostsRemoved++;
                        tracksWithGhosts.Add(track.Name);
                        
                        Logger.Debug($"Removed ghost (Id={ghost.Id}, Number={ghost.Number}, ForecastSourceLocomotiveId={ghost.ForecastSourceLocomotiveId}) for loco (Id={loco.Id}, Number={loco.Number}) from track {track.Name}, reason={matchReason}", "Forecast");
                    }
                }
            }

            if (ghostsRemoved > 0)
            {
                Logger.Info($"Removed {ghostsRemoved} ghost(s) for loco Id={loco.Id} Number={loco.Number} from tracks: {string.Join(", ", tracksWithGhosts)}", "Forecast");
            }
            else
            {
                // Log all ghosts we can see to help debug
                var allGhosts = allTracks.SelectMany(t => t.Locomotives.Where(l => l.IsForecastGhost))
                    .Select(g => $"Ghost Id={g.Id} Number={g.Number} SeriesId={g.SeriesId} ForecastSourceLocomotiveId={g.ForecastSourceLocomotiveId}")
                    .ToList();
                
                Logger.Warning($"No ghosts found for loco Id={loco.Id} Number={loco.Number} SeriesId={loco.SeriesId}. All ghosts in system: [{string.Join("; ", allGhosts)}]", "Forecast");
            }

            return ghostsRemoved;
        }

        private void MenuItem_PlacementPrevisionnel_Click(object sender, RoutedEventArgs e)
        {
            var loco = GetLocomotiveFromMenuItem(sender);
            if (loco == null)
            {
                return;
            }

            Logger.Debug($"Forecast placement requested: loco Id={loco.Id} Number={loco.Number}", "Forecast");

            // Check if already in forecast mode
            if (loco.IsForecastOrigin)
            {
                Logger.Warning($"Loco Id={loco.Id} Number={loco.Number} already in forecast mode", "Forecast");
                MessageBox.Show("Cette locomotive est déjà en mode prévisionnel.", "Placement prévisionnel",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Check if locomotive is assigned to a track
            if (loco.AssignedTrackId == null)
            {
                Logger.Warning($"Loco Id={loco.Id} Number={loco.Number} not assigned to any track, cannot forecast", "Forecast");
                MessageBox.Show("La locomotive doit être placée dans une tuile pour activer le mode prévisionnel.", 
                    "Placement prévisionnel", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Show rolling line selection dialog
            var dialog = new RollingLineSelectionDialog(_tiles) { Owner = this };
            if (dialog.ShowDialog() != true || dialog.SelectedTrack == null)
            {
                Logger.Debug($"Forecast placement cancelled by user for loco Id={loco.Id} Number={loco.Number}", "Forecast");
                return;
            }

            var targetTrack = dialog.SelectedTrack;
            Logger.Info($"Forecast placement: loco Id={loco.Id} Number={loco.Number} -> track {targetTrack.Name}", "Forecast");

            // Set the original locomotive to forecast origin (will turn blue via DataTrigger)
            loco.IsForecastOrigin = true;
            loco.ForecastTargetRollingLineTrackId = targetTrack.Id;

            // Create a ghost locomotive for the rolling line
            // Ghost shows REAL status color (red/orange/yellow/green) via StatutToBrushConverter
            var ghost = new LocomotiveModel
            {
                Id = -100000 - loco.Id, // Unique negative ID
                SeriesId = loco.SeriesId,
                SeriesName = loco.SeriesName,
                Number = loco.Number,
                Status = loco.Status, // Ghost reflects real status
                Pool = loco.Pool,
                TractionPercent = loco.TractionPercent,
                HsReason = loco.HsReason,
                MaintenanceDate = loco.MaintenanceDate,
                IsForecastGhost = true,
                ForecastSourceLocomotiveId = loco.Id, // Link back to original using Id
                AssignedTrackId = targetTrack.Id
            };

            Logger.Debug($"Created ghost Id={ghost.Id} Number={ghost.Number} for source loco Id={loco.Id} Number={loco.Number}, Status={loco.Status}", "Forecast");

            // Add ghost ONLY to the target track, NOT to _locomotives
            targetTrack.Locomotives.Add(ghost);

            _repository.AddHistory("ForecastPlacement", 
                $"Placement prévisionnel de la loco {loco.Number} vers {targetTrack.Name}.");
            PersistState();
            RefreshTapisT13();
        }

        private void MenuItem_AnnulerPrevisionnel_Click(object sender, RoutedEventArgs e)
        {
            var loco = GetLocomotiveFromMenuItem(sender);
            if (loco == null || !loco.IsForecastOrigin)
            {
                return;
            }

            Logger.Info($"Cancelling forecast placement for loco Id={loco.Id} Number={loco.Number}", "Forecast");

            // Remove all forecast ghosts for this locomotive
            int ghostsRemoved = RemoveForecastGhostsFor(loco);

            // Restore original locomotive state (DO NOT touch Status)
            loco.IsForecastOrigin = false;
            loco.ForecastTargetRollingLineTrackId = null;

            _repository.AddHistory("ForecastCancelled", 
                $"Annulation du placement prévisionnel de la loco {loco.Number}.");
            PersistState();
            RefreshTapisT13();
            
            if (ghostsRemoved > 0)
            {
                Logger.Info($"Forecast cancelled successfully for loco Id={loco.Id} Number={loco.Number}", "Forecast");
            }
        }

        private void MenuItem_ValiderPrevisionnel_Click(object sender, RoutedEventArgs e)
        {
            var loco = GetLocomotiveFromMenuItem(sender);
            if (loco == null || !loco.IsForecastOrigin)
            {
                return;
            }

            Logger.Info($"Validating forecast placement for loco Id={loco.Id} Number={loco.Number}", "Forecast");

            // Save target track ID BEFORE resetting flags
            var targetTrackId = loco.ForecastTargetRollingLineTrackId;
            if (targetTrackId == null)
            {
                Logger.Error($"ForecastTargetRollingLineTrackId is null for loco Id={loco.Id} Number={loco.Number}", null, "Forecast");
                MessageBox.Show("Erreur: ligne de roulement cible non définie.", "Erreur",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Find the target track
            var targetTrack = _tiles.SelectMany(t => t.Tracks)
                .FirstOrDefault(t => t.Id == targetTrackId);

            if (targetTrack == null)
            {
                Logger.Error($"Target track Id={targetTrackId} not found for loco Id={loco.Id} Number={loco.Number}", null, "Forecast");
                MessageBox.Show("La ligne de roulement cible n'a pas été trouvée.", "Erreur",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // CRITICAL: Remove ghost FIRST, before checking for real locomotives
            Logger.Debug($"Removing ghosts before validation for loco Id={loco.Id} Number={loco.Number}", "Forecast");
            int ghostsRemoved = RemoveForecastGhostsFor(loco);
            
            // Now check if target line has real locomotives (after ghost removal)
            var realLocosInTarget = targetTrack.Locomotives.Where(l => !l.IsForecastGhost).ToList();
            if (realLocosInTarget.Any())
            {
                Logger.Warning($"Target track {targetTrack.Name} occupied by {realLocosInTarget.Count} real loco(s)", "Forecast");
                var result = MessageBox.Show(
                    $"La ligne {targetTrack.Name} est maintenant occupée par la locomotive {realLocosInTarget.First().Number}.\n" +
                    "Voulez-vous quand même valider le placement prévisionnel ? Cela remplacera la locomotive existante.",
                    "Ligne occupée",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);
            
                if (result != MessageBoxResult.Yes)
                {
                    Logger.Info($"User declined to replace existing loco on {targetTrack.Name}", "Forecast");
                    // Re-create ghost since validation was cancelled
                    var ghost = new LocomotiveModel
                    {
                        Id = -100000 - loco.Id,
                        SeriesId = loco.SeriesId,
                        SeriesName = loco.SeriesName,
                        Number = loco.Number,
                        Status = loco.Status,
                        Pool = loco.Pool,
                        TractionPercent = loco.TractionPercent,
                        HsReason = loco.HsReason,
                        MaintenanceDate = loco.MaintenanceDate,
                        IsForecastGhost = true,
                        ForecastSourceLocomotiveId = loco.Id,
                        AssignedTrackId = targetTrack.Id
                    };
                    targetTrack.Locomotives.Add(ghost);
                    return;
                }
                
                Logger.Warning($"User confirmed: replacing existing loco on {targetTrack.Name}", "Forecast");
                
                // Remove existing real locomotives (user confirmed)
                foreach (var realLoco in realLocosInTarget.ToList())
                {
                    targetTrack.Locomotives.Remove(realLoco);
                    realLoco.AssignedTrackId = null;
                    realLoco.AssignedTrackOffsetX = null;
                    Logger.Info($"Removed loco Id={realLoco.Id} Number={realLoco.Number} from {targetTrack.Name} (replaced by forecast)", "Forecast");
                }
            }

            // Reset forecast flags BEFORE moving
            loco.IsForecastOrigin = false;
            loco.ForecastTargetRollingLineTrackId = null;

            Logger.Info($"Moving loco Id={loco.Id} Number={loco.Number} to {targetTrack.Name}", "Forecast");
            
            // Use existing MoveLocomotiveToTrack method to properly move the locomotive
            MoveLocomotiveToTrack(loco, targetTrack, 0);

            // CRITICAL: Check if move was successful
            if (loco.AssignedTrackId == targetTrack.Id)
            {
                // Move succeeded
                _repository.AddHistory("ForecastValidated", 
                    $"Validation du placement prévisionnel de la loco {loco.Number} vers {targetTrack.Name}.");
                PersistState();
                RefreshTapisT13();
                
                Logger.Info($"Forecast validated successfully: loco Id={loco.Id} Number={loco.Number} moved to {targetTrack.Name}", "Forecast");
            }
            else
            {
                // Move failed
                Logger.Warning($"Forecast validation FAILED: loco Id={loco.Id} Number={loco.Number} NOT moved to {targetTrack.Name} (AssignedTrackId={loco.AssignedTrackId})", "Forecast");
                MessageBox.Show($"Le déplacement a échoué. La locomotive n'a pas pu être placée sur {targetTrack.Name}.", 
                    "Erreur de validation", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenSwapDialog(LocomotiveModel loco)
        {
            if (!string.Equals(loco.Pool, "Sibelit", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("Le swap est réservé aux locomotives de la pool Sibelit.", "Swap", MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var lineasCandidates = _locomotives
                .Where(item => string.Equals(item.Pool, "Lineas", StringComparison.OrdinalIgnoreCase)
                               && item.AssignedTrackId == null)
                .OrderBy(item => item.Number)
                .ToList();
            if (!lineasCandidates.Any())
            {
                MessageBox.Show("Aucune locomotive Lineas disponible pour le swap.", "Swap", MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var dialog = new SwapDialog(loco, new ObservableCollection<LocomotiveModel>(lineasCandidates))
            {
                Owner = this
            };
            if (dialog.ShowDialog() == true && dialog.SelectedLoco != null)
            {
                ApplySwap(loco, dialog.SelectedLoco);
            }
        }

        private void ApplySwap(LocomotiveModel sibelitLoco, LocomotiveModel lineasLoco)
        {
            var track = _tiles.SelectMany(t => t.Tracks).FirstOrDefault(t => t.Locomotives.Contains(sibelitLoco));
            var trackIndex = track?.Locomotives.IndexOf(sibelitLoco) ?? -1;
            var trackOffset = sibelitLoco.AssignedTrackOffsetX;

            if (track != null)
            {
                track.Locomotives.Remove(sibelitLoco);
            }

            sibelitLoco.AssignedTrackId = null;
            sibelitLoco.AssignedTrackOffsetX = null;
            sibelitLoco.Pool = "Lineas";

            lineasLoco.Pool = "Sibelit";
            if (track != null)
            {
                if (trackIndex >= 0 && trackIndex <= track.Locomotives.Count)
                {
                    track.Locomotives.Insert(trackIndex, lineasLoco);
                }
                else
                {
                    track.Locomotives.Add(lineasLoco);
                }
                lineasLoco.AssignedTrackId = track.Id;
                lineasLoco.AssignedTrackOffsetX = trackOffset;
                EnsureTrackOffsets(track);
            }
            else
            {
                lineasLoco.AssignedTrackId = null;
                lineasLoco.AssignedTrackOffsetX = null;
            }

            _repository.AddHistory("LocomotiveSwapped",
                $"Swap Sibelit {sibelitLoco.Number} ↔ Lineas {lineasLoco.Number}.");
            UpdatePoolVisibility();
            PersistState();
            RefreshTapisT13();
        }

        private static LocomotiveModel? GetLocomotiveFromMenuItem(object sender)
        {
            if (sender is not MenuItem menuItem)
            {
                return null;
            }

            // PRIORITY 1: Get from ContextMenu's DataContext or PlacementTarget
            // LocomotiveItem_PreviewMouseRightButtonDown sets these correctly
            var contextMenu = menuItem.Parent as ContextMenu
                ?? ItemsControl.ItemsControlFromItemContainer(menuItem) as ContextMenu;
            
            if (contextMenu != null)
            {
                // First try ContextMenu's DataContext (set by PreviewMouseRightButtonDown)
                if (contextMenu.DataContext is LocomotiveModel contextLoco)
                {
                    return contextLoco;
                }
                
                // Then try PlacementTarget's DataContext
                if (contextMenu.PlacementTarget is FrameworkElement element && element.DataContext is LocomotiveModel placementLoco)
                {
                    return placementLoco;
                }
            }

            // PRIORITY 2: CommandParameter (if explicitly set)
            if (menuItem.CommandParameter is LocomotiveModel parameter)
            {
                return parameter;
            }

            // PRIORITY 3: MenuItem's own DataContext (can be ambiguous, use as last resort)
            if (menuItem.DataContext is LocomotiveModel dataContext)
            {
                return dataContext;
            }

            return null;
        }

        private void MarkLocomotiveHs(LocomotiveModel loco)
        {
            var dialog = new StatusDialog(loco, LocomotiveStatus.HS) { Owner = this };
            if (dialog.ShowDialog() == true)
            {
                _repository.AddHistory("StatusChanged", $"Statut modifié pour {loco.Number} (HS).");
                PersistState();
                RefreshTapisT13();
            }
        }

        private void LocomotiveHsCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var loco = GetFocusedLocomotive();
            if (loco != null)
            {
                MarkLocomotiveHs(loco);
            }
        }

        private void LocomotiveHsCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = GetFocusedLocomotive() != null;
        }

        private LocomotiveModel? GetFocusedLocomotive()
        {
            if (Keyboard.FocusedElement is DependencyObject element)
            {
                var loco = GetLocomotiveFromElement(element);
                if (loco != null)
                {
                    return loco;
                }
            }

            return LocomotiveList.SelectedItem as LocomotiveModel;
        }

        private static LocomotiveModel? GetLocomotiveFromElement(DependencyObject? element)
        {
            while (element != null)
            {
                if (element is FrameworkElement frameworkElement && frameworkElement.DataContext is LocomotiveModel loco)
                {
                    return loco;
                }

                element = VisualTreeHelper.GetParent(element);
            }

            return null;
        }

        private void Tile_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is DependencyObject source
                && (FindAncestor<Button>(source) != null
                    || FindAncestor<MenuItem>(source) != null
                    || FindAncestor<Menu>(source) != null
                    || FindAncestor<Thumb>(source) != null))
            {
                return;
            }

            if (_isResizingTile)
            {
                return;
            }

            if (sender is Border border && border.DataContext is TileModel tile)
            {
                _draggedTile = tile;
                _tileDragStart = GetDropPositionInWorkspace(e);
                border.CaptureMouse();
            }
        }

        private void Tile_MouseMove(object sender, MouseEventArgs e)
        {
            if (_draggedTile == null || e.LeftButton != MouseButtonState.Pressed || _isResizingTile)
            {
                return;
            }

            var currentPosition = GetDropPositionInWorkspace(e);
            var offset = currentPosition - _tileDragStart;
            _draggedTile.X = Math.Max(0, _draggedTile.X + offset.X);
            _draggedTile.Y = Math.Max(0, _draggedTile.Y + offset.Y);
            _tileDragStart = currentPosition;
            UpdateTileCanvasExtent();
        }

        private void Tile_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_draggedTile != null)
            {
                ResolveTileOverlap(_draggedTile);
                _repository.AddHistory("TileMoved", $"Tuile {_draggedTile.Name} déplacée.");
                PersistState();
                UpdateTileCanvasExtent();
            }
            if (sender is Border border)
            {
                border.ReleaseMouseCapture();
            }
            _draggedTile = null;
        }

        private void TileResizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            if (sender is Thumb thumb && thumb.DataContext is TileModel tile)
            {
                _isResizingTile = true;
                tile.Width = Math.Max(MinTileWidth, tile.Width + e.HorizontalChange);
                tile.Height = Math.Max(MinTileHeight, tile.Height + e.VerticalChange);
                UpdateTileCanvasExtent();
            }
        }

        private void TileResizeThumb_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            _isResizingTile = false;
            if (sender is Thumb thumb && thumb.DataContext is TileModel tile)
            {
                ResolveTileOverlap(tile);
                _repository.AddHistory("TileResized", $"Tuile {tile.Name} redimensionnée.");
                PersistState();
                UpdateTileCanvasExtent();
            }
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
            RefreshTapisT13();
        }

        private void MenuItem_History_Click(object sender, RoutedEventArgs e)
        {
            var history = _repository.LoadHistory();
            var dialog = new HistoriqueWindow(history) { Owner = this };
            dialog.ShowDialog();
        }

        private void MenuItem_TapisT13_Click(object sender, RoutedEventArgs e)
        {
            OpenModelessWindow(() => new TapisT13Window(_locomotives, _tiles));
        }

        private void MenuItem_PlanningPdf_Click(object sender, RoutedEventArgs e)
        {
            OpenModelessWindow(() => new PlanningPdfWindow(_repository));
        }

        private void MenuItem_DatabaseManagement_Click(object sender, RoutedEventArgs e)
        {
            OpenModelessWindow(() => new DatabaseManagementWindow(_repository, _locomotives, _tiles));
        }

        private void MenuItem_ResetLocomotives_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Réinitialiser toutes les locomotives ?", "Réinitialisation des locomotives", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            Logger.Warning("User initiated locomotive reset", "Reset");

            foreach (var track in _tiles.SelectMany(tile => tile.Tracks))
            {
                track.Locomotives.Clear();
            }

            foreach (var loco in _locomotives)
            {
                loco.AssignedTrackId = null;
                loco.AssignedTrackOffsetX = null;
                loco.Status = LocomotiveStatus.Ok;
                loco.TractionPercent = null;
                loco.HsReason = null;
                loco.Pool = "Lineas";
            }

            _repository.AddHistory("ResetLocomotives", "Réinitialisation des locomotives.");
            UpdatePoolVisibility();
            PersistState();
            RefreshTapisT13();
            
            Logger.Info($"All locomotives reset successfully ({_locomotives.Count} locomotives)", "Reset");
        }

        private void MenuItem_ResetTiles_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Supprimer toutes les tuiles ?", "Réinitialisation des tuiles", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            Logger.Warning("User initiated tile reset", "Reset");
            
            foreach (var tile in _tiles.ToList())
            {
                foreach (var track in tile.Tracks)
                {
                    foreach (var loco in track.Locomotives.ToList())
                    {
                        track.Locomotives.Remove(loco);
                        loco.AssignedTrackId = null;
                        loco.AssignedTrackOffsetX = null;
                    }
                }
            }

            _tiles.Clear();
            _repository.AddHistory("ResetTiles", "Suppression de toutes les tuiles.");
            UpdatePoolVisibility();
            PersistState();
            RefreshTapisT13();
            
            Logger.Info("All tiles reset successfully", "Reset");
        }

        private void MenuItem_Import_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Logger.Info("Opening Import window", "Menu");
                
                var importWindow = new ImportWindow(_locomotives, () =>
                {
                    // Callback when import is complete
                    // Refresh the UI to show updated pools
                    PersistState();
                });
                
                importWindow.Owner = this;
                importWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to open Import window", ex, "Menu");
                MessageBox.Show($"Impossible d'ouvrir la fenêtre d'import.\n\nErreur: {ex.Message}", 
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MenuItem_Logs_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var logsDirectory = Logger.LogsDirectory;
                Logger.Info($"Opening logs folder: {logsDirectory}", "Menu");
                
                // Open the logs folder in Windows Explorer
                System.Diagnostics.Process.Start("explorer.exe", logsDirectory);
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to open logs folder", ex, "Menu");
                MessageBox.Show($"Impossible d'ouvrir le dossier de logs.\n\nChemin: {Logger.LogsDirectory}\n\nErreur: {ex.Message}", 
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ToggleDarkMode_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem)
            {
                _isDarkMode = menuItem.IsChecked;
                ApplyTheme(_isDarkMode);
            }
        }

        private void OpenModelessWindow<TWindow>(Func<TWindow> factory) where TWindow : Window
        {
            var windowType = typeof(TWindow);
            if (_modelessWindows.TryGetValue(windowType, out var existing) && existing.IsVisible)
            {
                if (existing is IRefreshableWindow refreshable)
                {
                    refreshable.RefreshData();
                }

                existing.Activate();
                return;
            }

            var window = factory();
            window.Owner = this;
            window.Closed += (_, _) => _modelessWindows.Remove(windowType);
            _modelessWindows[windowType] = window;

            if (window is IRefreshableWindow newRefreshable)
            {
                newRefreshable.RefreshData();
            }

            window.Show();
            window.Activate();
        }

        private void RefreshTapisT13()
        {
            if (_modelessWindows.TryGetValue(typeof(TapisT13Window), out var window)
                && window is IRefreshableWindow refreshable
                && window.IsVisible)
            {
                refreshable.RefreshData();
            }
        }

        private void LocomotiveItem_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not FrameworkElement element)
            {
                return;
            }

            var contextMenu = element.ContextMenu;
            if (contextMenu == null)
            {
                return;
            }

            if (FindAncestor<ListBoxItem>(element) is ListBoxItem listBoxItem)
            {
                listBoxItem.IsSelected = true;
            }

            contextMenu.DataContext = element.DataContext;
            contextMenu.PlacementTarget = element;
            contextMenu.IsOpen = true;
            e.Handled = true;
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

            if (tile.Type == TileType.RollingLine)
            {
                return tile.RollingLineTracks.FirstOrDefault(t => !t.Locomotives.Any())
                       ?? tile.RollingLineTracks.FirstOrDefault();
            }

            return tile.MainTrack;
        }

        private void EnsureDefaultTracks(TileModel tile)
        {
            if (tile.Type == TileType.RollingLine)
            {
                var numbers = ResolveRollingLineNumbers(tile);
                NormalizeRollingLineTracks(tile, numbers);
                return;
            }

            if (!tile.Tracks.Any() && tile.Type != TileType.ArretLigne)
            {
                tile.Tracks.Add(CreateDefaultTrack(tile));
            }
            tile.RefreshTrackCollections();
        }

        private static List<int> ResolveRollingLineNumbers(TileModel tile)
        {
            var existingNumbers = tile.Tracks
                .Where(t => t.Kind == TrackKind.RollingLine)
                .Select(t => int.TryParse(t.Name, out var value) ? value : 0)
                .Where(value => value > 0)
                .OrderBy(n => n)
                .ToList();

            if (existingNumbers.Any())
            {
                return existingNumbers;
            }

            // If no existing tracks, use the stored count or default
            var count = tile.RollingLineCount ?? DefaultRollingLineCount;
            return Enumerable.Range(RollingLineStartNumber, count).ToList();
        }

        private static void NormalizeRollingLineTracks(TileModel tile, List<int> desiredNumbers)
        {
            var desiredNumbersSet = desiredNumbers.Select(n => n.ToString()).ToHashSet();
            var existing = tile.Tracks
                .Where(t => t.Kind == TrackKind.RollingLine)
                .ToDictionary(t => t.Name, StringComparer.OrdinalIgnoreCase);

            var rollingTracks = new List<TrackModel>();
            foreach (var number in desiredNumbers.OrderBy(n => n))
            {
                var key = number.ToString();
                if (existing.TryGetValue(key, out var track))
                {
                    rollingTracks.Add(track);
                }
                else
                {
                    rollingTracks.Add(new TrackModel
                    {
                        Name = key,
                        Kind = TrackKind.RollingLine
                    });
                }
            }

            var nonRollingTracks = tile.Tracks.Where(t => t.Kind != TrackKind.RollingLine).ToList();
            tile.Tracks.Clear();
            foreach (var track in nonRollingTracks)
            {
                tile.Tracks.Add(track);
            }
            foreach (var track in rollingTracks)
            {
                tile.Tracks.Add(track);
            }

            tile.RefreshTrackCollections();
        }

        private static List<int> EnsureRollingLineNumbersWithinAssignments(TileModel tile, List<int> requestedNumbers)
        {
            var assignedNumbers = tile.Tracks
                .Where(t => t.Kind == TrackKind.RollingLine && t.Locomotives.Any())
                .Select(t => int.TryParse(t.Name, out var value) ? value : 0)
                .Where(value => value > 0)
                .ToList();

            if (!assignedNumbers.Any())
            {
                return requestedNumbers;
            }

            // Include all assigned numbers in the result
            var result = new HashSet<int>(requestedNumbers);
            foreach (var assigned in assignedNumbers)
            {
                result.Add(assigned);
            }

            return result.OrderBy(n => n).ToList();
        }

        private static string FormatRollingLineRange(List<int> numbers)
        {
            if (numbers == null || numbers.Count == 0)
            {
                return "";
            }

            if (numbers.Count == 1)
            {
                return numbers[0].ToString();
            }

            var sorted = numbers.OrderBy(n => n).ToList();
            var ranges = new List<string>();
            int rangeStart = sorted[0];
            int rangeEnd = sorted[0];

            for (int i = 1; i < sorted.Count; i++)
            {
                if (sorted[i] == rangeEnd + 1)
                {
                    rangeEnd = sorted[i];
                }
                else
                {
                    ranges.Add(rangeStart == rangeEnd ? rangeStart.ToString() : $"{rangeStart}-{rangeEnd}");
                    rangeStart = sorted[i];
                    rangeEnd = sorted[i];
                }
            }

            ranges.Add(rangeStart == rangeEnd ? rangeStart.ToString() : $"{rangeStart}-{rangeEnd}");

            return string.Join(", ", ranges);
        }

        private List<int>? PromptRollingLineNumbers(int defaultCount)
        {
            var dialog = new RollingLineRangeDialog(defaultCount.ToString())
            {
                Owner = this
            };

            if (dialog.ShowDialog() != true)
            {
                return null;
            }

            return dialog.SelectedNumbers;
        }

        private void RollingLineRow_DragOver(object sender, DragEventArgs e)
        {
            if (sender is not Border border || border.DataContext is not TrackModel track)
            {
                return;
            }

            if (!e.Data.GetDataPresent(typeof(LocomotiveModel)))
            {
                e.Effects = DragDropEffects.None;
                return;
            }

            var loco = (LocomotiveModel)e.Data.GetData(typeof(LocomotiveModel))!;
            
            // Prevent dragging ghost locomotives
            if (loco.IsForecastGhost)
            {
                e.Effects = DragDropEffects.None;
                border.Background = Brushes.MistyRose;
                e.Handled = true;
                return;
            }
            
            // Pour les lignes de roulement, on permet toujours le drop
            // Si la ligne est occupée par une autre loco, on fera un swap
            var canDrop = !track.Locomotives.Any() || track.Locomotives.Contains(loco) || 
                          (track.Locomotives.Count == 1 && !track.Locomotives.Contains(loco));
            
            e.Effects = canDrop ? DragDropEffects.Move : DragDropEffects.None;
            
            // Feedback visuel: bleu si drop possible (même pour swap)
            border.Background = canDrop ? new SolidColorBrush(Color.FromArgb(50, 0, 120, 215)) : Brushes.MistyRose;
            e.Handled = true;
        }

        private void RollingLineRow_DragLeave(object sender, DragEventArgs e)
        {
            if (sender is Border border)
            {
                border.Background = Brushes.Transparent;
            }
        }

        private void RollingLineRow_Drop(object sender, DragEventArgs e)
        {
            if (sender is not Border border || border.DataContext is not TrackModel track)
            {
                return;
            }

            border.Background = Brushes.Transparent;

            if (!e.Data.GetDataPresent(typeof(LocomotiveModel)))
            {
                return;
            }

            var loco = (LocomotiveModel)e.Data.GetData(typeof(LocomotiveModel))!;
            
            // Prevent dropping ghost locomotives
            if (loco.IsForecastGhost)
            {
                MessageBox.Show("Les locomotives fantômes (prévision) ne peuvent pas être déplacées.", "Action impossible",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            // Si la ligne cible contient déjà une locomotive différente, on fait un swap
            if (track.Locomotives.Any() && !track.Locomotives.Contains(loco))
            {
                var existingLoco = track.Locomotives.First();
                
                // Check if existing is a ghost - don't allow swap with ghosts
                if (existingLoco.IsForecastGhost)
                {
                    MessageBox.Show("Impossible d'échanger avec une locomotive fantôme (prévision).", "Action impossible",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                var sourceTrack = _tiles.SelectMany(t => t.Tracks).FirstOrDefault(t => t.Locomotives.Contains(loco));
                
                if (sourceTrack != null && sourceTrack.Kind == TrackKind.RollingLine)
                {
                    // Swap: échanger les locomotives entre les deux lignes
                    SwapLocomotivesBetweenTracks(loco, sourceTrack, existingLoco, track);
                    _repository.AddHistory("LocomotiveMoved", $"Croisement: {loco.Number} ↔ {existingLoco.Number} entre {sourceTrack.Name} et {track.Name}.");
                    PersistState();
                    e.Handled = true;
                    return;
                }
                else
                {
                    // Si la source n'est pas une ligne de roulement, on bloque
                    MessageBox.Show("Une seule locomotive est autorisée par ligne de roulement.", "Action impossible",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
            }

            // Cas normal: déplacement simple (ligne vide ou même loco)
            MoveLocomotiveToTrack(loco, track, 0);
            PersistState();
            e.Handled = true;
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

                    if (IsOverlapping(tile, other))
                    {
                        tile.X += step;
                        tile.Y += step;
                        hasOverlap = true;
                        break;
                    }
                }
            }
        }

        private static bool IsOverlapping(TileModel first, TileModel second)
        {
            return first.X < second.X + second.Width
                   && first.X + first.Width > second.X
                   && first.Y < second.Y + second.Height
                   && first.Y + first.Height > second.Y;
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

        private void ApplyTheme(bool darkMode)
        {
            if (darkMode)
            {
                Resources["AppBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(34, 34, 34));
                Resources["PanelBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(45, 45, 45));
                Resources["CanvasBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(40, 40, 40));
                Resources["TileBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(55, 55, 55));
                Resources["TileBorderBrush"] = new SolidColorBrush(Color.FromRgb(90, 90, 90));
                Resources["TrackBorderBrush"] = new SolidColorBrush(Color.FromRgb(80, 80, 80));
                Resources["ListBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(50, 50, 50));
                Resources["ListBorderBrush"] = new SolidColorBrush(Color.FromRgb(90, 90, 90));
                Resources["AppForegroundBrush"] = new SolidColorBrush(Colors.WhiteSmoke);
                Resources["MenuBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(45, 45, 45));
                Resources["MenuBorderBrush"] = new SolidColorBrush(Color.FromRgb(70, 70, 70));
                Resources["ToolBarBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(45, 45, 45));
                Resources["ToolBarBorderBrush"] = new SolidColorBrush(Color.FromRgb(70, 70, 70));
            }
            else
            {
                Resources["AppBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(242, 242, 242));
                Resources["PanelBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(239, 244, 255));
                Resources["CanvasBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(247, 247, 247));
                Resources["TileBackgroundBrush"] = new SolidColorBrush(Colors.White);
                Resources["TileBorderBrush"] = new SolidColorBrush(Color.FromRgb(176, 176, 176));
                Resources["TrackBorderBrush"] = new SolidColorBrush(Color.FromRgb(224, 224, 224));
                Resources["ListBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(245, 245, 245));
                Resources["ListBorderBrush"] = new SolidColorBrush(Color.FromRgb(221, 221, 221));
                Resources["AppForegroundBrush"] = new SolidColorBrush(Color.FromRgb(17, 17, 17));
                Resources["MenuBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(242, 242, 242));
                Resources["MenuBorderBrush"] = new SolidColorBrush(Color.FromRgb(221, 221, 221));
                Resources["ToolBarBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(242, 242, 242));
                Resources["ToolBarBorderBrush"] = new SolidColorBrush(Color.FromRgb(221, 221, 221));
            }
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
                    Width = tile.Width,
                    Height = tile.Height,
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
                    Width = tile.Width,
                    Height = tile.Height,
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
                        loco.AssignedTrackOffsetX = null;
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
                    Y = layoutTile.Y,
                    Width = layoutTile.Width > 0 ? layoutTile.Width : MinTileWidth,
                    Height = layoutTile.Height > 0 ? layoutTile.Height : MinTileHeight
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
            UpdateTileCanvasExtent();
        }

        private void UpdateTileCanvasExtent()
        {
            if (TileCanvas == null)
            {
                return;
            }

            if (!_tiles.Any())
            {
                TileCanvas.Width = Math.Max(0, TileScrollViewer?.ViewportWidth ?? 0);
                TileCanvas.Height = Math.Max(0, TileScrollViewer?.ViewportHeight ?? 0);
                return;
            }

            var maxX = _tiles.Max(tile => tile.X + tile.Width);
            var maxY = _tiles.Max(tile => tile.Y + tile.Height);
            var viewportWidth = TileScrollViewer?.ViewportWidth ?? 0;
            var viewportHeight = TileScrollViewer?.ViewportHeight ?? 0;
            TileCanvas.Width = Math.Max(viewportWidth, maxX + CanvasPadding);
            TileCanvas.Height = Math.Max(viewportHeight, maxY + CanvasPadding);
        }

        private Point GetDropPositionInWorkspace(MouseEventArgs e)
        {
            if (TileScrollViewer == null || TileCanvas == null)
            {
                return e.GetPosition(null);
            }

            var viewportPosition = e.GetPosition(TileScrollViewer);
            return new Point(
                viewportPosition.X + TileScrollViewer.HorizontalOffset,
                viewportPosition.Y + TileScrollViewer.VerticalOffset);
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
            public double Width { get; set; }
            public double Height { get; set; }
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
