using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Docnet.Core;
using Docnet.Core.Models;
using Microsoft.Win32;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf.IO;
using Ploco.Data;
using Ploco.Models;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace Ploco.Dialogs
{
    public partial class PlanningPdfWindow : Window
    {
        private readonly PlocoRepository _repository;
        private readonly ObservableCollection<PdfPageViewModel> _pages = new();
        private PdfDocumentModel? _document;
        private readonly Dictionary<int, PdfTemplateCalibrationModel> _calibrations = new();
        private readonly Dictionary<int, (double Width, double Height)> _pdfPageSizes = new();
        private double _zoom = 1.0;
        private bool _isDraggingToken;
        private PdfPlacementViewModel? _draggedPlacement;
        private Point _dragStart;
        private bool _isCalibrationMode;
        private CalibrationStep _calibrationStep = CalibrationStep.None;

        public PlanningPdfWindow(PlocoRepository repository)
        {
            InitializeComponent();
            _repository = repository;
            PdfPages.ItemsSource = _pages;
            DocumentDatePicker.SelectedDate = DateTime.Today;
            UpdateZoomLabel();
        }

        private void LoadPdf_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "PDF (*.pdf)|*.pdf",
                Title = "Choisir un PDF"
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            LoadPdf(dialog.FileName);
        }

        private void LoadPdf(string filePath)
        {
            if (!File.Exists(filePath))
            {
                MessageBox.Show("Fichier introuvable.", "Planning PDF", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _pages.Clear();
            _calibrations.Clear();
            _pdfPageSizes.Clear();

            var date = DocumentDatePicker.SelectedDate ?? DateTime.Today;
            var templateHash = ComputeFileHash(filePath);
            var pageCount = GetPdfPageCount(filePath);
            _document = _repository.GetPdfDocument(filePath, date) ?? new PdfDocumentModel
            {
                FilePath = filePath,
                DocumentDate = date,
                TemplateHash = templateHash,
                PageCount = pageCount
            };

            _document.TemplateHash = templateHash;
            _document.PageCount = pageCount;
            _document = _repository.SavePdfDocument(_document);

            var calibrations = _repository.LoadTemplateCalibrations(templateHash);
            foreach (var calibration in calibrations)
            {
                _calibrations[calibration.PageIndex] = calibration;
            }

            var extracted = ExtractCalibration(filePath);
            foreach (var calibration in extracted)
            {
                if (!_calibrations.ContainsKey(calibration.PageIndex))
                {
                    _calibrations[calibration.PageIndex] = calibration;
                    _repository.SaveTemplateCalibration(calibration);
                }
            }

            RenderPages(filePath);
            LoadPlacements();
        }

        private void RenderPages(string filePath)
        {
            var renderer = DocLib.Instance.GetDocReader(filePath, new PageDimensions(1440, 2030));
            for (var pageIndex = 0; pageIndex < renderer.GetPageCount(); pageIndex++)
            {
                using var pageReader = renderer.GetPageReader(pageIndex);
                var rawBytes = pageReader.GetImage();
                var (pdfWidth, pdfHeight) = _pdfPageSizes.TryGetValue(pageIndex, out var size) ? size : (pageReader.GetPageWidth(), pageReader.GetPageHeight());
                var page = new PdfPageViewModel(pageIndex, rawBytes, pageReader.GetPageWidth(), pageReader.GetPageHeight(), pdfWidth, pdfHeight);
                page.ApplyZoom(_zoom);
                _pages.Add(page);
            }
        }

        private void LoadPlacements()
        {
            if (_document == null)
            {
                return;
            }

            var placements = _repository.LoadPlacements(_document.Id);
            foreach (var page in _pages)
            {
                page.Placements.Clear();
            }

            foreach (var placement in placements)
            {
                var page = _pages.FirstOrDefault(p => p.PageIndex == placement.PageIndex);
                if (page == null)
                {
                    continue;
                }

                var vm = PdfPlacementViewModel.FromModel(placement);
                ApplyPlacementPosition(page, vm);
                page.Placements.Add(vm);
            }
        }

        private void ApplyPlacementPosition(PdfPageViewModel page, PdfPlacementViewModel placement)
        {
            if (!_calibrations.TryGetValue(page.PageIndex, out var calibration))
            {
                placement.X = 10;
                placement.Y = 10;
                return;
            }

            var x = MapMinuteToX(calibration, page, placement.MinuteOfDay);
            var row = calibration.Rows.FirstOrDefault(r => string.Equals(r.RoulementId, placement.RoulementId, StringComparison.OrdinalIgnoreCase));
            var y = row != null ? MapPdfYToImage(page, row.YCenter) : 10;
            placement.X = x - placement.Width / 2;
            placement.Y = y - placement.Height / 2;
        }

        private void Overlay_DragOver(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(typeof(LocomotiveModel)) && !_isDraggingToken)
            {
                e.Effects = DragDropEffects.None;
                return;
            }

            e.Effects = DragDropEffects.Move;
            e.Handled = true;
        }

        private void Overlay_Drop(object sender, DragEventArgs e)
        {
            if (sender is not Canvas canvas || canvas.DataContext is not PdfPageViewModel page)
            {
                return;
            }

            if (_document == null)
            {
                MessageBox.Show("Chargez un PDF avant d'ajouter des placements.", "Planning PDF", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!_calibrations.TryGetValue(page.PageIndex, out var calibration))
            {
                MessageBox.Show("Calibration manquante pour cette page. Lancez un recalibrage.", "Planning PDF",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dropPoint = GetCanvasPoint(e.GetPosition(canvas));
            if (_isDraggingToken && _draggedPlacement != null)
            {
                UpdatePlacementFromDrop(page, calibration, _draggedPlacement, dropPoint);
                _isDraggingToken = false;
                _draggedPlacement = null;
                return;
            }

            if (!e.Data.GetDataPresent(typeof(LocomotiveModel)))
            {
                return;
            }

            var loco = (LocomotiveModel)e.Data.GetData(typeof(LocomotiveModel))!;
            var placement = BuildPlacementFromLocomotive(loco, _document.Id, page.PageIndex);
            UpdatePlacementFromDrop(page, calibration, placement, dropPoint);
            SavePlacement(page, placement);
        }

        private void UpdatePlacementFromDrop(PdfPageViewModel page, PdfTemplateCalibrationModel calibration, PdfPlacementViewModel placement, Point dropPoint)
        {
            var minute = MapXToMinute(calibration, page, dropPoint.X);
            var row = FindNearestRow(calibration, page, dropPoint.Y);
            if (row == null)
            {
                MessageBox.Show("Aucun roulement détecté pour ce drop.", "Planning PDF", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            placement.MinuteOfDay = minute;
            placement.RoulementId = row.RoulementId;
            placement.X = dropPoint.X - placement.Width / 2;
            placement.Y = dropPoint.Y - placement.Height / 2;

            if (placement.Id == 0)
            {
                page.Placements.Add(placement);
            }
            else
            {
                SavePlacement(page, placement);
            }
        }

        private void SavePlacement(PdfPageViewModel page, PdfPlacementViewModel placement)
        {
            if (_document == null)
            {
                return;
            }

            var existing = _pages.SelectMany(p => p.Placements)
                .FirstOrDefault(p => p.LocNumber == placement.LocNumber && p.Id != placement.Id);
            if (existing != null)
            {
                var existingPage = _pages.FirstOrDefault(p => p.Placements.Contains(existing));
                existingPage?.Placements.Remove(existing);
                if (existing.Id > 0)
                {
                    _repository.DeletePlacement(existing.Id);
                }
            }

            placement.PdfDocumentId = _document.Id;
            var model = placement.ToModel();
            _repository.SavePlacement(model);
            placement.Id = model.Id;
            placement.UpdatedAt = model.UpdatedAt;
            placement.CreatedAt = model.CreatedAt;
        }

        private PdfPlacementViewModel BuildPlacementFromLocomotive(LocomotiveModel loco, int documentId, int pageIndex)
        {
            return new PdfPlacementViewModel
            {
                PdfDocumentId = documentId,
                PageIndex = pageIndex,
                LocNumber = loco.Number,
                Status = loco.Status,
                TractionPercent = loco.TractionPercent,
                HsReason = loco.HsReason
            };
        }

        private void Token_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is PdfPlacementViewModel placement)
            {
                _draggedPlacement = placement;
                _dragStart = e.GetPosition(PdfPages);
                _isDraggingToken = true;
                border.CaptureMouse();
            }
        }

        private void Token_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDraggingToken || _draggedPlacement == null || e.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }

            if (sender is Border border)
            {
                DragDrop.DoDragDrop(border, _draggedPlacement, DragDropEffects.Move);
            }
        }

        private void Overlay_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!_isCalibrationMode || sender is not Canvas canvas || canvas.DataContext is not PdfPageViewModel page)
            {
                return;
            }

            if (!_calibrations.TryGetValue(page.PageIndex, out var calibration))
            {
                calibration = new PdfTemplateCalibrationModel
                {
                    TemplateHash = _document?.TemplateHash ?? string.Empty,
                    PageIndex = page.PageIndex,
                    XStart = 0,
                    XEnd = page.PageWidth
                };
                _calibrations[page.PageIndex] = calibration;
            }

            var point = GetCanvasPoint(e.GetPosition(canvas));
            var pdfX = MapImageXToPdf(page, point.X);
            var pdfY = MapImageYToPdf(page, point.Y);

            if (_calibrationStep == CalibrationStep.SelectStart)
            {
                calibration.XStart = pdfX;
                _calibrationStep = CalibrationStep.SelectEnd;
                MessageBox.Show("Cliquez sur la position 24:00.", "Calibrage", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else if (_calibrationStep == CalibrationStep.SelectEnd)
            {
                calibration.XEnd = pdfX;
                _calibrationStep = CalibrationStep.SelectRows;
                MessageBox.Show("Cliquez sur une ligne et saisissez son identifiant.", "Calibrage", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else if (_calibrationStep == CalibrationStep.SelectRows)
            {
                var input = new SimpleTextDialog("Roulement", "Identifiant (ex: @1101) :", string.Empty)
                {
                    Owner = this
                };
                if (input.ShowDialog() == true && !string.IsNullOrWhiteSpace(input.ResponseText))
                {
                    calibration.Rows.Add(new PdfTemplateRowMapping
                    {
                        RoulementId = input.ResponseText.Trim(),
                        YCenter = pdfY
                    });
                }
            }

            _repository.SaveTemplateCalibration(calibration);
        }

        private void Overlay_MouseMove(object sender, MouseEventArgs e)
        {
        }

        private void Overlay_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isDraggingToken = false;
            _draggedPlacement = null;
        }

        private void EditPlacement_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.DataContext is PdfPlacementViewModel placement)
            {
                var dialog = new PdfPlacementDialog(placement) { Owner = this };
                if (dialog.ShowDialog() == true)
                {
                    SavePlacement(_pages.First(p => p.PageIndex == placement.PageIndex), placement);
                }
            }
        }

        private void DeletePlacement_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.DataContext is PdfPlacementViewModel placement)
            {
                var page = _pages.First(p => p.PageIndex == placement.PageIndex);
                page.Placements.Remove(placement);
                if (placement.Id > 0)
                {
                    _repository.DeletePlacement(placement.Id);
                }
            }
        }

        private void ExportPdf_Click(object sender, RoutedEventArgs e)
        {
            if (_document == null)
            {
                MessageBox.Show("Aucun PDF chargé.", "Planning PDF", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new SaveFileDialog
            {
                Filter = "PDF (*.pdf)|*.pdf",
                FileName = $"planning_{_document.DocumentDate:yyyyMMdd}.pdf"
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            ExportPdf(dialog.FileName);
        }

        private void ExportPdf(string outputPath)
        {
            if (_document == null)
            {
                return;
            }

            using var input = PdfReader.Open(_document.FilePath, PdfDocumentOpenMode.Modify);
            foreach (var page in _pages)
            {
                if (!_calibrations.TryGetValue(page.PageIndex, out var calibration))
                {
                    continue;
                }

                var pdfPage = input.Pages[page.PageIndex];
                using var gfx = XGraphics.FromPdfPage(pdfPage);
                foreach (var placement in page.Placements)
                {
                    var xPdf = MapMinuteToPdfX(calibration, placement.MinuteOfDay);
                    var row = calibration.Rows.FirstOrDefault(r => string.Equals(r.RoulementId, placement.RoulementId, StringComparison.OrdinalIgnoreCase));
                    if (row == null)
                    {
                        continue;
                    }

                    var yPdf = row.YCenter;
                    var rectWidth = 46;
                    var rectHeight = 18;
                    var x = xPdf - rectWidth / 2;
                    var y = pdfPage.Height - yPdf - rectHeight / 2;

                    var brush = placement.Status switch
                    {
                        LocomotiveStatus.HS => XBrushes.IndianRed,
                        LocomotiveStatus.ManqueTraction => XBrushes.Orange,
                        _ => XBrushes.SeaGreen
                    };
                    gfx.DrawRectangle(brush, x, y, rectWidth, rectHeight);
                    var font = new XFont("Arial", 8, XFontStyle.Bold);
                    gfx.DrawString(placement.LocNumber.ToString(), font, XBrushes.White,
                        new XRect(x, y, rectWidth, rectHeight), XStringFormats.CenterLeft);

                    var badge = placement.Status == LocomotiveStatus.ManqueTraction && placement.TractionPercent.HasValue
                        ? $"{placement.TractionPercent}%"
                        : placement.Status == LocomotiveStatus.HS ? "HS" : string.Empty;
                    if (!string.IsNullOrWhiteSpace(badge))
                    {
                        gfx.DrawString(badge, new XFont("Arial", 7, XFontStyle.Regular), XBrushes.White,
                            new XRect(x, y, rectWidth, rectHeight), XStringFormats.CenterRight);
                    }
                }
            }

            input.Save(outputPath);
            MessageBox.Show("Export terminé.", "Planning PDF", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExportCsv_Click(object sender, RoutedEventArgs e)
        {
            if (_document == null)
            {
                return;
            }

            var dialog = new SaveFileDialog
            {
                Filter = "CSV (*.csv)|*.csv",
                FileName = $"planning_{_document.DocumentDate:yyyyMMdd}.csv"
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            var builder = new StringBuilder();
            builder.AppendLine("pageIndex,roulementId,minuteOfDay,heure,locNumber,status,tractionPercent,onTrain,trainNumber,hsReason,comment");
            foreach (var placement in _pages.SelectMany(p => p.Placements))
            {
                var time = TimeSpan.FromMinutes(placement.MinuteOfDay);
                builder.AppendLine($"{placement.PageIndex},{placement.RoulementId},{placement.MinuteOfDay},{time:hh\\:mm},{placement.LocNumber},{placement.Status},{placement.TractionPercent},{placement.OnTrain},{placement.TrainNumber},{placement.HsReason},{placement.Comment}");
            }

            File.WriteAllText(dialog.FileName, builder.ToString(), Encoding.UTF8);
            MessageBox.Show("Export CSV terminé.", "Planning PDF", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Recalibrate_Click(object sender, RoutedEventArgs e)
        {
            if (_document == null)
            {
                MessageBox.Show("Chargez un PDF avant de calibrer.", "Planning PDF", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _isCalibrationMode = !_isCalibrationMode;
            _calibrationStep = _isCalibrationMode ? CalibrationStep.SelectStart : CalibrationStep.None;
            MessageBox.Show(_isCalibrationMode
                    ? "Mode calibrage activé. Cliquez sur la position 00:00."
                    : "Mode calibrage désactivé.",
                "Calibrage", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ZoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _zoom = e.NewValue;
            UpdateZoomLabel();
            foreach (var page in _pages)
            {
                page.ApplyZoom(_zoom);
            }
        }

        private void UpdateZoomLabel()
        {
            if (ZoomLabel != null)
            {
                ZoomLabel.Text = $"{_zoom:P0}";
            }
        }

        private static int GetPdfPageCount(string filePath)
        {
            using var pdf = PdfDocument.Open(filePath);
            return pdf.NumberOfPages;
        }

        private static string ComputeFileHash(string filePath)
        {
            using var stream = File.OpenRead(filePath);
            using var sha = SHA256.Create();
            return Convert.ToHexString(sha.ComputeHash(stream));
        }

        private List<PdfTemplateCalibrationModel> ExtractCalibration(string filePath)
        {
            var results = new List<PdfTemplateCalibrationModel>();
            using var pdf = PdfDocument.Open(filePath);
            var rowRegex = new Regex(@"@?\d{4}", RegexOptions.Compiled);
            var timeRegex = new Regex(@"^(?:[01]?\d|2[0-3])$", RegexOptions.Compiled);

            for (var pageIndex = 0; pageIndex < pdf.NumberOfPages; pageIndex++)
            {
                var page = pdf.GetPage(pageIndex);
                _pdfPageSizes[pageIndex] = (page.Width, page.Height);
                var words = page.GetWords().ToList();
                var rowCandidates = words
                    .Where(w => rowRegex.IsMatch(w.Text))
                    .Select(w => new
                    {
                        Id = w.Text.StartsWith("@") ? w.Text : $"@{w.Text}",
                        Y = (w.BoundingBox.Bottom + w.BoundingBox.Top) / 2
                    })
                    .GroupBy(item => item.Id)
                    .Select(group => new PdfTemplateRowMapping
                    {
                        RoulementId = group.Key,
                        YCenter = group.Average(item => item.Y)
                    })
                    .ToList();

                var timeCandidates = words
                    .Where(w => timeRegex.IsMatch(w.Text))
                    .Select(w => new { Hour = int.Parse(w.Text, CultureInfo.InvariantCulture), X = (w.BoundingBox.Left + w.BoundingBox.Right) / 2 })
                    .GroupBy(item => item.Hour)
                    .Select(group => new { Hour = group.Key, X = group.Average(item => item.X) })
                    .ToList();

                if (!rowCandidates.Any() || timeCandidates.Count < 2)
                {
                    continue;
                }

                var xStart = timeCandidates.OrderBy(t => t.Hour).First().X;
                var xEnd = timeCandidates.OrderBy(t => t.Hour).Last().X;
                results.Add(new PdfTemplateCalibrationModel
                {
                    TemplateHash = _document?.TemplateHash ?? string.Empty,
                    PageIndex = pageIndex,
                    XStart = xStart,
                    XEnd = xEnd,
                    Rows = rowCandidates
                });
            }

            return results;
        }

        private static int MapXToMinute(PdfTemplateCalibrationModel calibration, PdfPageViewModel page, double xImage)
        {
            var pdfX = MapImageXToPdf(page, xImage);
            var ratio = (pdfX - calibration.XStart) / (calibration.XEnd - calibration.XStart);
            ratio = Math.Max(0, Math.Min(1, ratio));
            return (int)Math.Round(ratio * 1440);
        }

        private static double MapMinuteToX(PdfTemplateCalibrationModel calibration, PdfPageViewModel page, int minute)
        {
            var pdfX = MapMinuteToPdfX(calibration, minute);
            return MapPdfXToImage(page, pdfX);
        }

        private static double MapMinuteToPdfX(PdfTemplateCalibrationModel calibration, int minute)
        {
            var ratio = minute / 1440.0;
            return calibration.XStart + ratio * (calibration.XEnd - calibration.XStart);
        }

        private static PdfTemplateRowMapping? FindNearestRow(PdfTemplateCalibrationModel calibration, PdfPageViewModel page, double yImage)
        {
            var pdfY = MapImageYToPdf(page, yImage);
            return calibration.Rows.OrderBy(r => Math.Abs(r.YCenter - pdfY)).FirstOrDefault();
        }

        private static double MapPdfXToImage(PdfPageViewModel page, double pdfX)
        {
            return pdfX * page.ScaleX;
        }

        private static double MapPdfYToImage(PdfPageViewModel page, double pdfY)
        {
            return (page.PdfHeight - pdfY) * page.ScaleY;
        }

        private static double MapImageXToPdf(PdfPageViewModel page, double imageX)
        {
            return imageX / page.ScaleX;
        }

        private static double MapImageYToPdf(PdfPageViewModel page, double imageY)
        {
            return page.PdfHeight - (imageY / page.ScaleY);
        }

        private Point GetCanvasPoint(Point position)
        {
            return new Point(position.X / _zoom, position.Y / _zoom);
        }

        private sealed class PdfPageViewModel
        {
            public int PageIndex { get; }
            public BitmapSource PageImage { get; }
            public double PdfWidth { get; }
            public double PdfHeight { get; }
            public double ScaleX { get; private set; }
            public double ScaleY { get; private set; }
            public double PageWidth { get; private set; }
            public double PageHeight { get; private set; }
            public ObservableCollection<PdfPlacementViewModel> Placements { get; } = new();

            public PdfPageViewModel(int pageIndex, byte[] imageBytes, int width, int height, double pdfWidth, double pdfHeight)
            {
                PageIndex = pageIndex;
                PdfWidth = pdfWidth;
                PdfHeight = pdfHeight;
                PageImage = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, imageBytes, width * 4);
                ApplyZoom(1.0);
            }

            public void ApplyZoom(double zoom)
            {
                PageWidth = PageImage.PixelWidth * zoom;
                PageHeight = PageImage.PixelHeight * zoom;
                ScaleX = PageWidth / PdfWidth;
                ScaleY = PageHeight / PdfHeight;
            }
        }

        public class PdfPlacementViewModel
        {
            public int Id { get; set; }
            public int PdfDocumentId { get; set; }
            public int PageIndex { get; set; }
            public string RoulementId { get; set; } = string.Empty;
            public int MinuteOfDay { get; set; }
            public int LocNumber { get; set; }
            public LocomotiveStatus Status { get; set; }
            public int? TractionPercent { get; set; }
            public int? MotorsHsCount { get; set; }
            public string? HsReason { get; set; }
            public bool OnTrain { get; set; }
            public string? TrainNumber { get; set; }
            public string? TrainStopTime { get; set; }
            public string? Comment { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime UpdatedAt { get; set; }
            public double X { get; set; }
            public double Y { get; set; }
            public double Width => 48;
            public double Height => 24;
            public Brush StatusBrush => Status switch
            {
                LocomotiveStatus.HS => Brushes.IndianRed,
                LocomotiveStatus.ManqueTraction => Brushes.Orange,
                _ => Brushes.SeaGreen
            };

            public string BadgeText
            {
                get
                {
                    if (Status == LocomotiveStatus.ManqueTraction && TractionPercent.HasValue)
                    {
                        return $"{TractionPercent}%";
                    }

                    return Status == LocomotiveStatus.HS ? "HS" : string.Empty;
                }
            }

            public PdfPlacementModel ToModel()
            {
                return new PdfPlacementModel
                {
                    Id = Id,
                    PdfDocumentId = PdfDocumentId,
                    PageIndex = PageIndex,
                    RoulementId = RoulementId,
                    MinuteOfDay = MinuteOfDay,
                    LocNumber = LocNumber,
                    Status = Status,
                    TractionPercent = TractionPercent,
                    MotorsHsCount = MotorsHsCount,
                    HsReason = HsReason,
                    OnTrain = OnTrain,
                    TrainNumber = TrainNumber,
                    TrainStopTime = TrainStopTime,
                    Comment = Comment,
                    CreatedAt = CreatedAt,
                    UpdatedAt = UpdatedAt
                };
            }

            public static PdfPlacementViewModel FromModel(PdfPlacementModel model)
            {
                return new PdfPlacementViewModel
                {
                    Id = model.Id,
                    PdfDocumentId = model.PdfDocumentId,
                    PageIndex = model.PageIndex,
                    RoulementId = model.RoulementId,
                    MinuteOfDay = model.MinuteOfDay,
                    LocNumber = model.LocNumber,
                    Status = model.Status,
                    TractionPercent = model.TractionPercent,
                    MotorsHsCount = model.MotorsHsCount,
                    HsReason = model.HsReason,
                    OnTrain = model.OnTrain,
                    TrainNumber = model.TrainNumber,
                    TrainStopTime = model.TrainStopTime,
                    Comment = model.Comment,
                    CreatedAt = model.CreatedAt,
                    UpdatedAt = model.UpdatedAt
                };
            }
        }

        private enum CalibrationStep
        {
            None,
            SelectStart,
            SelectEnd,
            SelectRows
        }
    }
}
