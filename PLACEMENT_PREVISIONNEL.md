# Placement Prévisionnel (Forecast Placement) - Feature Documentation

## Vue d'ensemble

La fonctionnalité "Placement prévisionnel" permet de planifier le déplacement d'une locomotive vers une ligne de roulement sans l'y déplacer physiquement immédiatement. Cette fonctionnalité ajoute un état de prévision avec des indicateurs visuels clairs :

- **Bleu** : Locomotive dans sa tuile d'origine (mode prévisionnel actif)
- **Vert** : Copie fantôme sur la ligne de roulement cible (placement prévu)

## Fonctionnement

### 1. Activation du mode prévisionnel

**Action** : Clic droit sur une locomotive dans une tuile → "Placement prévisionnel"

**Conditions** :
- La locomotive doit être assignée à une tuile (pas dans la liste des locomotives non assignées)
- La locomotive ne doit pas déjà être en mode prévisionnel

**Comportement** :
1. Une boîte de dialogue s'ouvre pour sélectionner une ligne de roulement
2. Les lignes disponibles sont affichées, triées par disponibilité (libres en premier)
3. Une fois la ligne sélectionnée :
   - La locomotive dans sa tuile d'origine devient **BLEUE**
   - Une copie fantôme **VERTE** apparaît sur la ligne de roulement sélectionnée

### 2. Annulation du placement prévisionnel

**Action** : Clic droit sur la locomotive **BLEUE** → "Annuler le placement prévisionnel"

**Comportement** :
1. La copie fantôme verte est supprimée de la ligne de roulement
2. La locomotive dans la tuile retrouve sa couleur normale (basée sur son statut)
3. Tous les indicateurs de prévision sont réinitialisés
4. **Aucune autre donnée n'est modifiée**

### 3. Validation du placement prévisionnel

**Action** : Clic droit sur la locomotive **BLEUE** → "Valider le placement prévisionnel"

**Comportement** :
1. La copie fantôme verte est supprimée
2. La locomotive réelle est retirée de sa tuile d'origine
3. La locomotive réelle est placée sur la ligne de roulement cible
4. Le statut et les autres propriétés de la locomotive sont préservés

**Cas particulier** : Si la ligne cible a été occupée entre-temps par une autre locomotive réelle :
- Une confirmation est demandée à l'utilisateur
- Si accepté, la locomotive existante est retirée et remplacée

## Architecture technique

### Modèle de données

**Nouvelles propriétés dans `LocomotiveModel`** :
```csharp
public bool IsForecastOrigin { get; set; }                    // True si la loco est en mode prévisionnel (bleue)
public int? ForecastTargetRollingLineTrackId { get; set; }    // ID de la ligne de roulement cible
public bool IsForecastGhost { get; set; }                     // True si c'est une copie fantôme (verte)
public int? ForecastSourceLocomotiveId { get; set; }          // ID de la loco source (pour les ghosts)
```

**Ghost ID** : Les locomotives fantômes ont un ID unique négatif calculé : `-100000 - sourceId`

### Système de couleurs (WPF DataTriggers)

**IMPORTANT** : Le système de couleurs existant (`StatutToBrushConverter`) est **PRÉSERVÉ**. Les couleurs de prévision sont une **surcouche visuelle** via WPF DataTriggers.

**Style appliqué** : `LocomotiveBorderStyle`
```xml
<Style x:Key="LocomotiveBorderStyle" TargetType="Border">
    <!-- Binding d'origine préservé -->
    <Setter Property="Background" Value="{Binding Status, Converter={StaticResource StatutToBrushConverter}}"/>
    <Style.Triggers>
        <!-- Forecast origin: Blue (priorité haute) -->
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

La priorité des couleurs est :
1. **IsForecastOrigin** → Bleu (locomotive d'origine en mode prévisionnel)
2. **IsForecastGhost** → Vert (copie fantôme sur la ligne)
3. **Status** → Couleurs normales via `StatutToBrushConverter` (Rouge/Orange/Vert selon le statut)

**Important** : 
- Le statut de la locomotive n'est JAMAIS modifié pour obtenir une couleur
- Les DataTriggers sont évalués en priorité et surchargent le Background si nécessaire
- Quand on annule, la couleur revient automatiquement au statut (pas besoin de restaurer)

### Protection Drag & Drop

Les locomotives fantômes ne peuvent pas être déplacées :
- Blocage au niveau de `PreviewMouseMove` (empêche le démarrage du drag)
- Blocage au niveau de `DragOver` (refuse le drop)
- Blocage au niveau de `Drop` avec message d'erreur explicite
- Impossible d'échanger (swap) avec une locomotive fantôme

### Persistance

Les locomotives fantômes ne sont **jamais sauvegardées** dans la base de données :
- Elles sont filtrées lors de l'appel à `PersistState()`
- Seules les locomotives réelles et leurs états de prévision sont sauvegardés
- Les propriétés `IsForecastOrigin`, `ForecastTargetRollingLineTrackId`, `ForecastSourceLocomotiveId` sont persistées
- Les ghosts ne sont **pas** dans la collection `_locomotives`, uniquement dans `track.Locomotives`

## Scénarios d'utilisation

### Scénario 1 : Planification simple
1. Locomotive 1234 est dans le Dépôt A
2. Opérateur active "Placement prévisionnel" → Sélectionne ligne 1105
3. Loco 1234 devient bleue dans Dépôt A, une copie verte apparaît sur ligne 1105
4. Plus tard, opérateur valide → Loco 1234 est déplacée sur ligne 1105

### Scénario 2 : Changement d'avis
1. Locomotive 1234 est en prévision sur ligne 1105 (bleue dans tuile, verte sur ligne)
2. Opérateur annule le placement prévisionnel
3. Loco 1234 retrouve sa couleur normale dans la tuile, fantôme disparaît

### Scénario 3 : Ligne occupée entre-temps
1. Locomotive 1234 en prévision sur ligne 1105
2. Une autre loco 5678 est manuellement placée sur ligne 1105
3. Opérateur valide la prévision de 1234
4. Système demande confirmation (ligne occupée par 5678)
5. Si confirmé : 5678 est retirée, 1234 prend sa place

## Compatibilité

### Fonctionnalités préservées
✅ Système de statuts (Ok/ManqueTraction/HS) continue de fonctionner
✅ Drag & drop des locomotives réelles fonctionne normalement
✅ Swap entre pools fonctionne
✅ Modification du statut fonctionne
✅ Historique des actions est maintenu

### Restrictions
❌ Les locomotives fantômes ne peuvent pas être déplacées
❌ Impossible d'échanger avec une locomotive fantôme
❌ Une locomotive ne peut avoir qu'un seul placement prévisionnel à la fois

## Points d'attention pour les développeurs

1. **Toujours vérifier `IsForecastGhost`** avant d'autoriser des opérations sur une locomotive
2. **Ne jamais persister les fantômes** - toujours filtrer lors de la sauvegarde
3. **Préserver le statut original** via `OriginalStatus` pour la restauration
4. **Gérer les cas de lignes occupées** lors de la validation
5. **Maintenir la cohérence** entre l'origine bleue et le fantôme vert (même numéro, mêmes propriétés de base)

## Tests recommandés

1. ✅ Activation du mode prévisionnel
2. ✅ Annulation et restauration correcte de la couleur
3. ✅ Validation et déplacement effectif
4. ✅ Tentative de drag d'un fantôme (doit être bloquée)
5. ✅ Persistance et rechargement (fantômes non sauvegardés)
6. ✅ Changement de statut d'une loco en prévision
7. ✅ Validation sur ligne devenue occupée
8. ✅ Multiple locomotives en prévision simultanément
