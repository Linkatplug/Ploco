using System;

namespace Ploco.Pdf.Annotations
{
    /// <summary>
    /// Base class for all PDF annotations.
    /// Represents abstract annotation information independent of any PDF library.
    /// </summary>
    public abstract class PdfAnnotationBase
    {
        /// <summary>
        /// Page index (0-based) where the annotation should be placed.
        /// </summary>
        public int PageIndex { get; set; }

        /// <summary>
        /// X coordinate in PDF space (points).
        /// </summary>
        public double X { get; set; }

        /// <summary>
        /// Y coordinate in PDF space (points, measured from bottom-left).
        /// </summary>
        public double Y { get; set; }

        /// <summary>
        /// Width of the annotation in PDF points.
        /// </summary>
        public double Width { get; set; }

        /// <summary>
        /// Height of the annotation in PDF points.
        /// </summary>
        public double Height { get; set; }
    }
}
