using Ploco.Models;
using System;

namespace Ploco.Pdf.Annotations
{
    /// <summary>
    /// Represents a locomotive placement annotation as a rectangle with text.
    /// This is rendered as a FreeText annotation in the PDF.
    /// </summary>
    public class LocoRectangleAnnotation : PdfAnnotationBase
    {
        /// <summary>
        /// Locomotive number to display.
        /// </summary>
        public int LocomotiveNumber { get; set; }

        /// <summary>
        /// Locomotive status (affects color and badges).
        /// </summary>
        public LocomotiveStatus Status { get; set; }

        /// <summary>
        /// Traction percentage for ManqueTraction status.
        /// </summary>
        public int? TractionPercent { get; set; }

        /// <summary>
        /// Whether the locomotive is on a train.
        /// </summary>
        public bool OnTrain { get; set; }

        /// <summary>
        /// Train number if on train.
        /// </summary>
        public string? TrainNumber { get; set; }

        /// <summary>
        /// Background color (RGB hex format, e.g., "#FFFFFF").
        /// </summary>
        public string BackgroundColor { get; set; } = "#FFFFFF";

        /// <summary>
        /// Border color (RGB hex format).
        /// </summary>
        public string BorderColor { get; set; } = "#FF0000";

        /// <summary>
        /// Text color (RGB hex format).
        /// </summary>
        public string TextColor { get; set; } = "#000000";

        /// <summary>
        /// Border width in points.
        /// </summary>
        public double BorderWidth { get; set; } = 1.0;

        /// <summary>
        /// Font size for the main text.
        /// </summary>
        public double FontSize { get; set; } = 10.0;

        /// <summary>
        /// Gets the display text for the annotation.
        /// </summary>
        public string GetDisplayText()
        {
            var text = LocomotiveNumber.ToString();
            
            if (Status == LocomotiveStatus.ManqueTraction && TractionPercent.HasValue)
            {
                text += $" {TractionPercent}%";
            }
            else if (Status == LocomotiveStatus.HS)
            {
                text += " HS";
            }
            
            return text;
        }
    }
}
