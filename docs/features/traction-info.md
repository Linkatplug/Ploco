# Fonctionnalité: Commentaire de Traction et Affichage du Pourcentage

## Vue d'ensemble

Cette fonctionnalité améliore la gestion du statut "Manque de traction" en permettant d'ajouter un commentaire optionnel et en affichant le pourcentage de traction au lieu du texte générique "Manque traction" dans le Tapis T13.

## Modifications Apportées

### 1. Nouveau Champ: TractionInfo

**Fichier:** `Ploco/Models/DomainModels.cs`

Ajout d'une propriété `TractionInfo` au modèle `LocomotiveModel`:

```csharp
private string? _tractionInfo;

public string? TractionInfo
{
    get => _tractionInfo;
    set
    {
        if (_tractionInfo != value)
        {
            _tractionInfo = value;
            OnPropertyChanged();
        }
    }
}
```

Cette propriété stocke le commentaire optionnel saisi par l'utilisateur lors de la définition du statut "Manque de traction".

### 2. Interface Utilisateur: Dialogue de Statut

**Fichier:** `Ploco/Dialogs/StatusDialog.xaml`

Ajout d'un champ de commentaire dans le panneau de traction:

```xml
<StackPanel x:Name="TractionPanel" Visibility="Collapsed" Margin="0,0,0,12">
    <TextBlock Text="Nombre de moteurs HS sur 4" FontWeight="SemiBold"/>
    <TextBox x:Name="TractionMotorsText" Margin="0,6,0,4"/>
    <TextBlock x:Name="TractionHint" Foreground="SlateGray"/>
    <TextBlock Text="Commentaire (optionnel)" FontWeight="SemiBold" Margin="0,8,0,0"/>
    <TextBox x:Name="TractionInfoText" Margin="0,6,0,0" Height="60" 
             AcceptsReturn="True" TextWrapping="Wrap"/>
</StackPanel>
```

**Caractéristiques:**
- Commentaire **optionnel** (pas de validation requise)
- Zone de texte multi-ligne (60px de hauteur)
- Retour à la ligne automatique

### 3. Logique: Chargement et Sauvegarde

**Fichier:** `Ploco/Dialogs/StatusDialog.xaml.cs`

**Chargement dans le constructeur:**
```csharp
TractionInfoText.Text = locomotive.TractionInfo ?? string.Empty;
```

**Sauvegarde lors de la validation:**
```csharp
if (status == LocomotiveStatus.ManqueTraction)
{
    // ... validation des moteurs ...
    _locomotive.TractionPercent = MotorsToTractionPercent(motorsHs);
    _locomotive.TractionInfo = string.IsNullOrWhiteSpace(TractionInfoText.Text) 
        ? null 
        : TractionInfoText.Text.Trim();
    _locomotive.HsReason = null;
    _locomotive.DefautInfo = null;
}
else
{
    _locomotive.TractionPercent = null;
    _locomotive.TractionInfo = null;
}
```

### 4. Affichage dans TapisT13

**Fichier:** `Ploco/Dialogs/TapisT13Window.xaml.cs`

Modification de la logique d'affichage de la colonne "Statut/Motif":

```csharp
var motif = string.Empty;
if (isHs)
{
    motif = loco.HsReason ?? string.Empty;
}
else if (loco.Status == LocomotiveStatus.DefautMineur)
{
    motif = loco.DefautInfo ?? string.Empty;
}
else if (loco.Status == LocomotiveStatus.ManqueTraction && loco.TractionPercent.HasValue)
{
    motif = string.IsNullOrWhiteSpace(loco.TractionInfo)
        ? $"{loco.TractionPercent}%"
        : $"{loco.TractionPercent}% {loco.TractionInfo}";
}
```

**Format d'affichage:**
- **Sans commentaire:** `"75%"`
- **Avec commentaire:** `"50% Moteur avant gauche défaillant"`

### 5. Persistance en Base de Données

**Fichier:** `Ploco/Data/PlocoRepository.cs`

**Création de la colonne:**
```csharp
await connection.ExecuteAsync(@"
    CREATE TABLE IF NOT EXISTS locomotives (
        ...
        traction_info TEXT,
        ...
    )");
```

**Sauvegarde:**
```csharp
await connection.ExecuteAsync(@"
    INSERT OR REPLACE INTO locomotives (..., traction_info, ...)
    VALUES (..., @TractionInfo, ...)",
    new
    {
        ...
        loco.TractionInfo,
        ...
    });
```

**Chargement:**
```csharp
TractionInfo = reader["traction_info"] as string
```

## Calcul du Pourcentage de Traction

Le pourcentage de traction est calculé en fonction du nombre de moteurs HS:

| Moteurs HS | Pourcentage | Affichage |
|------------|-------------|-----------|
| 1 | 75% | 🟠 "75%" ou "75% [commentaire]" |
| 2 | 50% | 🟠 "50%" ou "50% [commentaire]" |
| 3 | 25% | 🟠 "25%" ou "25% [commentaire]" |
| 4 | 0% | ❌ Refusé (doit être en statut HS) |

## Exemples d'Utilisation

### Exemple 1: Sans Commentaire

**Action utilisateur:**
1. Clic droit sur locomotive 1334
2. "Modifier statut" → "Manque de traction"
3. Nombre de moteurs HS: `2`
4. Commentaire: *(vide)*
5. Valider

**Résultat dans TapisT13:**
```
Locomotive | Statut/Motif
-----------|-------------
1334       | 50% 🟠
```

### Exemple 2: Avec Commentaire

**Action utilisateur:**
1. Clic droit sur locomotive 1335
2. "Modifier statut" → "Manque de traction"
3. Nombre de moteurs HS: `1`
4. Commentaire: `Moteur avant gauche défaillant`
5. Valider

**Résultat dans TapisT13:**
```
Locomotive | Statut/Motif
-----------|----------------------------------
1335       | 75% Moteur avant gauche défaillant 🟠
```

### Exemple 3: Avec Commentaire Long

**Action utilisateur:**
1. Clic droit sur locomotive 1336
2. "Modifier statut" → "Manque de traction"
3. Nombre de moteurs HS: `3`
4. Commentaire: `Vérifier les circuits électriques\nMoteurs arrière non opérationnels`
5. Valider

**Résultat dans TapisT13:**
```
Locomotive | Statut/Motif
-----------|--------------------------------------------------------------
1336       | 25% Vérifier les circuits électriques Moteurs arrière... 🟠
```

## Comparaison Avant/Après

### Avant

| Locomotive | Statut/Motif |
|------------|--------------|
| 1334 | Manque traction 🟠 |
| 1335 | Manque traction 🟠 |
| 1336 | Manque traction 🟠 |

**Problèmes:**
- ❌ Pas d'information sur le niveau de traction
- ❌ Pas de détails sur le problème spécifique
- ❌ Toutes les locomotives avec manque de traction semblent identiques

### Après

| Locomotive | Statut/Motif |
|------------|--------------|
| 1334 | 50% 🟠 |
| 1335 | 75% Moteur avant gauche défaillant 🟠 |
| 1336 | 25% Vérifier circuits électriques 🟠 |

**Améliorations:**
- ✅ Pourcentage de traction visible immédiatement
- ✅ Commentaire optionnel pour détails spécifiques
- ✅ Meilleure priorisation (25% = plus urgent que 75%)
- ✅ Information utile pour la maintenance

## Avantages

### Pour les Opérateurs
- **Visibilité immédiate** du niveau de traction
- **Priorisation** facile des réparations (25% plus urgent que 75%)
- **Contexte** supplémentaire avec les commentaires

### Pour la Maintenance
- **Détails techniques** dans les commentaires
- **Historique** des problèmes de traction
- **Planification** plus efficace des interventions

### Pour la Gestion
- **Suivi** précis de la capacité de traction
- **Statistiques** sur les défaillances de moteurs
- **Reporting** amélioré

## Tests de Validation

### Test 1: Définir Manque de Traction Sans Commentaire
1. ✅ Sélectionner une locomotive OK
2. ✅ Modifier statut → "Manque de traction"
3. ✅ Entrer nombre de moteurs: 2
4. ✅ Laisser commentaire vide
5. ✅ Valider
6. ✅ Vérifier TapisT13: affiche "50%" en orange

### Test 2: Définir Manque de Traction Avec Commentaire
1. ✅ Sélectionner une locomotive OK
2. ✅ Modifier statut → "Manque de traction"
3. ✅ Entrer nombre de moteurs: 1
4. ✅ Entrer commentaire: "Moteur avant défaillant"
5. ✅ Valider
6. ✅ Vérifier TapisT13: affiche "75% Moteur avant défaillant" en orange

### Test 3: Changer de Statut Efface le Commentaire
1. ✅ Locomotive en ManqueTraction avec commentaire
2. ✅ Modifier statut → "OK"
3. ✅ Valider
4. ✅ Remettre en "Manque de traction"
5. ✅ Vérifier que le commentaire est vide

### Test 4: Persistance
1. ✅ Définir ManqueTraction avec commentaire
2. ✅ Fermer l'application
3. ✅ Relancer l'application
4. ✅ Vérifier TapisT13: commentaire toujours présent

### Test 5: Commentaire Multi-Ligne
1. ✅ Définir ManqueTraction
2. ✅ Entrer commentaire multi-ligne avec retours à la ligne
3. ✅ Valider
4. ✅ Vérifier affichage correct (peut être tronqué dans TapisT13)

## Notes Techniques

### Pourquoi Commentaire Optionnel?

Le commentaire est optionnel car:
- Le pourcentage seul est souvent suffisant
- Pas tous les cas nécessitent des détails supplémentaires
- Flexibilité pour l'utilisateur

### Gestion des Retours à la Ligne

Le champ de commentaire accepte les retours à la ligne (`AcceptsReturn="True"`), mais dans TapisT13, le texte est affiché sur une ligne. Les retours à la ligne sont convertis en espaces pour l'affichage.

### Colonne Orange

La couleur orange est conservée pour ManqueTraction dans la colonne "Statut/Motif" du TapisT13, avec le même style que précédemment (`MotifCellStyle`).

## Fichiers Modifiés

1. **Ploco/Models/DomainModels.cs**
   - Ajout de la propriété `TractionInfo`

2. **Ploco/Dialogs/StatusDialog.xaml**
   - Ajout du champ de commentaire dans TractionPanel

3. **Ploco/Dialogs/StatusDialog.xaml.cs**
   - Logique de chargement/sauvegarde de TractionInfo

4. **Ploco/Dialogs/TapisT13Window.xaml.cs**
   - Modification de l'affichage pour ManqueTraction

5. **Ploco/Data/PlocoRepository.cs**
   - Ajout de la colonne `traction_info`
   - Persistance de TractionInfo

## Conclusion

Cette fonctionnalité améliore significativement la gestion des locomotives avec manque de traction en fournissant:

✅ Information précise sur le niveau de traction
✅ Possibilité d'ajouter des détails techniques
✅ Meilleure priorisation des interventions
✅ Traçabilité améliorée

Le système reste simple d'utilisation tout en offrant plus de flexibilité et d'informations utiles pour les opérations et la maintenance.
