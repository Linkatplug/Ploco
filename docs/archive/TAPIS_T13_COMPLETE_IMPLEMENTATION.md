# TapisT13 - Implémentation Complète avec Support Forecast/Ghost

## Vue d'ensemble

Cette implémentation respecte strictement les exigences :
- ✅ Aucune nouvelle règle métier inventée
- ✅ Utilise uniquement les propriétés existantes
- ✅ Support complet du mode forecast (placement prévisionnel)
- ✅ Gestion correcte des différents types de voies (Line, RollingLine, Depot)
- ✅ Cohérence avec la logique HS existante

## Concept Clé : Track Effectif

### Principe
Le "track effectif" est le track à utiliser pour calculer l'affichage :

**Si locomotive en mode prévisionnel (`IsForecastOrigin = true`):**
- La locomotive est bleue dans sa tuile d'origine
- Elle a un ghost (copie verte) sur une ligne de roulement
- → Le track effectif est celui du GHOST (là où elle sera)

**Sinon:**
- → Le track effectif est le track réel (position actuelle)

### Implémentation

```csharp
private static TrackModel? GetEffectiveTrack(
    LocomotiveModel loco, 
    List<TrackModel> tracks, 
    List<LocomotiveModel> allLocomotives)
{
    // Mode prévisionnel : utiliser le track du ghost
    if (loco.IsForecastOrigin && loco.ForecastTargetRollingLineTrackId.HasValue)
    {
        var ghost = allLocomotives.FirstOrDefault(l => 
            l.IsForecastGhost && 
            l.ForecastSourceLocomotiveId == loco.Id);
        
        if (ghost != null)
        {
            var ghostTrack = tracks.FirstOrDefault(t => t.Locomotives.Contains(ghost));
            if (ghostTrack != null)
            {
                return ghostTrack;
            }
        }
    }
    
    // Sinon : track réel
    return tracks.FirstOrDefault(t => t.Locomotives.Contains(loco));
}
```

## Règles d'Affichage

### 1️⃣ Locomotive HS (Rouge) - INCHANGÉ

**Conditions:**
- `Status == HS`

**Texte:**
- Si sur Line avec train : `"NomTuile NumeroTrain"`
- Sinon : `"NomTuile"`

**Affichage:**
- Colonne "Loc HS" : texte en rouge
- Colonne "Infos/Rapport" : même texte en rouge

**Exemple:**
- Loco 1347 (HS) sur FIZ train 41836 → 🔴 "FIZ 41836"

### 2️⃣ Locomotive Non-HS sur Line avec Train (Vert)

**Conditions:**
- `Status != HS`
- `effectiveTrack.Kind == TrackKind.Line`
- `effectiveTrack.IsOnTrain == true`

**Texte:**
- EXACTEMENT le même que pour HS : `"NomTuile NumeroTrain"`

**Affichage:**
- Colonne "Loc HS" : vide
- Colonne "Infos/Rapport" : texte en vert

**Exemple:**
- Loco 1334 (OK) sur FIZ train 41836 → 🟢 "FIZ 41836"

**⚠️ Important:** Le vert dépend des CONDITIONS, pas du contenu du texte.

### 3️⃣ Locomotive OK en Dépôt/Garage

**Conditions:**
- `Status == Ok`
- `effectiveTrack.Kind != Line`
- `effectiveTrack.Kind != RollingLine`

**Texte:**
- `"DISPO NomTuile"`

**Affichage:**
- Colonne "Loc HS" : vide
- Colonne "Infos/Rapport" : texte sans couleur

**Exemple:**
- Loco 1335 (OK) dans dépôt FIZ → "DISPO FIZ" (pas de couleur)

### 4️⃣ Ligne de Roulement (Sans Couleur)

**Conditions:**
- `effectiveTrack.Kind == RollingLine`

**Texte:**
- UNIQUEMENT le numéro de ligne : `"1103"`, `"1113"`, etc.

**Affichage:**
- Colonne "Loc HS" : vide
- Colonne "Infos/Rapport" : numéro sans couleur

**Exemple:**
- Loco 1336 (OK) sur rolling line 1103 → "1103" (pas de couleur)

**⚠️ Important:** 
- Jamais le texte "Ligne de roulement"
- Jamais de couleur verte ici

## Code Métier

### GetTrainLocationText

```csharp
private static string GetTrainLocationText(
    LocomotiveModel loco, 
    TrackModel? track, 
    IEnumerable<TileModel> tiles)
{
    if (track == null) return string.Empty;

    var location = ResolveLocation(track, tiles);
    
    // Line avec train : "TileName TrainNumber"
    if (track.Kind == TrackKind.Line && 
        track.IsOnTrain && 
        !string.IsNullOrWhiteSpace(track.TrainNumber))
    {
        return $"{location} {track.TrainNumber}";
    }
    
    // OK en dépôt/garage : "DISPO TileName"
    if (loco.Status == LocomotiveStatus.Ok && 
        track.Kind != TrackKind.Line && 
        track.Kind != TrackKind.RollingLine)
    {
        return $"DISPO {location}";
    }

    // Sinon : juste le nom
    return location;
}
```

### LoadRows - Logique Principale

```csharp
// Track effectif (considère le mode forecast)
var effectiveTrack = GetEffectiveTrack(loco, tracks, allLocomotives);

// Texte calculé
var trainLocationText = GetTrainLocationText(loco, effectiveTrack, tiles);
var rollingLineNumber = ResolveRollingLineNumber(effectiveTrack);

var isHs = loco.Status == LocomotiveStatus.HS;

// Condition VERT : Non-HS + Line + avec train
var isNonHsOnLine = !isHs 
    && effectiveTrack?.Kind == TrackKind.Line 
    && effectiveTrack.IsOnTrain == true;

// Colonne "Loc HS" : uniquement pour HS
var locHs = isHs ? trainLocationText : string.Empty;

// Colonne "Infos/Rapport"
var report = isHs ? trainLocationText 
    : isNonHsOnLine ? trainLocationText
    : !string.IsNullOrWhiteSpace(rollingLineNumber) ? rollingLineNumber
    : trainLocationText;
```

## Exemples Complets

### Cas Normal (Sans Forecast)

| Loco | Status | Track | Text | Loc HS | Infos/Rapport |
|------|--------|-------|------|--------|---------------|
| 1347 | HS | FIZ Line train 41836 | "FIZ 41836" | 🔴 FIZ 41836 | 🔴 FIZ 41836 |
| 1334 | OK | FIZ Line train 41836 | "FIZ 41836" | (vide) | 🟢 FIZ 41836 |
| 1335 | OK | FIZ Depot | "DISPO FIZ" | (vide) | DISPO FIZ |
| 1336 | OK | Rolling line 1103 | "1103" | (vide) | 1103 |
| 1337 | ManqueTraction | MUN Line train 42350 | "MUN 42350" | (vide) | 🟢 MUN 42350 |

### Cas Forecast (Placement Prévisionnel)

**Scénario:** Loco 1338 (OK) est en mode prévisionnel
- Position réelle : Dépôt FIZ
- Ghost : Sur FIZ Line train 41836
- `IsForecastOrigin = true`

**Résultat:**
- Track effectif = track du ghost (FIZ Line train 41836)
- Texte = "FIZ 41836"
- Affichage = 🟢 "FIZ 41836" dans Infos/Rapport

| Loco | IsForecastOrigin | Track Réel | Ghost Track | Effective Track | Text | Infos/Rapport |
|------|------------------|------------|-------------|-----------------|------|---------------|
| 1338 | true | FIZ Depot | FIZ Line 41836 | FIZ Line 41836 | "FIZ 41836" | 🟢 FIZ 41836 |

## XAML - Styles

### HsCellStyle
```xml
<Style x:Key="HsCellStyle" TargetType="DataGridCell">
    <Style.Triggers>
        <DataTrigger Binding="{Binding IsHs}" Value="True">
            <Setter Property="Background" Value="#FFD32F2F"/>
            <Setter Property="Foreground" Value="White"/>
        </DataTrigger>
    </Style.Triggers>
</Style>
```

### ReportCellStyle
```xml
<Style x:Key="ReportCellStyle" TargetType="DataGridCell">
    <Style.Triggers>
        <!-- Rouge pour HS -->
        <DataTrigger Binding="{Binding IsHs}" Value="True">
            <Setter Property="Background" Value="#FFD32F2F"/>
            <Setter Property="Foreground" Value="White"/>
        </DataTrigger>
        <!-- Vert pour Non-HS sur Line avec train -->
        <DataTrigger Binding="{Binding IsNonHsOnLine}" Value="True">
            <Setter Property="Background" Value="#FF4CAF50"/>
            <Setter Property="Foreground" Value="White"/>
        </DataTrigger>
    </Style.Triggers>
</Style>
```

## Propriétés du Modèle T13Row

```csharp
private sealed class T13Row
{
    public string Locomotive { get; set; }      // Numéro loco
    public string MaintenanceDate { get; set; } // Date entretien
    public string Motif { get; set; }           // Motif HS
    public string LocHs { get; set; }           // Colonne Loc HS
    public string Report { get; set; }          // Colonne Infos/Rapport
    public bool IsHs { get; set; }              // Pour style rouge
    public bool IsNonHsOnLine { get; set; }     // Pour style vert
}
```

## Points de Vigilance

### ✅ À Faire
- Toujours calculer le track effectif AVANT tout autre calcul
- Baser le vert sur les CONDITIONS (Kind, IsOnTrain), pas sur le texte
- Utiliser les propriétés existantes uniquement
- Pour OK en dépôt : ajouter "DISPO"
- Pour rolling line : afficher seulement le numéro

### ❌ À Éviter
- Ne PAS inventer de nouvelles propriétés métier
- Ne PAS appliquer le vert sur les rolling lines
- Ne PAS afficher "Ligne de roulement" en texte
- Ne PAS baser le vert sur le contenu du texte retourné

## Test de Validation

Pour valider l'implémentation, tester :

1. **HS sur train** → Rouge dans Loc HS + Infos/Rapport
2. **Non-HS sur Line avec train** → Vert dans Infos/Rapport uniquement
3. **OK en dépôt** → "DISPO TileName" sans couleur
4. **Sur rolling line** → Juste le numéro, sans couleur
5. **Forecast mode** → Utilise le track du ghost pour tous les calculs

## Conclusion

Cette implémentation :
- ✅ Respecte toutes les exigences
- ✅ N'invente aucune nouvelle règle métier
- ✅ Réutilise les propriétés existantes
- ✅ Gère correctement le mode forecast
- ✅ Différencie les types de tracks
- ✅ Cohérente avec la logique HS existante

Build: ✅ 0 warnings, 0 errors
