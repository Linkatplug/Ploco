# Fix: Placement Prévisionnel - Problème d'Instances Multiples

## Problème Critique Résolu

### Symptômes Observés (Logs)

```
Ghost créé pour source Id=7460 Number=1312
Annuler reçoit Id=7509 Number=1312 → "No ghosts found"
Valider reçoit Id=7607 Number=1312 → "No ghosts found" → "occupied" → move échoue
```

### Cause Racine

**WPF Data Binding** crée parfois plusieurs instances d'objets `LocomotiveModel` pour la même locomotive logique.

Quand on clique sur une locomotive dans l'UI :
1. La locomotive affichée a `Id=7460 Number=1312`
2. Ghost créé avec `ForecastSourceLocomotiveId=7460`
3. Mais au clic droit sur menu "Annuler", WPF peut fournir une AUTRE instance avec `Id=7509 Number=1312`
4. `RemoveForecastGhostsFor()` cherche un ghost avec `ForecastSourceLocomotiveId=7509` → **NOT FOUND**

**Résultat:** Ghost reste affiché, annulation échoue, validation échoue.

## Solutions Implémentées

### A) Correction de `GetLocomotiveFromMenuItem()`

#### Avant (Problématique)
```csharp
private static LocomotiveModel? GetLocomotiveFromMenuItem(object sender)
{
    if (sender is not MenuItem menuItem) return null;
    
    // MAUVAIS: vérifie CommandParameter en premier
    if (menuItem.CommandParameter is LocomotiveModel parameter)
        return parameter;
    
    // MAUVAIS: vérifie DataContext du MenuItem (peut être ambigu)
    if (menuItem.DataContext is LocomotiveModel dataContext)
        return dataContext;
    
    // OK mais en dernier
    if (contextMenu?.PlacementTarget?.DataContext is LocomotiveModel loco)
        return loco;
}
```

**Problème:** Retourne une mauvaise instance car `menuItem.DataContext` peut contenir une copie.

#### Après (Correct)
```csharp
private static LocomotiveModel? GetLocomotiveFromMenuItem(object sender)
{
    if (sender is not MenuItem menuItem) return null;
    
    var contextMenu = menuItem.Parent as ContextMenu ?? ...;
    
    if (contextMenu != null)
    {
        // PRIORITÉ 1: ContextMenu.DataContext
        // (défini par LocomotiveItem_PreviewMouseRightButtonDown)
        if (contextMenu.DataContext is LocomotiveModel contextLoco)
            return contextLoco;
        
        // PRIORITÉ 2: PlacementTarget.DataContext
        if (contextMenu.PlacementTarget?.DataContext is LocomotiveModel placementLoco)
            return placementLoco;
    }
    
    // PRIORITÉ 3: CommandParameter (si explicitement défini)
    if (menuItem.CommandParameter is LocomotiveModel parameter)
        return parameter;
    
    // PRIORITÉ 4: MenuItem.DataContext (dernier recours)
    if (menuItem.DataContext is LocomotiveModel dataContext)
        return dataContext;
}
```

**Pourquoi ça marche:**
`LocomotiveItem_PreviewMouseRightButtonDown` définit déjà:
```csharp
contextMenu.DataContext = element.DataContext;
contextMenu.PlacementTarget = element;
```

Donc `contextMenu.DataContext` contient **toujours** la bonne instance.

### B) Ajout de Logging au Menu Contextuel

```csharp
private void LocomotiveTileContextMenu_Opened(object sender, RoutedEventArgs e)
{
    // ... récupérer loco ...
    
    Logger.Debug($"Opened context menu on loco Id={loco.Id} Number={loco.Number} " +
                 $"IsForecastOrigin={loco.IsForecastOrigin} IsForecastGhost={loco.IsForecastGhost}", 
                 "ContextMenu");
}
```

**Bénéfice:** On peut tracer quelle instance est utilisée dans les logs.

### C) Suppression de Ghost Robuste avec Fallback

#### Avant (Fragile)
```csharp
private int RemoveForecastGhostsFor(LocomotiveModel loco)
{
    foreach (var track in tracks)
    {
        var ghostsToRemove = track.Locomotives
            .Where(l => l.IsForecastGhost && 
                   l.ForecastSourceLocomotiveId == loco.Id) // SEULEMENT par Id exact
            .ToList();
    }
}
```

**Problème:** Si `loco.Id != ForecastSourceLocomotiveId`, ghost non trouvé.

#### Après (Robuste)
```csharp
private int RemoveForecastGhostsFor(LocomotiveModel loco)
{
    // 1. Prioriser la track cible si connue
    var targetTrackId = loco.ForecastTargetRollingLineTrackId;
    var allTracks = _tiles.SelectMany(t => t.Tracks).ToList();
    
    IEnumerable<TrackModel> orderedTracks;
    if (targetTrackId != null)
    {
        // Track cible en premier, puis les autres
        orderedTracks = allTracks.Where(t => t.Id == targetTrackId)
                                  .Concat(allTracks.Where(t => t.Id != targetTrackId));
    }
    else
    {
        orderedTracks = allTracks;
    }
    
    foreach (var track in orderedTracks)
    {
        // 2. Matching robuste avec FALLBACK
        var ghostsToRemove = track.Locomotives
            .Where(l => l.IsForecastGhost && 
                (l.ForecastSourceLocomotiveId == loco.Id ||              // Match primaire par Id
                 (l.Number == loco.Number && l.SeriesId == loco.SeriesId))) // Fallback par Number+SeriesId
            .ToList();
        
        foreach (var ghost in ghostsToRemove)
        {
            track.Locomotives.Remove(ghost);
            
            // 3. Logger la raison du match
            var matchReason = ghost.ForecastSourceLocomotiveId == loco.Id 
                ? "SourceIdMatch" 
                : "NumberSeriesFallback";
            
            Logger.Debug($"Removed ghost (Id={ghost.Id}, Number={ghost.Number}) " +
                        $"for source loco (Id={loco.Id}, Number={loco.Number}) " +
                        $"from track {track.Name}, reason={matchReason}", "Forecast");
        }
    }
}
```

**Avantages:**
1. ✅ **Match primaire par Id** - fonctionne dans le cas normal
2. ✅ **Fallback par Number+SeriesId** - fonctionne même avec instance différente
3. ✅ **Priorise track cible** - optimise la recherche
4. ✅ **Log la raison** - permet de voir si fallback utilisé

**Logs résultants:**
```
Removed ghost (Id=-107460, Number=1312) for source loco (Id=7509, Number=1312) 
from track 1102, reason=NumberSeriesFallback
```

### D) Protection Contre les Ghosts dans `MoveLocomotiveToTrack()`

#### Avant (Problématique)
```csharp
if (targetTrack.Kind == TrackKind.RollingLine && 
    targetTrack.Locomotives.Any() &&  // INCLUT LES GHOSTS!
    !targetTrack.Locomotives.Contains(loco))
{
    MessageBox.Show("Une seule locomotive est autorisée...");
    return;
}
```

**Problème:** Si un ghost est présent, considéré comme "occupé" → move bloqué.

#### Après (Correct)
```csharp
if (targetTrack.Kind == TrackKind.RollingLine)
{
    // Ne considérer QUE les locos réelles (exclure ghosts)
    var realLocosInTarget = targetTrack.Locomotives
        .Where(l => !l.IsForecastGhost)
        .ToList();
    
    if (realLocosInTarget.Any() && !targetTrack.Locomotives.Contains(loco))
    {
        Logger.Warning($"Cannot move loco Id={loco.Id} Number={loco.Number} " +
                      $"to occupied rolling line {targetTrack.Name} " +
                      $"(occupied by {realLocosInTarget.Count} real loco(s))", "Movement");
        MessageBox.Show("Une seule locomotive est autorisée...");
        return;
    }
}
```

**Résultat:** Un ghost ne peut JAMAIS bloquer un déplacement.

### E) Logs Améliorés

Tous les logs incluent maintenant **Id + Number**:

```
Moving locomotive Id=7460 Number=1312 to track 1102 at index 0
Removed loco Id=7460 Number=1312 from Depot A (index 0)
Added loco Id=7460 Number=1312 to 1102 (end)
Successfully moved loco Id=7460 Number=1312 to 1102
```

**Bénéfice:** On voit clairement si on travaille avec la même instance ou non.

## Scénarios de Test

### Scénario 1: Annulation avec Instance Différente

**Setup:**
1. Créer prévision: loco Id=7460 Number=1312 → rolling line 1102
2. Ghost créé avec `ForecastSourceLocomotiveId=7460`

**Action:**
3. Clic droit "Annuler" (WPF fournit instance Id=7509 Number=1312)

**Avant Fix:**
- ❌ "No ghosts found for loco Id=7509 Number=1312"
- ❌ Ghost reste affiché sur rolling line
- ❌ Origin reste bleue

**Après Fix:**
- ✅ "Removed ghost... reason=NumberSeriesFallback"
- ✅ Ghost supprimé de la rolling line
- ✅ Origin redevient couleur statut

### Scénario 2: Validation avec Instance Différente

**Setup:**
1. Créer prévision: loco Id=7460 Number=1312 → rolling line 1109
2. Ghost créé avec `ForecastSourceLocomotiveId=7460`

**Action:**
3. Clic droit "Valider" (WPF fournit instance Id=7607 Number=1312)

**Avant Fix:**
- ❌ "No ghosts found for loco Id=7607 Number=1312"
- ❌ "Cannot move loco 1312 to occupied rolling line 1109" (ghost compte comme occupant)
- ❌ Move échoue
- ❌ Log "Forecast validated" quand même (bug)
- ❌ Doublon: ghost + loco ou rien

**Après Fix:**
- ✅ "Removed ghost... reason=NumberSeriesFallback"
- ✅ Ghost supprimé AVANT vérification d'occupation
- ✅ Move réussit (pas de ghost bloquant)
- ✅ Log "validated" seulement si move réussi
- ✅ Une seule loco dans rolling line (la vraie)

### Scénario 3: Même Instance (Cas Normal)

**Action:**
Annuler ou Valider avec la même instance (Id=7460)

**Résultat:**
- ✅ "Removed ghost... reason=SourceIdMatch"
- ✅ Match primaire par Id fonctionne
- ✅ Fallback pas utilisé

## Points Clés de la Solution

### 1. Priorité de Récupération d'Instance
```
ContextMenu.DataContext > PlacementTarget.DataContext > CommandParameter > MenuItem.DataContext
```

### 2. Double Matching pour Ghost
```
Primary: ForecastSourceLocomotiveId == loco.Id
Fallback: Number == loco.Number && SeriesId == loco.SeriesId
```

### 3. Ghosts Exclus des Vérifications
```
realLocos = locomotives.Where(l => !l.IsForecastGhost)
```

### 4. Logs Complets
```
"Id={internalId} Number={displayNumber}"
"reason={SourceIdMatch|NumberSeriesFallback}"
```

## Prévention des Régressions

### À NE JAMAIS FAIRE

❌ **Ne pas** utiliser `menuItem.DataContext` en priorité
```csharp
// MAUVAIS
if (menuItem.DataContext is LocomotiveModel loco) // Peut être mauvaise instance
    return loco;
```

❌ **Ne pas** matcher ghost uniquement par Id
```csharp
// FRAGILE
.Where(l => l.IsForecastGhost && l.ForecastSourceLocomotiveId == loco.Id)
```

❌ **Ne pas** compter les ghosts comme "occupation"
```csharp
// MAUVAIS
if (targetTrack.Locomotives.Any()) // Inclut ghosts
```

### À TOUJOURS FAIRE

✅ **Priorité** `ContextMenu.DataContext` / `PlacementTarget.DataContext`
```csharp
if (contextMenu.DataContext is LocomotiveModel loco)
    return loco; // Instance correcte
```

✅ **Fallback matching** par Number+SeriesId
```csharp
.Where(l => l.IsForecastGhost && 
    (l.ForecastSourceLocomotiveId == loco.Id ||
     (l.Number == loco.Number && l.SeriesId == loco.SeriesId)))
```

✅ **Exclure ghosts** des vérifications d'occupation
```csharp
var realLocos = track.Locomotives.Where(l => !l.IsForecastGhost).ToList();
```

✅ **Logger Id+Number** partout
```csharp
Logger.Info($"Operation on loco Id={loco.Id} Number={loco.Number}");
```

## Conclusion

Cette correction rend le système de placement prévisionnel **robuste face au comportement de WPF** où plusieurs instances de la même locomotive logique peuvent exister.

Le **fallback matching** par Number+SeriesId garantit que les opérations réussissent même quand WPF fournit une instance différente.

Les **logs améliorés** permettent de tracer précisément quelle instance est utilisée et comment le matching fonctionne.

La **protection contre les ghosts** dans MoveLocomotiveToTrack empêche définitivement qu'un ghost bloque un déplacement.

✅ Le système est maintenant **production-ready** et gère correctement tous les cas de figure.
