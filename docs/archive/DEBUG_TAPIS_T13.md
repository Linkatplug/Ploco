# Debug: TapisT13 Report Field Issue

## Problem
User reports that the "Infos/Rapport" column is still empty for non-HS locomotives on rolling lines with train assignment.

## Example
- Tile: FIZ (rolling line)
- Train: 41836
- Loco 1347 (HS): Shows "FIZ 41836" in red ✅
- Loco 1334 (OK): Should show "FIZ 41836" in green but shows empty ❌

## Code Logic (Should Work)

### Current Implementation
```csharp
var isHs = loco.Status == LocomotiveStatus.HS;
var isOnRollingLine = track?.Kind == TrackKind.RollingLine;
var isNonHsOnRollingLine = isOnRollingLine && !isHs && track?.IsOnTrain == true;

var trainInfo = track?.IsOnTrain == true
    ? string.IsNullOrWhiteSpace(track.TrainNumber)
        ? location
        : $"{location} {track.TrainNumber}"
    : location;

var report = isHs ? trainInfo 
    : isNonHsOnRollingLine ? trainInfo
    : !string.IsNullOrWhiteSpace(rollingLineNumber) ? rollingLineNumber
    : string.Empty;
```

### Expected Flow for Loco 1334 (OK) on FIZ train 41836
1. `track.Kind` = `TrackKind.RollingLine` ✓
2. `track.IsOnTrain` = `true` ✓
3. `track.TrainNumber` = `"41836"` ✓
4. `location` = `"FIZ"` (from GetLocationAbbreviation("Muizen"))
5. `trainInfo` = `"FIZ 41836"` ✓
6. `isHs` = `false` (Status = Ok) ✓
7. `isOnRollingLine` = `true` ✓
8. `isNonHsOnRollingLine` = `true` (rolling line AND not HS AND IsOnTrain) ✓
9. `report` = `trainInfo` = `"FIZ 41836"` ✓

## Debug Logging Added

Added comprehensive logging to track each step:

```csharp
Logger.Info($"Processing loco {loco.Number}: Status={loco.Status}, Pool={loco.Pool}", "TapisT13");
Logger.Info($"  Track: Name={track.Name}, Kind={track.Kind}, IsOnTrain={track.IsOnTrain}, TrainNumber={track.TrainNumber ?? "null"}", "TapisT13");
Logger.Info($"  Location: {location}, RollingLineNumber: {rollingLineNumber}", "TapisT13");
Logger.Info($"  TrainInfo: {trainInfo}", "TapisT13");
Logger.Info($"  Flags: isHs={isHs}, isOnRollingLine={isOnRollingLine}, isNonHsOnRollingLine={isNonHsOnRollingLine}", "TapisT13");
Logger.Info($"  Results: LocHs='{locHs}', Report='{report}'", "TapisT13");
```

## How to Debug

1. Run the application
2. Open TapisT13 window (click on "Tapis T13" button)
3. Check the logs at: `%AppData%\Ploco\Logs\Ploco_YYYY-MM-DD_HH-mm-ss.log`
4. Search for `[TapisT13]` entries
5. Find the entry for loco 1334 (or whichever non-HS loco is on a train)
6. Check the logged values to see where the logic breaks

## Expected Log Output
```
[INFO   ] [TapisT13] Processing loco 1334: Status=Ok, Pool=Sibelit
[INFO   ] [TapisT13]   Track: Name=1103, Kind=RollingLine, IsOnTrain=True, TrainNumber=41836
[INFO   ] [TapisT13]   Location: FIZ, RollingLineNumber: 1103
[INFO   ] [TapisT13]   TrainInfo: FIZ 41836
[INFO   ] [TapisT13]   Flags: isHs=False, isOnRollingLine=True, isNonHsOnRollingLine=True
[INFO   ] [TapisT13]   Results: LocHs='', Report='FIZ 41836'
```

## Possible Issues to Check

### 1. Track.IsOnTrain is False
If `track.IsOnTrain` is `false`, then:
- `isNonHsOnRollingLine` = `false`
- `report` falls through to `rollingLineNumber` or empty

**Fix**: Ensure the track has `IsOnTrain = true` when a locomotive is assigned to a train.

### 2. Location Resolution Fails
If `location` is empty:
- `trainInfo` might be just the train number
- Check `GetLocationAbbreviation()` for the tile name

**Fix**: Verify tile name mapping in `GetLocationAbbreviation()`.

### 3. TrainNumber is Empty
If `track.TrainNumber` is empty but `IsOnTrain` is true:
- `trainInfo` = location only (no train number)

**Fix**: Ensure `TrainNumber` is set when `IsOnTrain = true`.

## Next Steps

1. User runs the application and opens TapisT13 window
2. User checks the logs and shares the output for loco 1334
3. Based on the logs, we can identify exactly where the issue is
4. Implement targeted fix based on findings
