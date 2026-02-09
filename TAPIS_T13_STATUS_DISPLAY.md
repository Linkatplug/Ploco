# TapisT13 - Affichage des Informations de Statut

## Vue d'ensemble

Cette fonctionnalité améliore l'affichage des informations de statut dans la fenêtre TapisT13 en :
1. Affichant les informations de statut pour les locomotives Rouge (HS), Jaune (DefautMineur) et Orange (ManqueTraction)
2. Améliorant la lisibilité de la couleur jaune (passage à une couleur ambre plus foncée)

## Modifications Apportées

### 1. Affichage des Informations de Statut

#### Colonne "Statut / Motif"
La colonne affiche maintenant les informations de statut selon le type de locomotive :

| Statut | Information Affichée | Source |
|--------|---------------------|--------|
| HS (Rouge) | Raison du statut HS | `loco.HsReason` |
| DefautMineur (Jaune) | Description du défaut | `loco.DefautInfo` |
| ManqueTraction (Orange) | "Manque traction" | Texte fixe |
| Ok (Vert) | Vide | - |

#### Code Implémenté
```csharp
// Motif/Status info for HS, DefautMineur, and ManqueTraction
var motif = loco.Status switch
{
    LocomotiveStatus.HS => loco.HsReason ?? string.Empty,
    LocomotiveStatus.DefautMineur => loco.DefautInfo ?? string.Empty,
    LocomotiveStatus.ManqueTraction => "Manque traction",
    _ => string.Empty
};
```

### 2. Coloration de la Colonne Statut/Motif

#### Nouveau Style XAML: MotifCellStyle
```xml
<Style x:Key="MotifCellStyle" TargetType="DataGridCell">
    <Style.Triggers>
        <DataTrigger Binding="{Binding Status}" Value="HS">
            <Setter Property="Background" Value="#FFD32F2F"/>
            <Setter Property="Foreground" Value="White"/>
        </DataTrigger>
        <DataTrigger Binding="{Binding Status}" Value="DefautMineur">
            <Setter Property="Background" Value="#FFC107"/>
            <Setter Property="Foreground" Value="Black"/>
        </DataTrigger>
        <DataTrigger Binding="{Binding Status}" Value="ManqueTraction">
            <Setter Property="Background" Value="Orange"/>
            <Setter Property="Foreground" Value="Black"/>
        </DataTrigger>
    </Style.Triggers>
</Style>
```

#### Règles de Coloration

| Statut | Couleur de Fond | Couleur de Texte | Code Couleur |
|--------|----------------|------------------|--------------|
| HS | 🔴 Rouge | Blanc | #FFD32F2F |
| DefautMineur | 🟡 Ambre | Noir | #FFC107 |
| ManqueTraction | 🟠 Orange | Noir | Orange |
| Ok | Aucune | Noir | - |

### 3. Amélioration de la Couleur Jaune

#### Problème
- **Ancienne couleur**: `Brushes.Yellow` (#FFFF00)
- **Problème**: Trop clair, difficile à lire sur fond blanc
- **Lisibilité**: ❌ Mauvaise

#### Solution
- **Nouvelle couleur**: Ambre (#FFC107)
- **Avantages**: 
  - Plus foncé que le jaune pur
  - Meilleure lisibilité
  - Toujours reconnaissable comme "jaune"
  - Pas confondu avec l'orange
- **Lisibilité**: ✅ Excellente

#### Code Mis à Jour
```csharp
// Dans StatutToBrushConverter.cs
LocomotiveStatus.DefautMineur => new SolidColorBrush(Color.FromRgb(255, 193, 7)), // #FFC107
```

Cette couleur est appliquée dans :
- Les tuiles de locomotives (via StatutToBrushConverter)
- La colonne Statut/Motif du TapisT13 (via MotifCellStyle)

## Classe T13Row Étendue

```csharp
private sealed class T13Row
{
    public string Locomotive { get; set; } = string.Empty;
    public string MaintenanceDate { get; set; } = string.Empty;
    public string Motif { get; set; } = string.Empty;          // Affiche info de statut
    public string LocHs { get; set; } = string.Empty;
    public string Report { get; set; } = string.Empty;
    public bool IsHs { get; set; }
    public bool IsNonHsOnLine { get; set; }
    public LocomotiveStatus Status { get; set; }                // NOUVEAU - Pour binding couleur
}
```

## Exemples d'Affichage

### Exemple 1: Locomotive HS
```
Locomotive: 1347
Statut/Motif: "Problème moteur" (fond rouge, texte blanc)
Loc HS: "FIZ 41836" (fond rouge)
```

### Exemple 2: Locomotive DefautMineur
```
Locomotive: 1334
Statut/Motif: "Phare avant cassé" (fond ambre #FFC107, texte noir)
Infos/Rapport: "DISPO FIZ" (sans couleur)
```

### Exemple 3: Locomotive ManqueTraction
```
Locomotive: 1335
Statut/Motif: "Manque traction" (fond orange, texte noir)
Infos/Rapport: "DISPO MUN" (sans couleur)
```

### Exemple 4: Locomotive Ok
```
Locomotive: 1336
Statut/Motif: (vide, sans couleur)
Infos/Rapport: "DISPO THL" (sans couleur)
```

## Comparaison Avant/Après

### Avant
| Locomotive | Statut/Motif | Couleur |
|-----------|--------------|---------|
| 1347 (HS) | "Problème moteur" | Aucune |
| 1334 (DefautMineur) | (vide) | Aucune |
| 1335 (ManqueTraction) | (vide) | Aucune |

### Après
| Locomotive | Statut/Motif | Couleur |
|-----------|--------------|---------|
| 1347 (HS) | "Problème moteur" | 🔴 Rouge |
| 1334 (DefautMineur) | "Phare avant cassé" | 🟡 Ambre (lisible) |
| 1335 (ManqueTraction) | "Manque traction" | 🟠 Orange |

## Avantages

### 1. Visibilité Améliorée
✅ Toutes les informations de statut importantes sont visibles d'un coup d'œil
✅ Coloration cohérente dans toute l'application

### 2. Lisibilité Accrue
✅ Couleur jaune remplacée par ambre (#FFC107) beaucoup plus lisible
✅ Contraste suffisant entre fond et texte pour tous les statuts

### 3. Information Complète
✅ Raison HS visible (HsReason)
✅ Description du défaut mineur visible (DefautInfo)
✅ Indication claire pour manque de traction

### 4. Cohérence Visuelle
✅ Même palette de couleurs que les tuiles de locomotives
✅ Rouge = HS (problème majeur)
✅ Ambre = DefautMineur (à vérifier)
✅ Orange = ManqueTraction (problème spécifique)

## Tests de Validation

### Test 1: Locomotive HS avec Raison
1. Créer une locomotive avec Status = HS et HsReason = "Test moteur"
2. Ouvrir TapisT13
3. ✅ Vérifier: colonne Statut/Motif affiche "Test moteur" sur fond rouge

### Test 2: Locomotive DefautMineur avec Info
1. Créer une locomotive avec Status = DefautMineur et DefautInfo = "Phare cassé"
2. Ouvrir TapisT13
3. ✅ Vérifier: colonne Statut/Motif affiche "Phare cassé" sur fond ambre (#FFC107)
4. ✅ Vérifier: couleur lisible et distincte du jaune pur

### Test 3: Locomotive ManqueTraction
1. Créer une locomotive avec Status = ManqueTraction
2. Ouvrir TapisT13
3. ✅ Vérifier: colonne Statut/Motif affiche "Manque traction" sur fond orange

### Test 4: Locomotive Ok
1. Créer une locomotive avec Status = Ok
2. Ouvrir TapisT13
3. ✅ Vérifier: colonne Statut/Motif est vide et sans couleur

### Test 5: Couleur Jaune dans l'Application
1. Créer une locomotive avec Status = DefautMineur
2. Vérifier la couleur dans :
   - Tuile principale (MainWindow)
   - Colonne Statut/Motif (TapisT13)
3. ✅ Vérifier: couleur ambre (#FFC107) partout, lisible et cohérente

## Fichiers Modifiés

### 1. TapisT13Window.xaml.cs
- Ligne 64-70: Logique motif pour tous les statuts
- Ligne 75: Ajout propriété Status à T13Row
- Ligne 266: Propriété Status dans classe T13Row

### 2. TapisT13Window.xaml
- Lignes 16-29: Nouveau style MotifCellStyle
- Ligne 65: Application du style à la colonne Statut/Motif

### 3. StatutToBrushConverter.cs
- Ligne 20: DefautMineur → Ambre (#FFC107) au lieu de Yellow
- Ligne 30: Legacy DefautMineur → Ambre (#FFC107) au lieu de Gold

## Notes Techniques

### Choix de la Couleur Ambre
- **#FFC107** (Amber 500 de Material Design)
- Utilisé dans de nombreuses applications modernes
- Excellent contraste sur fond blanc
- Clairement jaune mais pas éblouissant
- Distinct de l'orange (#FFA500)

### Alternative Testée
- **Gold (#FFD700)**: Trop similaire au jaune pur
- **DarkGoldenrod (#B8860B)**: Trop sombre, aspect marron

### Binding XAML
Le binding `{Binding Status}` dans les DataTriggers permet de :
- Réagir directement au statut de la locomotive
- Éviter la duplication de logique
- Faciliter la maintenance

## Conclusion

Ces modifications améliorent significativement l'expérience utilisateur en :
1. ✅ Affichant clairement toutes les informations de statut importantes
2. ✅ Utilisant une couleur jaune/ambre beaucoup plus lisible
3. ✅ Maintenant une cohérence visuelle dans toute l'application
4. ✅ Facilitant l'identification rapide des locomotives nécessitant attention

**Status: Prêt pour la production** 🚀
