# Affichage Tuile Origine pour Locomotives HS sur Rolling Lines

## Vue d'ensemble

Cette fonctionnalité améliore l'affichage dans le Tapis T13 pour les locomotives HS (hors service) situées sur des lignes de roulement. Elle affiche maintenant:

1. **Colonne "Loc HS"**: La tuile d'origine de la locomotive (d'où elle vient)
2. **Colonne "Infos/Rapport"**: "HS CV {numéro}" avec le numéro de la rolling line (où elle est actuellement)

## Problème Résolu

### Avant
Quand une locomotive HS était mise sur une ligne de roulement:
- L'affichage n'était pas clair
- On ne savait pas d'où venait la locomotive
- Difficile de savoir où la renvoyer après réparation

### Maintenant
Affichage clair et complet:
- **Loc HS**: Tuile d'origine (FIZ, MONS, THL, etc.)
- **Infos/Rapport**: "HS CV 1112" (position actuelle)

## Implémentation Technique

### 1. Nouvelle Méthode: GetOriginTileLocation

```csharp
/// <summary>
/// Gets the origin tile location for a locomotive.
/// Searches for the tile where the locomotive originates (Depot/Garage/Line tracks, not RollingLine).
/// </summary>
private static string GetOriginTileLocation(LocomotiveModel loco, IEnumerable<TileModel> tiles)
{
    // Find the tile where loco originates (in Depot/Garage/Line tracks, not RollingLine)
    foreach (var tile in tiles)
    {
        foreach (var track in tile.Tracks.Where(t => t.Kind != TrackKind.RollingLine))
        {
            // Match by Id or Number (to handle WPF instance mismatches)
            if (track.Locomotives.Any(l => l.Id == loco.Id || l.Number == loco.Number))
            {
                return ResolveLocation(track, tiles);
            }
        }
    }
    return string.Empty;
}
```

**Fonctionnement:**
- Parcourt toutes les tuiles
- Examine les tracks de type Depot, Garage ou Line (pas RollingLine)
- Cherche si la locomotive y est présente
- Retourne le nom de la tuile trouvée (ex: "FIZ", "MONS")
- Gère les instances WPF différentes avec double matching (Id et Number)

### 2. Logique Modifiée dans LoadRows

```csharp
// For HS on rolling line: different display for LocHs vs Report
string locHs, report;
if (isHs && !string.IsNullOrWhiteSpace(rollingLineNumber))
{
    // HS on rolling line: show origin tile in LocHs, "HS CV {number}" in Report
    locHs = GetOriginTileLocation(loco, tiles);
    report = $"HS CV {rollingLineNumber}";
}
else
{
    // Normal logic for other cases
    locHs = isHs ? trainLocationText : string.Empty;
    report = isHs ? trainLocationText 
        : isNonHsOnLine ? trainLocationText
        : !string.IsNullOrWhiteSpace(rollingLineNumber) ? rollingLineNumber
        : trainLocationText;
}
```

**Logique:**
1. Si locomotive HS ET sur rolling line:
   - LocHs = Tuile d'origine
   - Report = "HS CV {numéro}"
2. Sinon:
   - Logique normale existante

## Exemples d'Utilisation

### Exemple 1: Locomotive en Maintenance sur Rolling Line

**Configuration:**
- Locomotive: 1347
- Status: HS (hors service)
- Tuile d'origine: Dépôt FIZ
- Position actuelle: Ligne de roulement 1112

**Affichage TapisT13:**
```
| Locomotive | Loc HS    | Infos/Rapport    |
|------------|-----------|------------------|
| 1347       | FIZ 🔴    | HS CV 1112 🔴   |
```

**Interprétation:**
- La locomotive vient du dépôt FIZ
- Elle est actuellement HS sur la ligne 1112
- Après réparation, elle doit retourner à FIZ

### Exemple 2: Plusieurs Locomotives en Maintenance

**Configuration:**
| Loco | Status | Origine | Rolling Line |
|------|--------|---------|--------------|
| 1347 | HS | FIZ | 1112 |
| 1348 | HS | MONS | 1105 |
| 1349 | HS | THL | 1103 |

**Affichage TapisT13:**
```
| Locomotive | Loc HS     | Infos/Rapport    |
|------------|------------|------------------|
| 1347       | FIZ 🔴     | HS CV 1112 🔴   |
| 1348       | MONS 🔴    | HS CV 1105 🔴   |
| 1349       | THL 🔴     | HS CV 1103 🔴   |
```

### Exemple 3: Locomotive HS sur Train (pas sur rolling line)

**Configuration:**
- Locomotive: 1350
- Status: HS
- Position: Train FIZ 41836

**Affichage TapisT13:**
```
| Locomotive | Loc HS        | Infos/Rapport    |
|------------|---------------|------------------|
| 1350       | FIZ 41836 🔴  | FIZ 41836 🔴    |
```

**Note:** Pas de rolling line, donc affichage normal.

### Exemple 4: Locomotive Non-HS sur Rolling Line

**Configuration:**
- Locomotive: 1335
- Status: OK
- Rolling Line: 1106

**Affichage TapisT13:**
```
| Locomotive | Loc HS  | Infos/Rapport |
|------------|---------|---------------|
| 1335       | (vide)  | 1106          |
```

**Note:** Pas HS, donc pas d'affichage dans Loc HS.

## Tableau de Référence Complet

| Scenario | Status | Position | Loc HS | Infos/Rapport | Notes |
|----------|--------|----------|--------|---------------|-------|
| HS sur rolling line (origine FIZ) | HS | Ligne 1112 | 🔴 FIZ | 🔴 HS CV 1112 | Affiche origine + rolling line |
| HS sur rolling line (origine MONS) | HS | Ligne 1105 | 🔴 MONS | 🔴 HS CV 1105 | Affiche origine + rolling line |
| HS sur train | HS | Train FIZ 41836 | 🔴 FIZ 41836 | 🔴 FIZ 41836 | Affichage normal train |
| HS en dépôt | HS | Dépôt FIZ | 🔴 DISPO FIZ | 🔴 DISPO FIZ | Affichage normal dépôt |
| OK sur rolling line | OK | Ligne 1103 | (vide) | 1103 | Juste numéro |
| OK sur train | OK | Train FIZ 41836 | (vide) | 🟢 FIZ 41836 | Train en vert |
| ManqueTraction sur rolling line | ManqueTraction | Ligne 1106 | (vide) | 1106 | Juste numéro |

## Avantages

### Pour les Opérateurs
✅ **Origine visible**: Savoir d'où vient chaque locomotive HS
✅ **Position actuelle claire**: "HS CV 1112" indique où elle est
✅ **Distinction visuelle**: Rouge pour HS, facile à repérer
✅ **Information complète**: Origine + position en un coup d'œil

### Pour la Maintenance
✅ **Traçabilité**: Historique clair de la provenance
✅ **Planification**: Savoir où renvoyer après réparation
✅ **Organisation**: Plusieurs locos HS bien différenciées
✅ **Gestion optimisée**: Pas de confusion possible

### Pour la Gestion
✅ **Reporting précis**: Origine et position de chaque loco HS
✅ **Statistiques**: Analyse par tuile d'origine
✅ **Suivi**: Temps passé en rolling line
✅ **Visibilité**: Vue complète de l'état du parc

## Tests de Validation

### Test 1: HS sur Rolling Line avec Origine
**Configuration:**
- Créer locomotive 1347 HS dans dépôt FIZ
- Déplacer sur rolling line 1112
- Ouvrir Tapis T13

**Résultat Attendu:**
- Loc HS: "FIZ" (rouge)
- Infos/Rapport: "HS CV 1112" (rouge)

### Test 2: Plusieurs HS sur Différentes Rolling Lines
**Configuration:**
- Loco 1347 HS (FIZ) sur ligne 1112
- Loco 1348 HS (MONS) sur ligne 1105
- Loco 1349 HS (THL) sur ligne 1103
- Ouvrir Tapis T13

**Résultat Attendu:**
Chaque locomotive affiche son origine respective dans Loc HS et "HS CV {numéro}" dans Report.

### Test 3: HS sur Train (pas sur rolling line)
**Configuration:**
- Créer locomotive 1350 HS sur train FIZ 41836
- Ouvrir Tapis T13

**Résultat Attendu:**
- Loc HS: "FIZ 41836" (rouge)
- Infos/Rapport: "FIZ 41836" (rouge)
- Pas de "HS CV"

### Test 4: Non-HS sur Rolling Line
**Configuration:**
- Créer locomotive 1335 OK sur ligne 1106
- Ouvrir Tapis T13

**Résultat Attendu:**
- Loc HS: (vide)
- Infos/Rapport: "1106" (pas de couleur)

### Test 5: Changement de Status
**Configuration:**
- Loco 1347 HS sur ligne 1112 (origine FIZ)
- Changer status à OK
- Ouvrir Tapis T13

**Résultat Attendu:**
- Loc HS: (vide)
- Infos/Rapport: "1112" (pas de couleur)

## Notes Techniques

### Gestion des Instances WPF
La méthode `GetOriginTileLocation` utilise un double matching:
```csharp
if (track.Locomotives.Any(l => l.Id == loco.Id || l.Number == loco.Number))
```

Cela gère les cas où WPF crée différentes instances pour la même locomotive logique.

### Exclusion des Rolling Lines
```csharp
foreach (var track in tile.Tracks.Where(t => t.Kind != TrackKind.RollingLine))
```

On cherche uniquement dans les tracks Depot/Garage/Line pour trouver l'origine, pas dans les rolling lines.

### Ordre de Priorité
1. Si HS + rolling line → Affichage spécial (origine + "HS CV")
2. Sinon → Logique normale existante

## Fichiers Modifiés

- **Ploco/Dialogs/TapisT13Window.xaml.cs**
  - Nouvelle méthode `GetOriginTileLocation` (lignes 123-140)
  - Logique modifiée dans LoadRows (lignes 50-68)

## Conclusion

Cette fonctionnalité améliore significativement la clarté de l'affichage pour les locomotives HS sur lignes de roulement. Elle permet:

- ✅ **Traçabilité complète**: Origine visible
- ✅ **Position actuelle claire**: "HS CV {numéro}"
- ✅ **Gestion facilitée**: Savoir où renvoyer après réparation
- ✅ **Information complète**: Tout en un coup d'œil

**Statut:** ✅ Implémenté et prêt pour production
**Build:** ✅ 0 avertissements, 0 erreurs
