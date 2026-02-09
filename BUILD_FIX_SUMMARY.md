# Build Fix Summary - TapisT13 Refactoring

## Problem Résolu ✅

### Erreurs de Build
Les erreurs suivantes ont été corrigées :
```
CSC : error CS2001: Fichier source 'ParcLocoWindow.g.cs' introuvable.
CSC : error CS2001: Fichier source 'PoolTransferWindow.g.cs' introuvable.
CSC : error CS2001: Fichier source 'MainWindow.g.cs' introuvable.
CSC : error CS2001: Fichier source 'HistoriqueDialog.g.cs' introuvable.
```

### Cause
Cache de build corrompu empêchant la génération des fichiers `.g.cs` à partir des fichiers XAML.

### Solution
```bash
dotnet clean
dotnet build
```

### Résultat
✅ Build réussi : 0 avertissements, 0 erreurs

---

## Refactoring TapisT13 Complété ✅

### Objectif
Afficher les locomotives non-HS sur les trains (en ligne) avec la même logique que les HS, mais en vert au lieu de rouge.

### Changements Techniques

#### 1. Méthode Helper `GetTrainLocationText`
```csharp
private static string GetTrainLocationText(LocomotiveModel loco, TrackModel? track, IEnumerable<TileModel> tiles)
{
    if (track == null) return string.Empty;
    var location = ResolveLocation(track, tiles);
    
    // LOGIQUE EXISTANTE HS : si track.IsOnTrain, inclure numéro de train
    if (track.IsOnTrain && !string.IsNullOrWhiteSpace(track.TrainNumber))
    {
        return $"{location} {track.TrainNumber}";
    }
    
    return location;
}
```

**Points clés :**
- ✅ Extrait la logique EXISTANTE utilisée pour les HS
- ✅ Pas de nouvelles règles inventées
- ✅ Réutilisable pour HS et non-HS

#### 2. Propriété `HasTrainInfo`
- Remplace `IsNonHsOnRollingLine`
- Signification plus claire : la locomotive a des infos de train à afficher
- Utilisée pour le DataTrigger vert dans XAML

#### 3. Logique Simplifiée dans `LoadRows`
```csharp
// Calcul unique du texte pour HS et non-HS
var trainLocationText = GetTrainLocationText(loco, track, tiles);
var hasTrainInfo = !string.IsNullOrWhiteSpace(trainLocationText);

// Affichage selon le statut
var locHs = isHs ? trainLocationText : string.Empty;
var report = isHs ? trainLocationText 
    : hasTrainInfo ? trainLocationText
    : rollingLineNumber ?? string.Empty;
```

#### 4. Mise à Jour XAML
```xml
<DataTrigger Binding="{Binding IsHs}" Value="True">
    <Setter Property="Background" Value="#FFD32F2F"/> <!-- Rouge -->
    <Setter Property="Foreground" Value="White"/>
</DataTrigger>
<DataTrigger Binding="{Binding HasTrainInfo}" Value="True">
    <Setter Property="Background" Value="#FF4CAF50"/> <!-- Vert -->
    <Setter Property="Foreground" Value="White"/>
</DataTrigger>
```

### Exemple de Résultat

**Tuile "FIZ" (En ligne) avec train 41836 :**

| Locomotive | Statut | Colonne "Loc HS" | Colonne "Infos/Rapport" |
|------------|--------|------------------|------------------------|
| 1347 | HS | 🔴 "FIZ 41836" (rouge) | 🔴 "FIZ 41836" (rouge) |
| 1334 | OK | Vide | 🟢 "FIZ 41836" (vert) |

### Avantages de cette Approche

1. **Réutilisation de Code** : Une seule méthode pour calculer le texte
2. **Pas de Duplication** : HS et non-HS utilisent la même logique
3. **Maintenabilité** : Un seul endroit à modifier si la logique change
4. **Clarté** : Le code est plus facile à comprendre
5. **Cohérence** : Même format de texte pour HS et non-HS

### Logging Debug Ajouté

```
[TapisT13] Processing loco 1334: Status=Ok, Pool=Sibelit
[TapisT13]   Track: Name=1103, Kind=RollingLine, IsOnTrain=True, TrainNumber=42350
[TapisT13]   TrainLocationText: FIZ 42350
[TapisT13]   Flags: isHs=False, hasTrainInfo=True
[TapisT13]   Results: LocHs='', Report='FIZ 42350'
```

Les logs permettent de vérifier que la logique fonctionne correctement.

---

## Comment Tester

1. **Ouvrir Ploco.exe**
2. **Placer une locomotive HS sur une ligne de roulement avec train**
   - Vérifier : affichage rouge dans "Loc HS" et "Infos/Rapport"
3. **Placer une locomotive OK sur la même ligne**
   - Vérifier : affichage vert dans "Infos/Rapport" uniquement
4. **Ouvrir Tapis T13**
   - Vérifier les colonnes "Loc HS" et "Infos/Rapport"
5. **Consulter les logs** (`%AppData%\Ploco\Logs\`)
   - Rechercher `[TapisT13]` pour voir les détails

---

## Fichiers Modifiés

- `Ploco/Dialogs/TapisT13Window.xaml` - DataTrigger pour HasTrainInfo
- `Ploco/Dialogs/TapisT13Window.xaml.cs` - Helper GetTrainLocationText + HasTrainInfo
- `TAPIS_T13_REFACTORING.md` - Documentation technique (nouveau)
- `BUILD_FIX_SUMMARY.md` - Ce document (nouveau)

---

## Conclusion

✅ Build fixé
✅ Refactoring complété
✅ Logique HS réutilisée (pas de nouvelles règles)
✅ Affichage vert pour locomotives non-HS sur trains
✅ Documentation complète
✅ Logging debug ajouté

**Prêt pour les tests utilisateur !**
