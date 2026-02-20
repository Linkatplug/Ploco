using System;
using System.Collections.Generic;
using Ploco.Models;

namespace Ploco.Data
{
    public interface IPlocoRepository
    {
        void Initialize();
        PdfDocumentModel? GetPdfDocument(string filePath, DateTime date);
        PdfDocumentModel SavePdfDocument(PdfDocumentModel document);
        List<PdfTemplateCalibrationModel> LoadTemplateCalibrations(string templateHash);
        void SaveTemplateCalibration(PdfTemplateCalibrationModel calibration);
        List<PdfPlacementModel> LoadPlacements(int pdfDocumentId);
        void SavePlacement(PdfPlacementModel placement);
        void DeletePlacement(int placementId);
        AppState LoadState();
        void SeedDefaultDataIfNeeded();
        void SaveState(AppState state);
        void AddHistory(string action, string details);
        List<HistoryEntry> LoadHistory();
        Dictionary<string, int> GetTableCounts();
        Dictionary<TrackKind, int> GetTrackKindCounts();
        void ClearHistory();
        void ResetOperationalState();
        void CopyDatabaseTo(string destinationPath);
        bool ReplaceDatabaseWith(string sourcePath);
    }
}
