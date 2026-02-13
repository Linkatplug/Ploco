using System;
using System.Collections.Generic;
using System.Linq;
using Ploco.Models;

namespace Ploco.Pdf.Mapping
{
    /// <summary>
    /// Maps business coordinates to PDF coordinates.
    /// This is the bridge between the business domain (minutes, line IDs) and PDF space (X, Y points).
    /// </summary>
    public class PdfCoordinateMapper
    {
        // Constants for default dimensions
        private const int MinutesPerDay = 1440; // 24 hours * 60 minutes
        private const double DefaultLocomotiveRectWidth = 46.0;
        private const double DefaultLocomotiveRectHeight = 18.0;
        private const double DefaultXPosition = 50.0;

        private readonly PdfTemplateCalibrationModel _calibration;
        private readonly double _pdfPageHeight;

        /// <summary>
        /// Initializes a new instance of the PdfCoordinateMapper.
        /// </summary>
        /// <param name="calibration">The calibration data for a specific page.</param>
        /// <param name="pdfPageHeight">The height of the PDF page in points.</param>
        public PdfCoordinateMapper(PdfTemplateCalibrationModel calibration, double pdfPageHeight)
        {
            _calibration = calibration ?? throw new ArgumentNullException(nameof(calibration));
            _pdfPageHeight = pdfPageHeight;
        }

        /// <summary>
        /// Maps a time (minute of day) to a PDF X coordinate.
        /// </summary>
        /// <param name="minuteOfDay">The minute of day (0-1440).</param>
        /// <returns>The X coordinate in PDF space (points).</returns>
        public double MapMinuteToX(int minuteOfDay)
        {
            // Use visual calibration lines if available (preferred method)
            var verticalLines = _calibration.VisualLines
                .Where(l => l.Type == CalibrationLineType.Vertical && l.MinuteOfDay.HasValue)
                .OrderBy(l => l.MinuteOfDay!.Value)
                .ToList();

            if (verticalLines.Count >= 2)
            {
                // Find the two closest vertical lines around our target minute
                var before = verticalLines.LastOrDefault(l => l.MinuteOfDay!.Value <= minuteOfDay);
                var after = verticalLines.FirstOrDefault(l => l.MinuteOfDay!.Value > minuteOfDay);

                if (before != null && after != null)
                {
                    // Linear interpolation between the two lines
                    var t = (double)(minuteOfDay - before.MinuteOfDay!.Value) / 
                            (after.MinuteOfDay!.Value - before.MinuteOfDay!.Value);
                    return before.Position + t * (after.Position - before.Position);
                }
                else if (before != null)
                {
                    // After the last line - extrapolate
                    if (verticalLines.Count >= 2)
                    {
                        var secondLast = verticalLines[verticalLines.Count - 2];
                        var slope = (before.Position - secondLast.Position) / 
                                    (before.MinuteOfDay!.Value - secondLast.MinuteOfDay!.Value);
                        return before.Position + slope * (minuteOfDay - before.MinuteOfDay!.Value);
                    }
                }
                else if (after != null)
                {
                    // Before the first line - extrapolate
                    if (verticalLines.Count >= 2)
                    {
                        var second = verticalLines[1];
                        var slope = (second.Position - after.Position) / 
                                    (second.MinuteOfDay!.Value - after.MinuteOfDay!.Value);
                        return after.Position - slope * (after.MinuteOfDay!.Value - minuteOfDay);
                    }
                }
            }

            // Fallback: use legacy XStart/XEnd proportional mapping
            if (_calibration.XStart > 0 && _calibration.XEnd > _calibration.XStart)
            {
                var t = minuteOfDay / (double)MinutesPerDay;
                return _calibration.XStart + t * (_calibration.XEnd - _calibration.XStart);
            }

            // No calibration available - return a default position
            return DefaultXPosition;
        }

        /// <summary>
        /// Maps a line/roulement ID to a PDF Y coordinate.
        /// </summary>
        /// <param name="roulementId">The line identifier (e.g., "@1101").</param>
        /// <returns>The Y coordinate in PDF space (points, from bottom-left), or null if not found.</returns>
        public double? MapRoulementToY(string roulementId)
        {
            if (string.IsNullOrWhiteSpace(roulementId))
            {
                return null;
            }

            // First, try to find in horizontal calibration lines
            var horizontalLine = _calibration.VisualLines
                .FirstOrDefault(l => l.Type == CalibrationLineType.Horizontal && 
                                     string.Equals(l.Label, roulementId, StringComparison.OrdinalIgnoreCase));

            if (horizontalLine != null)
            {
                // Position is already in PDF coordinates (Y from bottom)
                return horizontalLine.Position;
            }

            // Fallback: use legacy row mapping
            var row = _calibration.Rows
                .FirstOrDefault(r => string.Equals(r.RoulementId, roulementId, StringComparison.OrdinalIgnoreCase));

            if (row != null)
            {
                // YCenter is in PDF coordinates (Y from bottom)
                return row.YCenter;
            }

            return null;
        }

        /// <summary>
        /// Gets the rectangle bounds for a locomotive placement in PDF coordinates.
        /// </summary>
        /// <param name="minuteOfDay">The minute of day for the placement.</param>
        /// <param name="roulementId">The line identifier.</param>
        /// <param name="rectWidth">The width of the rectangle in points (default: 46).</param>
        /// <param name="rectHeight">The height of the rectangle in points (default: 18).</param>
        /// <returns>A tuple with (X, Y, Width, Height) in PDF coordinates, or null if mapping fails.</returns>
        public (double X, double Y, double Width, double Height)? GetLocoRectangle(
            int minuteOfDay, 
            string roulementId, 
            double rectWidth = DefaultLocomotiveRectWidth, 
            double rectHeight = DefaultLocomotiveRectHeight)
        {
            var x = MapMinuteToX(minuteOfDay);
            var y = MapRoulementToY(roulementId);

            if (!y.HasValue)
            {
                return null;
            }

            // Center the rectangle on the coordinates
            return (
                X: x - rectWidth / 2,
                Y: y.Value - rectHeight / 2,
                Width: rectWidth,
                Height: rectHeight
            );
        }
    }
}
