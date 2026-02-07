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
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
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
            if (targetTrack.Kind == TrackKind.RollingLine && targetTrack.Locomotives.Any() && !targetTrack.Locomotives.Contains(loco))
            {
                MessageBox.Show("Une seule locomotive est autorisée par ligne de roulement.", "Action impossible",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

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
            loco.AssignedTrackOffsetX = null;
            EnsureTrackOffsets(targetTrack);
            _repository.AddHistory("LocomotiveMoved", $"Loco {loco.Number} déplacée vers {targetTrack.Name}.");
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

            var dialog = new StatusDialog(loco) { Owner = this };
            if (dialog.ShowDialog() == true)
            {
                _repository.AddHistory("StatusChanged", $"Statut modifié pour {loco.Number}.");
                PersistState();
                RefreshTapisT13();
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

            if (menuItem.CommandParameter is LocomotiveModel parameter)
            {
                return parameter;
            }

            if (menuItem.DataContext is LocomotiveModel dataContext)
            {
                return dataContext;
            }

            var contextMenu = menuItem.Parent as ContextMenu
                ?? ItemsControl.ItemsControlFromItemContainer(menuItem) as ContextMenu;
            if (contextMenu?.PlacementTarget is FrameworkElement element && element.DataContext is LocomotiveModel loco)
            {
                return loco;
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
                        loco.AssignedTrackOffsetX = null;
                    }
                }
            }

            _tiles.Clear();
            _repository.AddHistory("ResetTiles", "Suppression de toutes les tuiles.");
            UpdatePoolVisibility();
            PersistState();
            RefreshTapisT13();
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

        private static int EnsureRollingLineCountWithinAssignments(TileModel tile, List<int> requestedNumbers)
        {
            var assignedNumbers = tile.Tracks
                .Where(t => t.Kind == TrackKind.RollingLine && t.Locomotives.Any())
                .Select(t => int.TryParse(t.Name, out var value) ? value : 0)
                .Where(value => value > 0)
                .ToList();

            if (!assignedNumbers.Any())
            {
                return requestedNumbers.Count;
            }

            var maxAssigned = assignedNumbers.Max();
            var minAssigned = assignedNumbers.Min();
            
            // Check if all assigned numbers are included in the requested numbers
            if (assignedNumbers.All(n => requestedNumbers.Contains(n)))
            {
                return requestedNumbers.Count;
            }

            // Need to expand to include all assigned numbers
            var expandedNumbers = new HashSet<int>(requestedNumbers);
            foreach (var assigned in assignedNumbers)
            {
                expandedNumbers.Add(assigned);
            }

            return expandedNumbers.Count;
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
            if (numbers.Count == 0)
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
            var canDrop = !track.Locomotives.Any() || track.Locomotives.Contains(loco);
            e.Effects = canDrop ? DragDropEffects.Move : DragDropEffects.None;
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
            if (track.Locomotives.Any() && !track.Locomotives.Contains(loco))
            {
                MessageBox.Show("Une seule locomotive est autorisée par ligne de roulement.", "Action impossible",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

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
