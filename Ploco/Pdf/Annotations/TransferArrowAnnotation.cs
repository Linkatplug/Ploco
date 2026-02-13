namespace Ploco.Pdf.Annotations
{
    /// <summary>
    /// Represents a transfer/crossing annotation with an arrow and label.
    /// This is rendered as a Line annotation with an associated Text annotation.
    /// </summary>
    public class TransferArrowAnnotation : PdfAnnotationBase
    {
        /// <summary>
        /// Starting X coordinate of the arrow.
        /// </summary>
        public double StartX { get; set; }

        /// <summary>
        /// Starting Y coordinate of the arrow.
        /// </summary>
        public double StartY { get; set; }

        /// <summary>
        /// Ending X coordinate of the arrow.
        /// </summary>
        public double EndX { get; set; }

        /// <summary>
        /// Ending Y coordinate of the arrow.
        /// </summary>
        public double EndY { get; set; }

        /// <summary>
        /// Label text to display near the arrow (e.g., "+1114").
        /// </summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>
        /// Arrow color (RGB hex format).
        /// </summary>
        public string ArrowColor { get; set; } = "#0000FF";

        /// <summary>
        /// Arrow line width in points.
        /// </summary>
        public double LineWidth { get; set; } = 1.5;

        /// <summary>
        /// Font size for the label.
        /// </summary>
        public double LabelFontSize { get; set; } = 9.0;
    }
}
