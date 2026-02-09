using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Annot;
using iText.Kernel.Geom;
using iText.Kernel.Colors;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using Ploco.Pdf.Annotations;

namespace Ploco.Pdf.Engine
{
    /// <summary>
    /// PDF export engine using iText 7.
    /// Responsible for opening source PDFs, copying pages, injecting annotations, and saving new files.
    /// This is the only class that directly interacts with the PDF library.
    /// </summary>
    public class PdfExportEngine : IDisposable
    {
        private bool _disposed;

        /// <summary>
        /// Exports a PDF with annotations without modifying the source.
        /// </summary>
        /// <param name="sourcePdfPath">Path to the source PDF file.</param>
        /// <param name="outputPdfPath">Path where the new PDF should be saved.</param>
        /// <param name="annotations">Collection of annotations to add to the PDF.</param>
        /// <param name="pageSizes">Dictionary of page sizes (page index -> (width, height) in points).</param>
        public void ExportPdfWithAnnotations(
            string sourcePdfPath,
            string outputPdfPath,
            IEnumerable<PdfAnnotationBase> annotations,
            Dictionary<int, (double Width, double Height)> pageSizes)
        {
            if (string.IsNullOrWhiteSpace(sourcePdfPath))
                throw new ArgumentException("Source PDF path cannot be empty.", nameof(sourcePdfPath));

            if (string.IsNullOrWhiteSpace(outputPdfPath))
                throw new ArgumentException("Output PDF path cannot be empty.", nameof(outputPdfPath));

            if (!File.Exists(sourcePdfPath))
                throw new FileNotFoundException("Source PDF file not found.", sourcePdfPath);

            // Open source PDF in read-only mode
            using var sourceReader = new PdfReader(sourcePdfPath);
            
            // Create new PDF writer
            using var writer = new PdfWriter(outputPdfPath);
            
            // Create new PDF document by copying source
            using var pdfDoc = new PdfDocument(sourceReader, writer);

            // Group annotations by page
            var annotationsByPage = annotations
                .GroupBy(a => a.PageIndex)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Add annotations to each page
            foreach (var pageIndex in annotationsByPage.Keys)
            {
                if (pageIndex < 0 || pageIndex >= pdfDoc.GetNumberOfPages())
                {
                    continue; // Skip invalid page indices
                }

                var page = pdfDoc.GetPage(pageIndex + 1); // iText uses 1-based indexing
                var pageAnnotations = annotationsByPage[pageIndex];

                foreach (var annotation in pageAnnotations)
                {
                    AddAnnotation(page, annotation, pageSizes);
                }
            }
        }

        /// <summary>
        /// Adds a single annotation to a PDF page.
        /// </summary>
        private void AddAnnotation(
            PdfPage page, 
            PdfAnnotationBase annotation,
            Dictionary<int, (double Width, double Height)> pageSizes)
        {
            switch (annotation)
            {
                case LocoRectangleAnnotation locoAnnotation:
                    AddLocoRectangleAnnotation(page, locoAnnotation);
                    break;
                    
                case TransferArrowAnnotation arrowAnnotation:
                    AddTransferArrowAnnotation(page, arrowAnnotation);
                    break;
                    
                case NoteAnnotation noteAnnotation:
                    AddNoteAnnotation(page, noteAnnotation);
                    break;
                    
                default:
                    throw new NotSupportedException($"Annotation type {annotation.GetType().Name} is not supported.");
            }
        }

        /// <summary>
        /// Adds a locomotive rectangle annotation (FreeText).
        /// </summary>
        private void AddLocoRectangleAnnotation(PdfPage page, LocoRectangleAnnotation annotation)
        {
            // Create rectangle for annotation bounds
            var rect = new Rectangle(
                (float)annotation.X,
                (float)annotation.Y,
                (float)annotation.Width,
                (float)annotation.Height
            );

            // Create FreeText annotation with contents
            var displayText = new PdfString(annotation.GetDisplayText());
            var freeText = new PdfFreeTextAnnotation(rect, displayText);
            
            // Set appearance properties
            freeText.SetDefaultAppearance(CreateDefaultAppearance(annotation.FontSize));
            
            // Set colors
            var bgColor = ParseColor(annotation.BackgroundColor);
            var borderColor = ParseColor(annotation.BorderColor);
            
            if (bgColor != null)
            {
                freeText.SetColor(bgColor.GetColorValue());
            }
            
            // Set border
            var border = new PdfAnnotationBorder(0, 0, (float)annotation.BorderWidth);
            freeText.SetBorder(border);
            
            if (borderColor != null)
            {
                // Border color is set via the C (color) entry
                freeText.Put(PdfName.C, new PdfArray(borderColor.GetColorValue()));
            }

            // Set flags to make it editable but visible
            freeText.SetFlag(PdfAnnotation.PRINT);
            
            // Add annotation to page
            page.AddAnnotation(freeText);
        }

        /// <summary>
        /// Adds a transfer arrow annotation (Line + Text).
        /// </summary>
        private void AddTransferArrowAnnotation(PdfPage page, TransferArrowAnnotation annotation)
        {
            // Create line annotation with coordinates as float array
            var lineCoords = new float[] 
            { 
                (float)annotation.StartX, (float)annotation.StartY,
                (float)annotation.EndX, (float)annotation.EndY 
            };
            
            var rect = new Rectangle(
                (float)Math.Min(annotation.StartX, annotation.EndX) - 10,
                (float)Math.Min(annotation.StartY, annotation.EndY) - 10,
                (float)Math.Abs(annotation.EndX - annotation.StartX) + 20,
                (float)Math.Abs(annotation.EndY - annotation.StartY) + 20
            );

            var lineAnnotation = new PdfLineAnnotation(rect, lineCoords);
            
            // Set line properties
            var arrowColor = ParseColor(annotation.ArrowColor);
            if (arrowColor != null)
            {
                lineAnnotation.SetColor(arrowColor.GetColorValue());
            }
            
            lineAnnotation.Put(PdfName.BS, new PdfDictionary());
            lineAnnotation.GetPdfObject().GetAsDictionary(PdfName.BS)?.Put(PdfName.W, new PdfNumber(annotation.LineWidth));
            
            // Set arrow style (arrow at end)
            lineAnnotation.SetLineEndingStyles(new PdfArray(new[] { PdfName.None, PdfName.OpenArrow }));
            
            lineAnnotation.SetFlag(PdfAnnotation.PRINT);
            page.AddAnnotation(lineAnnotation);

            // Add text label if provided
            if (!string.IsNullOrWhiteSpace(annotation.Label))
            {
                var labelX = (annotation.StartX + annotation.EndX) / 2;
                var labelY = (annotation.StartY + annotation.EndY) / 2;
                var labelRect = new Rectangle((float)labelX - 20, (float)labelY - 10, 40, 20);
                
                var labelText = new PdfString(annotation.Label);
                var textAnnotation = new PdfFreeTextAnnotation(labelRect, labelText);
                textAnnotation.SetDefaultAppearance(CreateDefaultAppearance(annotation.LabelFontSize));
                
                if (arrowColor != null)
                {
                    textAnnotation.SetColor(arrowColor.GetColorValue());
                }
                
                textAnnotation.SetFlag(PdfAnnotation.PRINT);
                page.AddAnnotation(textAnnotation);
            }
        }

        /// <summary>
        /// Adds a note annotation (Text or FreeText).
        /// </summary>
        private void AddNoteAnnotation(PdfPage page, NoteAnnotation annotation)
        {
            var rect = new Rectangle(
                (float)annotation.X,
                (float)annotation.Y,
                (float)annotation.Width,
                (float)annotation.Height
            );

            var noteContent = new PdfString(annotation.Content);
            var freeText = new PdfFreeTextAnnotation(rect, noteContent);
            freeText.SetDefaultAppearance(CreateDefaultAppearance(annotation.FontSize));
            
            var bgColor = ParseColor(annotation.BackgroundColor);
            if (bgColor != null)
            {
                freeText.SetColor(bgColor.GetColorValue());
            }
            
            if (!string.IsNullOrWhiteSpace(annotation.Title))
            {
                freeText.SetTitle(new PdfString(annotation.Title));
            }
            
            freeText.SetFlag(PdfAnnotation.PRINT);
            page.AddAnnotation(freeText);
        }

        /// <summary>
        /// Creates a default appearance string for FreeText annotations.
        /// This uses the PDF standard format for text appearance:
        /// - /Helv: Helvetica font (standard PDF font)
        /// - Tf: Set font and size operator
        /// - rg: Set RGB color operator (0 0 0 = black)
        /// Format: "/FontName fontSize Tf r g b rg"
        /// </summary>
        /// <param name="fontSize">The font size in points.</param>
        /// <returns>A PdfString containing the appearance definition.</returns>
        private PdfString CreateDefaultAppearance(double fontSize)
        {
            // PostScript-like format for PDF text appearance
            // Tf = set font and size, rg = RGB color (0 0 0 = black)
            return new PdfString($"/Helv {fontSize} Tf 0 0 0 rg");
        }

        /// <summary>
        /// Parses a hex color string to iText Color.
        /// </summary>
        /// <param name="hexColor">Hex color string (e.g., "#FF0000").</param>
        /// <returns>iText Color object, or null if parsing fails.</returns>
        private Color? ParseColor(string hexColor)
        {
            if (string.IsNullOrWhiteSpace(hexColor) || !hexColor.StartsWith("#"))
            {
                return null;
            }

            try
            {
                var hex = hexColor.TrimStart('#');
                if (hex.Length == 6)
                {
                    var r = Convert.ToInt32(hex.Substring(0, 2), 16) / 255f;
                    var g = Convert.ToInt32(hex.Substring(2, 2), 16) / 255f;
                    var b = Convert.ToInt32(hex.Substring(4, 2), 16) / 255f;
                    return new DeviceRgb(r, g, b);
                }
            }
            catch
            {
                // Invalid color format - return null
            }

            return null;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _disposed = true;
            }
        }
    }
}
