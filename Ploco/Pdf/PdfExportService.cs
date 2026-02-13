using System;
using System.Collections.Generic;
using System.Linq;
using Ploco.Models;
using Ploco.Pdf.Annotations;
using Ploco.Pdf.Engine;
using Ploco.Pdf.Mapping;

namespace Ploco.Pdf
{
    /// <summary>
    /// High-level service for PDF export operations.
    /// Orchestrates the mapping layer and PDF engine to produce annotated PDFs.
    /// </summary>
    public class PdfExportService
    {
        /// <summary>
        /// Exports a PDF with locomotive placement annotations.
        /// </summary>
        /// <param name="sourcePdfPath">Path to the source PDF file.</param>
        /// <param name="outputPdfPath">Path where the new PDF should be saved.</param>
        /// <param name="placements">Collection of locomotive placements to annotate.</param>
        /// <param name="calibrations">Dictionary of calibrations by page index.</param>
        /// <param name="pageSizes">Dictionary of page sizes (page index -> (width, height) in points).</param>
        public void ExportPdfWithPlacements(
            string sourcePdfPath,
            string outputPdfPath,
            IEnumerable<PdfPlacementModel> placements,
            Dictionary<int, PdfTemplateCalibrationModel> calibrations,
            Dictionary<int, (double Width, double Height)> pageSizes)
        {
            if (string.IsNullOrWhiteSpace(sourcePdfPath))
                throw new ArgumentException("Source PDF path cannot be empty.", nameof(sourcePdfPath));

            if (string.IsNullOrWhiteSpace(outputPdfPath))
                throw new ArgumentException("Output PDF path cannot be empty.", nameof(outputPdfPath));

            if (placements == null)
                throw new ArgumentNullException(nameof(placements));

            if (calibrations == null)
                throw new ArgumentNullException(nameof(calibrations));

            // Convert placements to annotations
            var annotations = new List<PdfAnnotationBase>();

            foreach (var placement in placements)
            {
                if (!calibrations.TryGetValue(placement.PageIndex, out var calibration))
                {
                    // Skip placements without calibration
                    continue;
                }

                if (!pageSizes.TryGetValue(placement.PageIndex, out var pageSize))
                {
                    // Skip placements without page size info
                    continue;
                }

                var mapper = new PdfCoordinateMapper(calibration, pageSize.Height);
                var rect = mapper.GetLocoRectangle(placement.MinuteOfDay, placement.RoulementId);

                if (!rect.HasValue)
                {
                    // Skip placements that can't be mapped
                    continue;
                }

                var annotation = new LocoRectangleAnnotation
                {
                    PageIndex = placement.PageIndex,
                    X = rect.Value.X,
                    Y = rect.Value.Y,
                    Width = rect.Value.Width,
                    Height = rect.Value.Height,
                    LocomotiveNumber = placement.LocNumber,
                    Status = placement.Status,
                    TractionPercent = placement.TractionPercent,
                    OnTrain = placement.OnTrain,
                    TrainNumber = placement.TrainNumber,
                    BackgroundColor = GetBackgroundColor(placement.Status),
                    BorderColor = GetBorderColor(placement.Status),
                    TextColor = "#FFFFFF",
                    BorderWidth = 1.5,
                    FontSize = 10.0
                };

                annotations.Add(annotation);
            }

            // Export PDF with annotations
            using var engine = new PdfExportEngine();
            engine.ExportPdfWithAnnotations(sourcePdfPath, outputPdfPath, annotations, pageSizes);
        }

        /// <summary>
        /// Gets the background color for a locomotive status.
        /// </summary>
        private string GetBackgroundColor(LocomotiveStatus status)
        {
            return status switch
            {
                LocomotiveStatus.HS => "#CD5C5C",           // IndianRed
                LocomotiveStatus.ManqueTraction => "#FFA500", // Orange
                LocomotiveStatus.DefautMineur => "#FFD700",   // Gold
                _ => "#2E8B57"                                // SeaGreen
            };
        }

        /// <summary>
        /// Gets the border color for a locomotive status.
        /// </summary>
        private string GetBorderColor(LocomotiveStatus status)
        {
            return status switch
            {
                LocomotiveStatus.HS => "#8B0000",           // DarkRed
                LocomotiveStatus.ManqueTraction => "#FF8C00", // DarkOrange
                LocomotiveStatus.DefautMineur => "#DAA520",   // GoldenRod
                _ => "#006400"                                // DarkGreen
            };
        }

        /// <summary>
        /// Validates that all required calibrations exist for the given placements.
        /// </summary>
        /// <returns>A list of page indices that are missing calibrations.</returns>
        public List<int> ValidateCalibrations(
            IEnumerable<PdfPlacementModel> placements,
            Dictionary<int, PdfTemplateCalibrationModel> calibrations)
        {
            var missingPages = new List<int>();
            var uniquePages = placements.Select(p => p.PageIndex).Distinct();

            foreach (var pageIndex in uniquePages)
            {
                if (!calibrations.ContainsKey(pageIndex))
                {
                    missingPages.Add(pageIndex);
                }
            }

            return missingPages;
        }

        /// <summary>
        /// Validates that all roulement IDs used in placements exist in calibrations.
        /// </summary>
        /// <returns>A dictionary of page indices and missing roulement IDs.</returns>
        public Dictionary<int, List<string>> ValidateRoulements(
            IEnumerable<PdfPlacementModel> placements,
            Dictionary<int, PdfTemplateCalibrationModel> calibrations)
        {
            var missingRoulements = new Dictionary<int, List<string>>();

            var placementsByPage = placements.GroupBy(p => p.PageIndex);

            foreach (var pageGroup in placementsByPage)
            {
                var pageIndex = pageGroup.Key;
                
                if (!calibrations.TryGetValue(pageIndex, out var calibration))
                {
                    continue; // Already flagged by ValidateCalibrations
                }

                var validRoulements = new HashSet<string>(
                    calibration.Rows.Select(r => r.RoulementId).Concat(
                    calibration.VisualLines
                        .Where(l => l.Type == CalibrationLineType.Horizontal)
                        .Select(l => l.Label)),
                    StringComparer.OrdinalIgnoreCase
                );

                var missing = pageGroup
                    .Select(p => p.RoulementId)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Where(r => !validRoulements.Contains(r))
                    .ToList();

                if (missing.Any())
                {
                    missingRoulements[pageIndex] = missing;
                }
            }

            return missingRoulements;
        }
    }
}
