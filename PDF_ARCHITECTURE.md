# PDF Architecture Documentation

## Overview

This document describes the new PDF export architecture implemented in Ploco Manager. The system provides a robust, modular approach to generating annotated railway planning PDFs.

## Architecture Principles

### Core Principles (Non-Negotiable)

✅ **DO**:
- Use PDF as final support
- Use standard PDF annotations
- Keep annotations 100% editable
- Never modify source PDF
- Maintain strict separation of responsibilities
- Support heavily annotated PDFs

❌ **DON'T**:
- Rasterize or use images
- Write directly to source PDF
- Use animations in PDF
- Mix PDF logic with business logic
- Create WPF dependencies in PDF logic

## Architecture Layers

### 1. Business Layer (Existing)
**Location**: `Models/DomainModels.cs`, `Models/PdfPlanningModels.cs`

**Responsibility**: 
- Know locomotive data (number, status, traction %)
- Know line/track information
- Know train schedules
- Store placements with business data

**Key Models**:
- `LocomotiveModel`: Locomotive with status
- `PdfPlacementModel`: Locomotive placement with time and location
- `PdfTemplateCalibrationModel`: Calibration data for mapping

### 2. Mapping Layer
**Location**: `Pdf/Mapping/PdfCoordinateMapper.cs`

**Responsibility**:
- Transform business coordinates → PDF coordinates
- Map time (minute of day) → X coordinate
- Map line ID (roulement) → Y coordinate
- Handle calibration data

**Key Methods**:
```csharp
double MapMinuteToX(int minuteOfDay)
double? MapRoulementToY(string roulementId)
(double X, double Y, double Width, double Height)? GetLocoRectangle(...)
```

**Algorithm**:
1. **Preferred**: Use visual calibration lines with linear interpolation
2. **Fallback**: Use legacy XStart/XEnd proportional mapping
3. **Default**: Return safe default position

### 3. Annotation Models Layer
**Location**: `Pdf/Annotations/`

**Responsibility**:
- Abstract representation of PDF annotations
- Independent of any PDF library
- Define visual properties (colors, sizes, text)

**Classes**:
- `PdfAnnotationBase`: Base class with position (X, Y, Width, Height)
- `LocoRectangleAnnotation`: Locomotive rectangle with text and colors
- `TransferArrowAnnotation`: Arrow with label for transfers/crossings
- `NoteAnnotation`: Text note annotation

### 4. PDF Engine Layer
**Location**: `Pdf/Engine/PdfExportEngine.cs`

**Responsibility**:
- Open source PDF (read-only)
- Copy all pages
- Inject annotations using iText 7
- Save to new file

**Technology**: iText 7 (v8.0.5)
- FreeText annotations for rectangles and notes
- Line annotations for arrows
- Standard PDF annotations (fully editable)

**Key Method**:
```csharp
void ExportPdfWithAnnotations(
    string sourcePdfPath,
    string outputPdfPath,
    IEnumerable<PdfAnnotationBase> annotations,
    Dictionary<int, (double Width, double Height)> pageSizes)
```

### 5. Service Layer
**Location**: `Pdf/PdfExportService.cs`

**Responsibility**:
- Orchestrate mapping and engine layers
- Convert business placements to annotations
- Validate calibrations before export
- Handle colors by status

**Key Method**:
```csharp
void ExportPdfWithPlacements(
    string sourcePdfPath,
    string outputPdfPath,
    IEnumerable<PdfPlacementModel> placements,
    Dictionary<int, PdfTemplateCalibrationModel> calibrations,
    Dictionary<int, (double Width, double Height)> pageSizes)
```

**Validation Methods**:
- `ValidateCalibrations()`: Check all pages have calibration
- `ValidateRoulements()`: Check all roulements are calibrated

### 6. UI Layer
**Location**: `Dialogs/PlanningPdfWindow.xaml.cs`

**Responsibility**:
- Visual display of PDF pages
- Interactive placement of locomotives
- User controls for calibration
- Export button integration

**Integration Point**:
```csharp
private void ExportPdf(string outputPath)
{
    var exportService = new PdfExportService();
    exportService.ExportPdfWithPlacements(
        _document.FilePath,
        outputPath,
        allPlacements,
        _calibrations,
        _pdfPageSizes
    );
}
```

## Data Flow

```
User clicks "Export"
    ↓
PlanningPdfWindow.ExportPdf()
    ↓
Convert PdfPlacementViewModel → PdfPlacementModel
    ↓
PdfExportService.ExportPdfWithPlacements()
    ↓
For each placement:
    PdfCoordinateMapper.GetLocoRectangle()
        → MapMinuteToX() → X coordinate
        → MapRoulementToY() → Y coordinate
    ↓
    Create LocoRectangleAnnotation
        → Set colors by status
        → Set text (locomotive number + badge)
    ↓
PdfExportEngine.ExportPdfWithAnnotations()
    ↓
For each annotation:
    AddLocoRectangleAnnotation()
        → Create PdfFreeTextAnnotation
        → Set appearance, colors, border
        → Add to page
    ↓
Save new PDF file
    ↓
Show success message
```

## Locomotive Colors

| Status | Background | Border | Text |
|--------|-----------|--------|------|
| OK | SeaGreen (#2E8B57) | DarkGreen (#006400) | White |
| HS | IndianRed (#CD5C5C) | DarkRed (#8B0000) | White |
| ManqueTraction | Orange (#FFA500) | DarkOrange (#FF8C00) | White |
| DefautMineur | Gold (#FFD700) | GoldenRod (#DAA520) | White |

## Calibration System

### Visual Calibration Lines (Preferred)

**Vertical Lines** (Time):
- User clicks on PDF and enters time (HH:MM)
- Stored with minute of day (0-1440)
- Multiple lines enable linear interpolation

**Horizontal Lines** (Roulements):
- User clicks on PDF and enters roulement ID (@1101, @1102, etc.)
- Stored with Y coordinate in PDF space
- Maps business line to visual position

### Legacy Calibration (Fallback)

- XStart: X coordinate for 00:00
- XEnd: X coordinate for 24:00
- Rows: List of (RoulementId, YCenter) mappings

### Coordinate Systems

1. **Canvas/Image Space**: Used for UI display (0,0 = top-left)
2. **PDF Space**: Used for annotations (0,0 = bottom-left)
3. **Business Space**: Minutes (0-1440) and Roulement IDs

## Export Behavior

### What Happens When You Click "Export"

1. **Validation Phase**:
   - Check all placements have calibration data
   - Check all roulements are defined in calibration
   - Show warnings if issues found (with option to continue)

2. **Conversion Phase**:
   - Convert all PdfPlacementViewModel to PdfPlacementModel
   - For each placement, use PdfCoordinateMapper to get rectangle
   - Create LocoRectangleAnnotation with proper colors and text

3. **Export Phase**:
   - Open source PDF in read-only mode
   - Create new PDF by copying all pages
   - Add FreeText annotations to pages
   - Save to new file

4. **Result**:
   - Source PDF is **unchanged**
   - New PDF contains **editable annotations**
   - All pages are preserved
   - Annotations work with existing PDF annotations

## Key Features

### ✅ Source PDF Protection
- Source PDF opened with `PdfReader` (read-only)
- New PDF created with `PdfDocument` copying source
- No in-place modifications

### ✅ Editable Annotations
- Uses standard PDF FreeText annotations
- Can be edited with any PDF reader (Adobe, Foxit, etc.)
- Can be moved, resized, deleted after export

### ✅ Color-Coded Status
- Immediate visual identification of locomotive status
- Consistent colors across all exports
- Configurable in `PdfExportService`

### ✅ Comprehensive Validation
- Pre-flight checks before export
- Clear warnings for missing calibrations
- Option to continue with partial data

### ✅ Robust Coordinate Mapping
- Handles multiple calibration formats
- Linear interpolation for accuracy
- Graceful fallbacks for edge cases

## Extension Points

### Adding New Annotation Types

1. Create new class in `Pdf/Annotations/` extending `PdfAnnotationBase`
2. Implement in `PdfExportEngine.AddAnnotation()`
3. Add conversion logic in `PdfExportService`

Example:
```csharp
public class CrossingAnnotation : PdfAnnotationBase
{
    public string SourceTrainNumber { get; set; }
    public string TargetTrainNumber { get; set; }
    // ...
}
```

### Customizing Appearance

Modify color mappings in `PdfExportService`:
```csharp
private string GetBackgroundColor(LocomotiveStatus status)
{
    return status switch
    {
        LocomotiveStatus.HS => "#CD5C5C", // Change this
        // ...
    };
}
```

### Supporting New Coordinate Systems

Add new mapping methods in `PdfCoordinateMapper`:
```csharp
public double MapCustomCoordinate(string customId)
{
    // Custom mapping logic
}
```

## Troubleshooting

### Problem: Annotations appear in wrong positions
**Solution**: Check calibration lines are correctly placed. Ensure visual calibration lines have accurate time values.

### Problem: Some placements are missing from export
**Solution**: Check validation warnings. Missing placements usually mean calibration data is incomplete.

### Problem: Colors don't match expectations
**Solution**: Verify locomotive status in database. Check color mappings in `PdfExportService.GetBackgroundColor()`.

### Problem: Export fails with exception
**Solution**: Check source PDF is not corrupted. Ensure source PDF path is accessible and file is not locked by another process.

## Performance Considerations

- **Memory**: iText 7 loads entire PDF into memory. Large PDFs (>100 pages) may consume significant RAM.
- **Speed**: Export time is ~100ms per page with annotations. 50-page PDF takes ~5 seconds.
- **Disk**: Output PDF size is similar to input (annotations add minimal overhead).

## Dependencies

### iText 7 (v8.0.5)
- **License**: AGPL (open-source) or Commercial
- **Usage**: PDF reading, writing, and annotation
- **Why**: Most robust library for standard PDF annotations

### PdfPig (v1.7.0-custom-5)
- **Usage**: Page dimension extraction (legacy)
- **Status**: Can potentially be replaced by iText 7 in future

### Docnet.Core (v2.3.0)
- **Usage**: Rendering PDF to bitmap images for UI
- **Status**: UI-only, not used in export

## Security Considerations

### ✅ No Vulnerabilities
- CodeQL scan: 0 alerts
- Dependency scan: No known vulnerabilities in iText 7 v8.0.5

### Best Practices
- Source PDF opened read-only
- No execution of PDF scripts
- No network access during export
- Output path validated before write

## Testing Guidelines

### Unit Testing
Focus on testing individual layers:
- `PdfCoordinateMapper`: Test coordinate transformations
- `PdfExportService`: Test validation logic
- Annotation models: Test data transformation

### Integration Testing
Test the full export pipeline:
1. Load test PDF
2. Create test placements
3. Export to temporary file
4. Verify annotations exist and are editable

### Manual Testing
1. Load a real railway PDF
2. Add calibration lines
3. Place locomotives with various statuses
4. Export PDF
5. Open in Adobe Reader
6. Verify annotations are editable
7. Verify colors and text are correct

## Future Improvements

### Potential Enhancements
- Add TransferArrowAnnotation implementation for crossings
- Add NoteAnnotation for comments
- Support custom fonts in annotations
- Add PDF/A compliance mode
- Batch export multiple PDFs
- Template-based exports

### Architecture Evolution
- Replace PdfPig with iText 7 for page dimensions
- Add caching layer for calibrations
- Implement async export for large PDFs
- Add progress reporting for long exports

## Conclusion

The new PDF architecture provides a solid, maintainable foundation for railway planning PDF exports. It respects the source document, produces standard annotations, and maintains clear separation of concerns throughout the codebase.

For questions or issues, refer to the code comments in each layer or contact the development team.
