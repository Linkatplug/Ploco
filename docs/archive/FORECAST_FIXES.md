# Correctifs Placement Prévisionnel - Résumé

## Problèmes Identifiés (via logs)

1. ❌ **Annulation**: "Ghost not found for loco 1311 in track 1102"
2. ❌ **Validation**: "Cannot move loco 1315 to occupied rolling line 1109"
3. ❌ **Validation**: Log "Forecast validated for loco 1315" alors que le déplacement a échoué

## Causes Racines

### 1. Recherche de Ghost Limitée
- L'annulation cherchait le ghost uniquement dans `ForecastTargetRollingLineTrackId`
- Si le ghost était déplacé ou perdu, il n'était pas trouvé
- **Solution**: Scanner TOUTES les tracks de TOUTES les tiles

### 2. Ordre d'Opérations Incorrect
- La validation vérifiait les locomotives réelles AVANT de supprimer le ghost
- Le ghost comptait comme "locomotive occupante"
- `MoveLocomotiveToTrack()` refusait le déplacement (rolling line occupée)
- **Solution**: Supprimer le ghost EN PREMIER

### 3. Absence de Vérification du Succès
- Le log "validated" était émis AVANT de vérifier si `MoveLocomotiveToTrack()` avait réussi
- Impossible de savoir si le déplacement a vraiment fonctionné
- **Solution**: Vérifier `loco.AssignedTrackId == targetTrack.Id` après le déplacement

### 4. Confusion Id vs Number
- Les logs utilisaient parfois `loco.Number` sans préciser qu'il s'agit du numéro affiché
- Difficile de déboguer avec uniquement le Number
- **Solution**: Toujours logger "Id=xx Number=1311"

### 5. Couleur Ghost Forcée
- Le XAML forçait la couleur verte pour les ghosts
- Ne reflétait pas le statut réel (HS=rouge, ManqueTraction=orange)
- **Solution**: Supprimer l'override vert, utiliser opacity pour distinction

## Correctifs Implémentés

### A) Fonction Utilitaire `RemoveForecastGhostsFor(loco)`

```csharp
private int RemoveForecastGhostsFor(LocomotiveModel loco)
{
    int ghostsRemoved = 0;
    var tracksWithGhosts = new List<string>();

    // Scan ALL tracks in ALL tiles
    foreach (var tile in _tiles)
    {
        foreach (var track in tile.Tracks)
        {
            var ghostsToRemove = track.Locomotives
                .Where(l => l.IsForecastGhost && l.ForecastSourceLocomotiveId == loco.Id)
                .ToList();

            foreach (var ghost in ghostsToRemove)
            {
                track.Locomotives.Remove(ghost);
                ghostsRemoved++;
                tracksWithGhosts.Add(track.Name);
                Logger.Debug($"Removed ghost (Id={ghost.Id}, Number={ghost.Number})...");
            }
        }
    }

    if (ghostsRemoved > 0)
    {
        Logger.Info($"Removed {ghostsRemoved} ghost(s) for loco Id={loco.Id} Number={loco.Number}...");
    }
    else
    {
        Logger.Warning($"No ghosts found for loco Id={loco.Id} Number={loco.Number}...");
    }

    return ghostsRemoved;
}
```

**Avantages:**
- ✅ Recherche exhaustive (toutes les tracks)
- ✅ Utilise `ForecastSourceLocomotiveId` (fiable)
- ✅ Logs détaillés avec Id+Number
- ✅ Retourne le nombre de ghosts supprimés
- ✅ Réutilisable pour Cancel et Validate

### B) Annulation Corrigée

**AVANT:**
```csharp
var targetTrack = _tiles.SelectMany(t => t.Tracks)
    .FirstOrDefault(t => t.Id == loco.ForecastTargetRollingLineTrackId);

if (targetTrack != null)
{
    var ghost = targetTrack.Locomotives.FirstOrDefault(...);
    // Recherche limitée à UNE seule track
}
```

**APRÈS:**
```csharp
int ghostsRemoved = RemoveForecastGhostsFor(loco);

loco.IsForecastOrigin = false;
loco.ForecastTargetRollingLineTrackId = null;

if (ghostsRemoved > 0)
{
    Logger.Info($"Forecast cancelled successfully for loco Id={loco.Id} Number={loco.Number}");
}
```

**Résultat:**
- ✅ Ghost toujours trouvé et supprimé
- ✅ Logs clairs avec Id+Number
- ✅ Log de succès uniquement si ghost supprimé

### C) Validation Corrigée - Ordre Strict

**AVANT (problématique):**
```csharp
// 1. Chercher le ghost
var ghost = targetTrack.Locomotives.FirstOrDefault(...);

// 2. Vérifier locos réelles (ghost encore présent!)
var realLocosInTarget = targetTrack.Locomotives.Where(l => !l.IsForecastGhost).ToList();

// 3. Supprimer ghost
if (ghost != null) targetTrack.Locomotives.Remove(ghost);

// 4. Déplacer loco (peut échouer)
MoveLocomotiveToTrack(loco, targetTrack, 0);

// 5. Logger "validated" (TOUJOURS, même si échec!)
Logger.Info("Forecast validated...");
```

**APRÈS (correct):**
```csharp
// 1. Sauver targetTrackId AVANT reset
var targetTrackId = loco.ForecastTargetRollingLineTrackId;

// 2. SUPPRIMER GHOST EN PREMIER
int ghostsRemoved = RemoveForecastGhostsFor(loco);

// 3. Maintenant vérifier locos réelles (sans ghost)
var realLocosInTarget = targetTrack.Locomotives.Where(l => !l.IsForecastGhost).ToList();

// 4. Gérer ligne occupée (avec confirmation utilisateur)
if (realLocosInTarget.Any())
{
    // Demander confirmation
    if (user says No)
    {
        // Re-créer ghost et sortir
        return;
    }
    // Supprimer locos existantes
}

// 5. Reset flags
loco.IsForecastOrigin = false;
loco.ForecastTargetRollingLineTrackId = null;

// 6. Déplacer loco
MoveLocomotiveToTrack(loco, targetTrack, 0);

// 7. VÉRIFIER SUCCÈS
if (loco.AssignedTrackId == targetTrack.Id)
{
    // Succès!
    Logger.Info($"Forecast validated successfully: loco Id={loco.Id}...");
}
else
{
    // Échec!
    Logger.Warning($"Forecast validation FAILED: loco Id={loco.Id} NOT moved...");
    MessageBox.Show("Le déplacement a échoué...");
}
```

**Points clés:**
1. ✅ Ghost supprimé AVANT vérification locos réelles
2. ✅ Ghost supprimé AVANT appel `MoveLocomotiveToTrack()`
3. ✅ Vérification du résultat du déplacement
4. ✅ Log "validated" uniquement si succès
5. ✅ Log warning si échec
6. ✅ Message d'erreur à l'utilisateur si échec

### D) Logs Améliorés - Id + Number

**AVANT:**
```
Forecast placement: loco 1311 -> track 1102
Ghost not found for loco 1311 in track 1102
```

**APRÈS:**
```
Forecast placement requested: loco Id=42 Number=1311
Forecast placement: loco Id=42 Number=1311 -> track 1102
Created ghost Id=-100042 Number=1311 for source loco Id=42 Number=1311, Status=HS
...
Removed ghost (Id=-100042, Number=1311) for source loco (Id=42, Number=1311) from track 1102
Removed 1 ghost(s) for loco Id=42 Number=1311 from tracks: 1102
Forecast cancelled successfully for loco Id=42 Number=1311
```

**Avantage:**
- ✅ Traçabilité complète avec Id interne
- ✅ Numéro affiché pour l'utilisateur
- ✅ Pas de confusion possible

### E) XAML - Couleur Ghost Corrigée

**AVANT:**
```xml
<!-- Forecast ghost: Green -->
<DataTrigger Binding="{Binding IsForecastGhost}" Value="True">
    <Setter Property="Background" Value="Green"/>
</DataTrigger>
```

**APRÈS:**
```xml
<!-- Forecast ghost: Show status color with slight transparency -->
<DataTrigger Binding="{Binding IsForecastGhost}" Value="True">
    <Setter Property="Opacity" Value="0.8"/>
</DataTrigger>
```

**Résultat:**
- ✅ Ghost HS → Rouge (avec opacity 0.8)
- ✅ Ghost ManqueTraction → Orange (avec opacity 0.8)
- ✅ Ghost Ok → Vert (avec opacity 0.8)
- ✅ Distinction visuelle par transparence, pas par couleur

## Tests de Validation

### 1. Statuts de Base
- [x] HS → Rouge ✅
- [x] ManqueTraction → Orange ✅
- [x] Ok → Vert ✅

### 2. Placement Prévisionnel
- [x] Origin devient bleue ✅
- [x] Ghost sur rolling line garde la couleur statut ✅
  - Ghost HS → Rouge (80% opacité)
  - Ghost ManqueTraction → Orange (80% opacité)
  - Ghost Ok → Vert (80% opacité)

### 3. Annulation
- [x] Ghost disparaît toujours (même s'il a bougé) ✅
- [x] Origin revient au statut automatiquement ✅
- [x] Log indique nombre de ghosts supprimés ✅

### 4. Validation
- [x] Aucune popup "Une seule locomotive..." ✅
- [x] Origin disparaît de la tuile ✅
- [x] Loco reste dans rolling line ✅
- [x] Pas de doublon ✅
- [x] Log "validated" uniquement si succès ✅
- [x] Log warning si échec ✅

### 5. Logs
- [x] Affichent Id+Number ✅
- [x] Indiquent clairement suppression ghost ✅
- [x] Indiquent résultat du déplacement ✅

## Prévention des Régressions

### Points de Contrôle pour Futures Modifications

1. **Suppression de Ghost:**
   - Toujours utiliser `RemoveForecastGhostsFor(loco)`
   - Ne jamais chercher dans une seule track
   - Toujours logger le résultat

2. **Ordre de Validation:**
   - TOUJOURS supprimer ghost AVANT `MoveLocomotiveToTrack()`
   - TOUJOURS vérifier succès après déplacement
   - Ne JAMAIS logger "validated" avant vérification

3. **Logs:**
   - Toujours logger "Id=xx Number=yyyy"
   - Logger les opérations critiques (création/suppression ghost)
   - Logger les succès ET les échecs

4. **Couleurs:**
   - Ne JAMAIS forcer la couleur d'un ghost
   - Utiliser opacity/border pour distinction visuelle
   - Toujours respecter `StatutToBrushConverter`

## Conclusion

Les correctifs garantissent:
- ✅ Robustesse: Ghost toujours trouvé et supprimé
- ✅ Fiabilité: Validation vérifie le succès réel
- ✅ Traçabilité: Logs clairs avec Id+Number
- ✅ Cohérence: Couleurs reflètent le statut réel
- ✅ Expérience utilisateur: Pas de faux messages d'erreur

Le système de placement prévisionnel est maintenant stable et fiable.
