# TapisT13 - Refactorisation : Réutilisation de la Logique HS

## Vue d'ensemble

Cette refactorisation extrait la logique existante de calcul du texte de localisation train (utilisée pour les locomotives HS) dans une méthode helper réutilisable. Les locomotives non-HS utilisent maintenant **exactement la même logique** que les locomotives HS.

## Principe

**Règle fondamentale :** Ne pas inventer de nouvelles règles, réutiliser ce qui fonctionne déjà pour HS.

## Architecture

### Méthode Helper : `GetTrainLocationText`

```csharp
/// <summary>
/// Gets the train location text for a locomotive using the EXISTING HS logic.
/// Returns "TileName" or "TileName TrainNumber" depending on track.IsOnTrain.
/// </summary>
private static string GetTrainLocationText(LocomotiveModel loco, TrackModel? track, IEnumerable<TileModel> tiles)
{
    if (track == null)
    {
        return string.Empty;
    }

    var location = ResolveLocation(track, tiles);
    
    // EXISTING HS logic: if track.IsOnTrain, include train number
    if (track.IsOnTrain && !string.IsNullOrWhiteSpace(track.TrainNumber))
    {
        return $"{location} {track.TrainNumber}";
    }

    return location;
}
```

### Utilisation dans LoadRows

```csharp
// Appel unique pour tous les types de locomotives
var trainLocationText = GetTrainLocationText(loco, track, tiles);
var hasTrainInfo = !string.IsNullOrWhiteSpace(trainLocationText);

// HS : affiche dans "Loc HS" (rouge)
var locHs = isHs ? trainLocationText : string.Empty;

// Non-HS avec info train : affiche dans "Infos/Rapport" (vert)
// Sinon : affiche numéro de ligne de roulement ou vide
var report = isHs ? trainLocationText 
    : hasTrainInfo ? trainLocationText
    : rollingLineNumber ?? string.Empty;
```

## Logique de Détection

### Propriété : `HasTrainInfo`

Indique si la locomotive a des informations de localisation train à afficher :

```csharp
var hasTrainInfo = !string.IsNullOrWhiteSpace(trainLocationText);
```

Cette propriété est `true` si :
- La locomotive est sur un track
- Le track est sur une tile "En ligne" (rolling line)
- `track.IsOnTrain == true` (logique EXISTANTE)
- Un texte de localisation a été calculé

### T13Row Model

```csharp
private sealed class T13Row
{
    public string Locomotive { get; set; } = string.Empty;
    public string MaintenanceDate { get; set; } = string.Empty;
    public string Motif { get; set; } = string.Empty;
    public string LocHs { get; set; } = string.Empty;
    public string Report { get; set; } = string.Empty;
    public bool IsHs { get; set; }
    public bool HasTrainInfo { get; set; }  // Pour affichage vert
}
```

## Style XAML

```xml
<Style x:Key="ReportCellStyle" TargetType="DataGridCell">
    <Style.Triggers>
        <!-- HS : Rouge -->
        <DataTrigger Binding="{Binding IsHs}" Value="True">
            <Setter Property="Background" Value="#FFD32F2F"/>
            <Setter Property="Foreground" Value="White"/>
        </DataTrigger>
        <!-- Non-HS avec info train : Vert -->
        <DataTrigger Binding="{Binding HasTrainInfo}" Value="True">
            <Setter Property="Background" Value="#FF4CAF50"/>
            <Setter Property="Foreground" Value="White"/>
        </DataTrigger>
    </Style.Triggers>
</Style>
```

## Flux de Données

### Pour une locomotive HS sur FIZ, train 41836

1. `GetTrainLocationText` → retourne `"FIZ 41836"`
2. `hasTrainInfo` → `true` (texte non vide)
3. `locHs` → `"FIZ 41836"` (car `isHs = true`)
4. `report` → `"FIZ 41836"` (car `isHs = true`)
5. `IsHs` → `true` dans T13Row
6. `HasTrainInfo` → `false` dans T13Row (car HS, pas affiché en vert)
7. Affichage : **Rouge** dans "Loc HS" ET "Infos/Rapport"

### Pour une locomotive OK sur FIZ, train 41836

1. `GetTrainLocationText` → retourne `"FIZ 41836"` (même logique !)
2. `hasTrainInfo` → `true` (texte non vide)
3. `locHs` → `""` (car `isHs = false`)
4. `report` → `"FIZ 41836"` (car `hasTrainInfo = true`)
5. `IsHs` → `false` dans T13Row
6. `HasTrainInfo` → `true` dans T13Row (car non-HS avec info train)
7. Affichage : **Vert** dans "Infos/Rapport", vide dans "Loc HS"

### Pour une locomotive OK sur FIZ, SANS train

1. `GetTrainLocationText` → retourne `"FIZ"` (pas de train number)
2. `hasTrainInfo` → `true` (texte non vide : juste le nom de la tuile)
3. `locHs` → `""` (car `isHs = false`)
4. `report` → `"FIZ"` (car `hasTrainInfo = true`)
5. `IsHs` → `false` dans T13Row
6. `HasTrainInfo` → `true` dans T13Row
7. Affichage : **Vert** dans "Infos/Rapport"

**Remarque :** Dans le cas où `track.IsOnTrain = false`, la méthode `GetTrainLocationText` retourne juste le nom de la tuile sans numéro de train.

### Pour une locomotive OK sur ligne de roulement, track.IsOnTrain = false

1. `GetTrainLocationText` → retourne `"FIZ"` (location seule, pas de train)
2. `hasTrainInfo` → `true` (texte non vide)
3. `locHs` → `""` (car `isHs = false`)
4. `report` → `"FIZ"` (car `hasTrainInfo = true`)
5. Affichage : **Vert** dans "Infos/Rapport"

## Avantages de cette Approche

1. **Réutilisation du code** : Une seule méthode pour calculer le texte de localisation
2. **Cohérence** : HS et non-HS utilisent la même logique
3. **Maintenabilité** : Modifications futures dans un seul endroit
4. **Clarté** : Le nom `GetTrainLocationText` indique clairement le rôle
5. **Fiabilité** : Aucune nouvelle règle inventée, on réutilise ce qui marche

## Propriétés Utilisées (EXISTANTES)

- `track.IsOnTrain` : Flag indiquant si le track est affecté à un train
- `track.TrainNumber` : Numéro du train (ex: "41836")
- `track.Kind` : Type de track (RollingLine, Depot, etc.)
- `track.Name` : Nom du track (ex: "1103")
- `tile.Name` : Nom de la tuile (ex: "Muizen" → abrégé en "FIZ")

**Important :** Toutes ces propriétés existaient déjà et étaient utilisées pour la logique HS. Aucune nouvelle propriété n'a été inventée.

## Logs de Débogage

Les logs affichent maintenant :
```
[TapisT13] Processing loco 1334: Status=Ok, Pool=Sibelit
[TapisT13]   Track: Name=1103, Kind=RollingLine, IsOnTrain=True, TrainNumber=41836
[TapisT13]   TrainLocationText: 'FIZ 41836', RollingLineNumber: '1103'
[TapisT13]   Flags: isHs=False, hasTrainInfo=True
[TapisT13]   Results: LocHs='', Report='FIZ 41836'
```

## Test

### Cas de test attendu

**Tuile "FIZ" (En ligne), train 41836 :**

| Loco | Statut | Loc HS | Infos/Rapport |
|------|--------|--------|---------------|
| 1347 | HS | 🔴 FIZ 41836 | 🔴 FIZ 41836 |
| 1334 | OK | (vide) | 🟢 FIZ 41836 |

✅ La loco 1347 (HS) apparaît en rouge dans les deux colonnes
✅ La loco 1334 (OK) apparaît en vert uniquement dans "Infos/Rapport"
✅ Le texte est identique pour les deux : "FIZ 41836"
✅ La logique est la même, seule la présentation (couleur/colonne) change

## Conclusion

Cette refactorisation garantit que :
1. Aucune nouvelle règle n'a été inventée
2. La logique HS existante est réutilisée à l'identique
3. Le code est plus maintenable et compréhensible
4. Les locomotives non-HS bénéficient de la même logique robuste que les HS
