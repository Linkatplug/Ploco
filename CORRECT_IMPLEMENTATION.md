# Placement Prévisionnel - Correct WPF Implementation

## ⚠️ Critical Correction

This implementation was corrected to follow the **proper WPF DataTrigger pattern** as specified in the requirements.

### ❌ Previous Incorrect Approach
- Replaced all `StatutToBrushConverter` bindings with a new `LocomotiveToBrushConverter`
- This broke the requirement: "Ne pas remplacer StatutToBrushConverter partout"

### ✅ Current Correct Approach
- **Preserved** all original `Background="{Binding Status, Converter={StaticResource StatutToBrushConverter}}"` bindings
- **Added** WPF Style with DataTriggers as visual overlay
- DataTriggers provide forecast colors (Blue/Green) when needed
- Original status colors work normally when not in forecast mode

## Implementation Details

### WPF Style with DataTriggers

```xml
<Style x:Key="LocomotiveBorderStyle" TargetType="Border">
    <!-- Original converter binding PRESERVED -->
    <Setter Property="Background" Value="{Binding Status, Converter={StaticResource StatutToBrushConverter}}"/>
    
    <!-- Padding, Margin, CornerRadius also in Style -->
    <Setter Property="Padding" Value="4"/>
    <Setter Property="Margin" Value="2"/>
    <Setter Property="CornerRadius" Value="4"/>
    
    <!-- DataTriggers as OVERLAY (higher priority) -->
    <Style.Triggers>
        <!-- Forecast origin: Blue -->
        <DataTrigger Binding="{Binding IsForecastOrigin}" Value="True">
            <Setter Property="Background" Value="Blue"/>
        </DataTrigger>
        <!-- Forecast ghost: Green -->
        <DataTrigger Binding="{Binding IsForecastGhost}" Value="True">
            <Setter Property="Background" Value="Green"/>
        </DataTrigger>
    </Style.Triggers>
</Style>
```

### Usage in Templates

All locomotive Border elements now use:
```xml
<Border Style="{StaticResource LocomotiveBorderStyle}"
        ContextMenu="{StaticResource LocomotiveTileContextMenu}"
        PreviewMouseRightButtonDown="LocomotiveItem_PreviewMouseRightButtonDown">
    <!-- Content -->
</Border>
```

### Color Priority

WPF evaluates in this order:
1. **DataTrigger for `IsForecastOrigin`** → If true, Background = Blue
2. **DataTrigger for `IsForecastGhost`** → If true, Background = Green
3. **Default Setter** → `StatutToBrushConverter` provides status-based color

Result:
- Normal loco (Ok status) → Green (from StatutToBrushConverter)
- Normal loco (HS status) → Red (from StatutToBrushConverter)
- Forecast origin → Blue (DataTrigger overrides)
- Forecast ghost → Green (DataTrigger overrides)

## Data Model

### Properties Added to `LocomotiveModel`

```csharp
public class LocomotiveModel : INotifyPropertyChanged
{
    // Forecast state properties
    private bool _isForecastOrigin;
    private int? _forecastTargetRollingLineTrackId;
    private bool _isForecastGhost;
    private int? _forecastSourceLocomotiveId;
    
    public bool IsForecastOrigin { get; set; }
    public int? ForecastTargetRollingLineTrackId { get; set; }
    public bool IsForecastGhost { get; set; }
    public int? ForecastSourceLocomotiveId { get; set; }
}
```

### Ghost ID Strategy

Ghosts use unique negative IDs to avoid conflicts:
```csharp
ghost.Id = -100000 - sourceLocomotive.Id;
ghost.ForecastSourceLocomotiveId = sourceLocomotive.Id;
```

Example:
- Source locomotive ID: 42
- Ghost ID: -100042
- ForecastSourceLocomotiveId: 42

## Business Logic

### Activation (Placement Prévisionnel)

```csharp
loco.IsForecastOrigin = true;
loco.ForecastTargetRollingLineTrackId = targetTrack.Id;

var ghost = new LocomotiveModel
{
    Id = -100000 - loco.Id,
    ForecastSourceLocomotiveId = loco.Id,
    IsForecastGhost = true,
    // ... copy other properties
};

targetTrack.Locomotives.Add(ghost); // Only in track, NOT in _locomotives
```

### Cancellation (Annuler)

```csharp
var ghost = targetTrack.Locomotives.FirstOrDefault(l => 
    l.IsForecastGhost && 
    l.ForecastSourceLocomotiveId == loco.Id);

if (ghost != null)
    targetTrack.Locomotives.Remove(ghost);

loco.IsForecastOrigin = false;
loco.ForecastTargetRollingLineTrackId = null;
// Color reverts automatically via DataTrigger
// NO Status manipulation needed!
```

### Validation (Valider)

```csharp
// Remove ghost
var ghost = targetTrack.Locomotives.FirstOrDefault(l => 
    l.IsForecastGhost && 
    l.ForecastSourceLocomotiveId == loco.Id);

if (ghost != null)
    targetTrack.Locomotives.Remove(ghost);

// Reset flags
loco.IsForecastOrigin = false;
loco.ForecastTargetRollingLineTrackId = null;

// Use existing mechanism to move
MoveLocomotiveToTrack(loco, targetTrack, 0);
```

## Persistence

Ghosts are filtered automatically in `PersistState()`:

```csharp
// Filter ghosts from locomotives collection (shouldn't be there anyway)
var locomotivesToSave = _locomotives.Where(l => !l.IsForecastGhost).ToList();

// Filter ghosts from track locomotives
foreach (var loco in track.Locomotives.Where(l => !l.IsForecastGhost))
{
    trackCopy.Locomotives.Add(loco);
}
```

## Key Differences from Previous Implementation

| Aspect | Previous (Incorrect) | Current (Correct) |
|--------|---------------------|-------------------|
| Color System | Replaced converter bindings | DataTriggers overlay |
| Ghost ID | Same as original | Unique negative ID |
| Property Name | `ForecastTargetLineId` | `ForecastTargetRollingLineTrackId` |
| Ghost Tracking | Number + SeriesId | `ForecastSourceLocomotiveId` |
| Status Handling | Saved `OriginalStatus` | Never touch Status |
| Color Revert | Restore from `OriginalStatus` | Automatic via DataTrigger |

## Verification

### Status Colors (Must Work)
```
❌ Before Forecast:
  HS loco → Red (StatutToBrushConverter)
  ManqueTraction → Orange (StatutToBrushConverter)
  Ok → Green (StatutToBrushConverter)

✅ During Forecast:
  Origin → Blue (DataTrigger overrides)
  Ghost → Green (DataTrigger overrides)

✅ After Cancel:
  HS loco → Red (reverts automatically)
  ManqueTraction → Orange (reverts automatically)
  Ok → Green (reverts automatically)
```

### No Status Manipulation
```csharp
// ❌ WRONG (old approach)
loco.OriginalStatus = loco.Status;
loco.Status = LocomotiveStatus.Ok; // Force green

// ✅ RIGHT (current approach)
loco.IsForecastOrigin = true; // DataTrigger shows blue
// Status unchanged!
```

## Conclusion

The implementation now correctly follows WPF best practices:
- Original color system untouched
- DataTriggers as visual overlay
- No business logic changes for visual effects
- Clean, maintainable, and extensible

This is the **correct and final implementation**.
