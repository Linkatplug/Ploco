namespace Ploco.Pdf.Annotations
{
    /// <summary>
    /// Represents a text note annotation.
    /// This is rendered as a Text annotation or FreeText annotation in the PDF.
    /// </summary>
    public class NoteAnnotation : PdfAnnotationBase
    {
        /// <summary>
        /// Note content text.
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Note title/subject.
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// Background color (RGB hex format).
        /// </summary>
        public string BackgroundColor { get; set; } = "#FFFF00";

        /// <summary>
        /// Text color (RGB hex format).
        /// </summary>
        public string TextColor { get; set; } = "#000000";

        /// <summary>
        /// Font size for the note text.
        /// </summary>
        public double FontSize { get; set; } = 10.0;
    }
}
