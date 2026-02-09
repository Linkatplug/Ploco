# TapisT13 - Affichage en Vert des Locomotives Non-HS sur Lignes de Roulement

## Vue d'ensemble

Cette fonctionnalité ajoute l'affichage en **vert** des locomotives non-HS (OK, ManqueTraction, DefautMineur) qui se trouvent sur des lignes de roulement **avec affectation train** dans le rapport TapisT13.

## Comportement

### Avant
- Seules les locomotives **HS** sur lignes de roulement étaient affichées (en rouge)
- Les locomotives non-HS sur lignes de roulement n'apparaissaient pas dans "Infos/Rapport"

### Après
- Les locomotives **HS** restent affichées en **rouge** (inchangé)
- Les locomotives **non-HS** sur lignes de roulement **avec affectation train** sont maintenant affichées en **vert**
- Format identique à HS : "NomTuile NumeroTrain"

## Critères d'affichage

### Affichage en Rouge (HS)
Une locomotive apparaît en rouge dans "Infos/Rapport" si :
- `Status == LocomotiveStatus.HS`
- Elle est sur n'importe quelle ligne/tuile

**Format d'affichage :**
- Si `IsOnTrain == true` et `TrainNumber` existe : `"NomTuile TrainNumber"`
- Sinon : `"NomTuile"`

### Affichage en Vert (Non-HS sur ligne de roulement AVEC affectation train)
Une locomotive apparaît en vert dans "Infos/Rapport" si **TOUTES** les conditions suivantes sont remplies :
- `Status != LocomotiveStatus.HS` (OK, ManqueTraction, DefautMineur)
- `track.Kind == TrackKind.RollingLine` (sur une ligne de roulement)
- `track.IsOnTrain == true` (affectée à un train) **← IMPORTANT**

**Format d'affichage :**
- Identique au format HS
- Si `IsOnTrain == true` et `TrainNumber` existe : `"NomTuile TrainNumber"`
- Sinon : `"NomTuile"`

**Note importante :** Une locomotive sur une ligne de roulement SANS affectation train (`IsOnTrain = false`) n'apparaîtra PAS en vert et le champ "Infos/Rapport" restera vide.

### Pas d'affichage couleur
Les locomotives ne sont pas colorées si :
- Elles ne sont pas sur une ligne de roulement ET ne sont pas HS

## Exemples

### Exemple 1 : Locomotive OK sur ligne 1103
```
Status: OK
Track: 1103 (RollingLine)
IsOnTrain: true
TrainNumber: "42350"
TileName: "Thionville"

→ Affichage dans "Infos/Rapport" : "THL 42350" (en VERT)
```

### Exemple 2 : Locomotive ManqueTraction sur ligne 1105 SANS affectation train
```
Status: ManqueTraction
Track: 1105 (RollingLine)
IsOnTrain: false  ← Pas de train affecté
TileName: "Anvers Nord"

→ Affichage dans "Infos/Rapport" : "" (VIDE, pas de couleur)
```

### Exemple 2bis : Locomotive ManqueTraction sur ligne 1105 AVEC affectation train
```
Status: ManqueTraction
Track: 1105 (RollingLine)
IsOnTrain: true  ← Train affecté
TrainNumber: "42352"
TileName: "Anvers Nord"

→ Affichage dans "Infos/Rapport" : "FN 42352" (en VERT)
```

### Exemple 3 : Locomotive HS sur ligne 1106
```
Status: HS
Track: 1106 (RollingLine)
IsOnTrain: true
TrainNumber: "42351"
TileName: "Mulhouse Nord"

→ Affichage dans "Loc HS" : "MUN 42351" (en ROUGE)
→ Affichage dans "Infos/Rapport" : "MUN 42351" (en ROUGE)
```

### Exemple 4 : Locomotive DefautMineur sur ligne 1107
```
Status: DefautMineur
DefautInfo: "Problème freins"
Track: 1107 (RollingLine)
IsOnTrain: true
TrainNumber: "42352"
TileName: "Woippy"

→ Affichage dans "Infos/Rapport" : "WPY 42352" (en VERT)
```

## Abréviations de Localisation

Les noms de tuiles sont abrégés selon la table suivante :

| Nom Complet | Abréviation |
|-------------|-------------|
| Thionville | THL |
| SRH | SRH |
| Anvers Nord / Anvers | FN |
| Mulhouse Nord / Mulhouse | MUN |
| Bale | BAL |
| Woippy | WPY |
| Uckange | UCK |
| Zeebrugge | LZR |
| Gent | FGZH |
| Muizen | FIZ |
| Monceau | LNC |
| La Louviere | GLI |
| Chatelet | FCL |
| Autres | Nom complet |

## Implémentation Technique

### Modèle de Données

**Classe T13Row :**
```csharp
private sealed class T13Row
{
    public string Locomotive { get; set; }
    public string MaintenanceDate { get; set; }
    public string Motif { get; set; }
    public string LocHs { get; set; }
    public string Report { get; set; }
    public bool IsHs { get; set; }
    public bool IsOnRollingLine { get; set; }          // Nouvelle propriété
    public bool IsNonHsOnRollingLine { get; set; }     // Nouvelle propriété
}
```

### Logique de Calcul

**Dans LoadRows() :**
```csharp
var isHs = loco.Status == LocomotiveStatus.HS;
var isOnRollingLine = track?.Kind == TrackKind.RollingLine;
// IMPORTANT: Nécessite IsOnTrain = true pour affichage vert
var isNonHsOnRollingLine = isOnRollingLine && !isHs && track?.IsOnTrain == true;

// Report logic - ORDRE DE PRIORITÉ IMPORTANT:
// 1. Si HS, afficher train info (comportement existant)
// 2. Si non-HS sur ligne avec train, afficher train info (VERT)
// 3. Si numéro de ligne existe, l'afficher (fallback)
// 4. Sinon vide
var report = isHs ? trainInfo 
    : isNonHsOnRollingLine ? trainInfo
    : !string.IsNullOrWhiteSpace(rollingLineNumber) ? rollingLineNumber
    : string.Empty;
```

**Note importante :** L'ordre de priorité a été modifié pour que les informations de train (HS ou non-HS) soient affichées en premier. Cela garantit que les locomotives non-HS affectées à des trains affichent correctement "NomTuile NumeroTrain" au lieu de simplement le numéro de ligne.

### Style XAML

**ReportCellStyle :**
```xaml
<Style x:Key="ReportCellStyle" TargetType="DataGridCell">
    <Style.Triggers>
        <!-- Priority 1: HS = Red -->
        <DataTrigger Binding="{Binding IsHs}" Value="True">
            <Setter Property="Background" Value="#FFD32F2F"/>
            <Setter Property="Foreground" Value="White"/>
        </DataTrigger>
        <!-- Priority 2: Non-HS on Rolling Line = Green -->
        <DataTrigger Binding="{Binding IsNonHsOnRollingLine}" Value="True">
            <Setter Property="Background" Value="#FF4CAF50"/>
            <Setter Property="Foreground" Value="White"/>
        </DataTrigger>
    </Style.Triggers>
</Style>
```

**Application :**
```xaml
<DataGridTextColumn Header="Infos / Rapport" 
                    Binding="{Binding Report}" 
                    Width="*" 
                    CellStyle="{StaticResource ReportCellStyle}"/>
```

## Priorité des Couleurs

Les DataTriggers sont appliqués dans l'ordre :
1. **Rouge (HS)** : Priorité absolue
2. **Vert (Non-HS sur ligne de roulement)** : Appliqué si pas HS
3. **Pas de couleur** : Par défaut

Cela signifie que si une locomotive est HS, elle sera TOUJOURS rouge, même si elle est sur une ligne de roulement.

## Correction du Bug d'Affichage (9 février 2026)

### Problème Identifié
Les locomotives non-HS sur lignes de roulement avec affectation train n'affichaient pas les informations de train dans "Infos/Rapport". Le champ restait vide.

**Exemple du problème :**
- Tuile "FIZ" (ligne de roulement)
- Loco 1347 (HS) sur train 41836 : ✅ Affichait "FIZ 41836" en rouge
- Loco 1334 (OK) sur train 41836 : ❌ Champ vide au lieu de "FIZ 41836" en vert

### Cause
L'ordre de priorité dans la logique `Report` donnait la priorité à `rollingLineNumber` (numéro de track comme "1103") sur les informations de train, empêchant l'affichage de "NomTuile NumeroTrain".

### Solution
Modification de l'ordre de priorité dans `LoadRows()` :

**Avant (incorrect) :**
```csharp
var report = !string.IsNullOrWhiteSpace(rollingLineNumber)
    ? rollingLineNumber      // ← Priorité 1 (bloquait train info)
    : isHs ? trainInfo       // ← Priorité 2
    : isNonHsOnRollingLine ? trainInfo  // ← Priorité 3
    : string.Empty;
```

**Après (correct) :**
```csharp
var report = isHs ? trainInfo               // ← Priorité 1
    : isNonHsOnRollingLine ? trainInfo      // ← Priorité 2
    : !string.IsNullOrWhiteSpace(rollingLineNumber) ? rollingLineNumber  // ← Priorité 3
    : string.Empty;
```

### Résultat
Les locomotives non-HS sur trains affichent maintenant correctement "NomTuile NumeroTrain" en vert dans la colonne "Infos/Rapport".

**Exemple corrigé :**
- Loco 1347 (HS) sur FIZ train 41836 : 🔴 "FIZ 41836" (rouge) ✅
- Loco 1334 (OK) sur FIZ train 41836 : 🟢 "FIZ 41836" (vert) ✅

## Tests

### Scénarios de Test

1. **Locomotive OK sur ligne de roulement**
   - ✅ Doit apparaître en vert dans "Infos/Rapport"
   - ✅ Format : "NomTuile" ou "NomTuile NumTrain"

2. **Locomotive ManqueTraction sur ligne de roulement**
   - ✅ Doit apparaître en vert dans "Infos/Rapport"
   - ✅ Format identique

3. **Locomotive DefautMineur sur ligne de roulement**
   - ✅ Doit apparaître en vert dans "Infos/Rapport"
   - ✅ Format identique

4. **Locomotive HS sur ligne de roulement**
   - ✅ Doit apparaître en rouge (comportement existant inchangé)
   - ✅ Dans "Loc HS" ET "Infos/Rapport"

5. **Locomotive OK hors ligne de roulement**
   - ✅ Pas d'affichage dans "Infos/Rapport"
   - ✅ Pas de couleur

6. **Locomotive avec IsOnTrain=true et TrainNumber**
   - ✅ Format : "THL 42350" (avec espace entre nom et numéro)

7. **Locomotive avec IsOnTrain=false**
   - ✅ Format : "THL" (nom seul)

## Bénéfices

1. **Visibilité améliorée** : Les locomotives en ligne mais non-HS sont maintenant visibles
2. **Distinction claire** : 
   - Rouge = Problème (HS)
   - Vert = En service sur ligne
3. **Cohérence** : Même format d'affichage que pour les HS
4. **Compatibilité** : Aucun impact sur le comportement existant des HS

## Compatibilité

Cette fonctionnalité est compatible avec :
- ✅ Le système de statuts existant (OK, ManqueTraction, HS, DefautMineur)
- ✅ Le système de placement prévisionnel (forecast)
- ✅ Les copies de colonnes (boutons "Copier...")
- ✅ Le tri et le filtrage du DataGrid
- ✅ Le rafraîchissement des données

## Notes

- Les couleurs utilisent le Material Design :
  - Rouge : `#FFD32F2F` (Red 700)
  - Vert : `#FF4CAF50` (Green 500)
- Le texte est blanc sur fond coloré pour une meilleure lisibilité
- La logique de détection de ligne de roulement utilise `TrackKind.RollingLine`
- Le format d'affichage respecte l'option "loc au train" via `track.IsOnTrain`
