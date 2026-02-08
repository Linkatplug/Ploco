using System;
using System.Collections.Generic;

namespace Ploco.Models
{
    public class PdfDocumentModel
    {
        public int Id { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public DateTime DocumentDate { get; set; }
        public string TemplateHash { get; set; } = string.Empty;
        public int PageCount { get; set; }
    }

    public enum CalibrationLineType
    {
        Horizontal,  // Ligne de roulement
        Vertical     // Marqueur d'heure
    }

    public class PdfTemplateCalibrationModel
    {
        public int Id { get; set; }
        public string TemplateHash { get; set; } = string.Empty;
        public int PageIndex { get; set; }
        public double XStart { get; set; }
        public double XEnd { get; set; }
        public List<PdfTemplateRowMapping> Rows { get; set; } = new();
        public List<PdfCalibrationLine> VisualLines { get; set; } = new();
    }

    public class PdfCalibrationLine
    {
        public int Id { get; set; }
        public int CalibrationId { get; set; }
        public CalibrationLineType Type { get; set; }
        public double Position { get; set; }  // X pour vertical, Y pour horizontal (coordonnées PDF)
        public string Label { get; set; } = string.Empty;  // Ex: "06:00" ou "@1101"
        public int? MinuteOfDay { get; set; }  // Pour lignes verticales (heures)
    }

    public class PdfTemplateRowMapping
    {
        public int Id { get; set; }
        public int CalibrationId { get; set; }
        public string RoulementId { get; set; } = string.Empty;
        public double YCenter { get; set; }
    }

    public class PdfPlacementModel
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
    }
}
