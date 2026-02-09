# Commentaire et Pourcentage pour Manque de Traction

## Vue d'ensemble

Cette fonctionnalité permet d'ajouter un commentaire optionnel lors de la définition du statut "Manque de traction" et affiche le pourcentage de traction (au lieu de "Manque traction") dans le Tapis T13.

### Problème Résolu

**Avant:**
- ❌ Pas de champ pour saisir un commentaire dans le dialogue de statut
- ❌ TapisT13 affichait toujours "Manque traction" (pas d'information sur le pourcentage)

**Après:**
- ✅ Champ commentaire optionnel dans le dialogue "Manque de traction"
- ✅ TapisT13 affiche le pourcentage exact (ex: "75%") avec le commentaire si présent

## Modifications Apportées

### 1. Modèle de Données (DomainModels.cs)

**Nouvelle propriété:** `TractionInfo`

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

Cette propriété stocke le commentaire optionnel saisi par l'utilisateur pour décrire le problème de traction.

### 2. Interface Utilisateur (StatusDialog.xaml)

**Ajout dans le TractionPanel:**

```xml
<StackPanel x:Name="TractionPanel" Visibility="Collapsed" Margin="0,0,0,12">
    <TextBlock Text="Nombre de moteurs HS sur 4" FontWeight="SemiBold"/>
    <TextBox x:Name="TractionMotorsText" Margin="0,6,0,12"/>
    <TextBlock Text="Commentaire (optionnel)" FontWeight="SemiBold"/>
    <TextBox x:Name="TractionInfoText" Margin="0,6,0,4" Height="60" 
             AcceptsReturn="True" TextWrapping="Wrap"/>
    <TextBlock x:Name="TractionHint" Foreground="SlateGray"/>
</StackPanel>
```

**Caractéristiques:**
- Zone de texte multi-ligne (60px de hauteur)
- Retour à la ligne automatique
- Retour chariot accepté (AcceptsReturn="True")
- **Optionnel** - pas de validation requise

### 3. Logique du Dialogue (StatusDialog.xaml.cs)

**Chargement du commentaire:**

```csharp
TractionInfoText.Text = locomotive.TractionInfo ?? string.Empty;
```

**Sauvegarde lors de la validation:**

```csharp
if (status == LocomotiveStatus.ManqueTraction)
{
    // ... validation du nombre de moteurs ...
    
    _locomotive.TractionPercent = MotorsToTractionPercent(motorsHs);
    _locomotive.TractionInfo = TractionInfoText.Text.Trim();  // ← NOUVEAU
    _locomotive.HsReason = null;
    _locomotive.DefautInfo = null;
}
else
{
    _locomotive.TractionPercent = null;
    _locomotive.TractionInfo = null;  // ← Effacé si autre statut
}
```

### 4. Affichage dans TapisT13 (TapisT13Window.xaml.cs)

**Nouvelle méthode pour formater l'affichage:**

```csharp
private static string FormatTractionMotif(LocomotiveModel loco)
{
    if (!loco.TractionPercent.HasValue)
        return string.Empty;
    
    var percent = $"{loco.TractionPercent}%";
    
    if (!string.IsNullOrWhiteSpace(loco.TractionInfo))
    {
        return $"{percent} {loco.TractionInfo}";
    }
    
    return percent;
}
```

**Utilisation dans LoadRows:**

```csharp
var motif = loco.Status switch
{
    LocomotiveStatus.HS => loco.HsReason ?? string.Empty,
    LocomotiveStatus.DefautMineur => loco.DefautInfo ?? string.Empty,
    LocomotiveStatus.ManqueTraction => FormatTractionMotif(loco),  // ← NOUVEAU
    _ => string.Empty
};
```

### 5. Persistance (PlocoRepository.cs)

**Ajout de la colonne:**

```csharp
EnsureColumn(connection, "locomotives", "traction_info", "TEXT");
```

**Chargement depuis la base:**

```csharp
command.CommandText = "SELECT id, series_id, number, status, pool, traction_percent, 
                       hs_reason, defaut_info, traction_info, maintenance_date 
                       FROM locomotives;";
// ...
TractionInfo = reader.IsDBNull(8) ? null : reader.GetString(8),
```

**Sauvegarde dans la base:**

```csharp
command.CommandText = "INSERT INTO locomotives 
    (series_id, number, status, pool, traction_percent, 
     hs_reason, defaut_info, traction_info, maintenance_date) 
    VALUES ($seriesId, $number, $status, $pool, $traction, 
            $reason, $defaut, $tractionInfo, $maintenance);";
// ...
command.Parameters.AddWithValue("$tractionInfo", 
    string.IsNullOrWhiteSpace(loco.TractionInfo) ? DBNull.Value : loco.TractionInfo);
```

## Calcul du Pourcentage de Traction

Le pourcentage de traction est calculé automatiquement en fonction du nombre de moteurs HS:

| Moteurs HS | Pourcentage | Affichage |
|------------|-------------|-----------|
| 1 sur 4 | 75% | "75%" ou "75% [commentaire]" |
| 2 sur 4 | 50% | "50%" ou "50% [commentaire]" |
| 3 sur 4 | 25% | "25%" ou "25% [commentaire]" |
| 4 sur 4 | - | Non autorisé (statut HS obligatoire) |

## Exemples d'Utilisation

### Exemple 1: Sans commentaire

**Saisie:**
- Statut: Manque de traction
- Nombre de moteurs HS: 1
- Commentaire: (vide)

**Résultat dans TapisT13:**
```
Statut/Motif: 75% 🟠
```

### Exemple 2: Avec commentaire court

**Saisie:**
- Statut: Manque de traction
- Nombre de moteurs HS: 2
- Commentaire: "Moteur avant gauche défaillant"

**Résultat dans TapisT13:**
```
Statut/Motif: 50% Moteur avant gauche défaillant 🟠
```

### Exemple 3: Avec commentaire détaillé

**Saisie:**
- Statut: Manque de traction
- Nombre de moteurs HS: 3
- Commentaire: "Vérifier circuits électriques - problème de connexion sur 3 moteurs"

**Résultat dans TapisT13:**
```
Statut/Motif: 25% Vérifier circuits électriques - problème de connexion sur 3 moteurs 🟠
```

## Comparaison Avant/Après

### Avant l'Implémentation

**Dialogue de statut:**
```
┌─────────────────────────────────┐
│ Manque de traction              │
│                                  │
│ Nombre de moteurs HS sur 4      │
│ [____]                          │
│                                  │
│ 1 moteur HS = 75% · 2...        │
└─────────────────────────────────┘
```

**TapisT13:**
```
| Locomotive | Statut/Motif      |
|------------|-------------------|
| 1334       | Manque traction   |
| 1335       | Manque traction   |
```

### Après l'Implémentation

**Dialogue de statut:**
```
┌─────────────────────────────────┐
│ Manque de traction              │
│                                  │
│ Nombre de moteurs HS sur 4      │
│ [__1__]                         │
│                                  │
│ Commentaire (optionnel)         │
│ ┌─────────────────────────────┐ │
│ │ Moteur avant gauche         │ │
│ │ défaillant                  │ │
│ └─────────────────────────────┘ │
│                                  │
│ 1 moteur HS = 75% · 2...        │
└─────────────────────────────────┘
```

**TapisT13:**
```
| Locomotive | Statut/Motif                              |
|------------|-------------------------------------------|
| 1334       | 75% Moteur avant gauche défaillant        |
| 1335       | 50%                                       |
```

## Avantages

### Pour les Opérateurs

✅ **Visibilité immédiate**: Le pourcentage indique clairement la capacité de traction
✅ **Priorisation facile**: 25% est plus urgent que 75%
✅ **Contexte disponible**: Le commentaire fournit des détails supplémentaires

### Pour la Maintenance

✅ **Détails techniques**: Le commentaire peut contenir des informations précises
✅ **Historique**: Les commentaires sont sauvegardés dans la base de données
✅ **Planification**: Meilleure identification des interventions à effectuer

### Pour la Gestion

✅ **Suivi précis**: Pourcentages exacts de capacité de traction
✅ **Statistiques**: Possibilité d'analyser les problèmes récurrents
✅ **Reporting**: Informations détaillées pour les rapports

## Tests de Validation

### Test 1: Commentaire optionnel
- ✅ Définir ManqueTraction avec 1 moteur HS, sans commentaire
- ✅ Vérifier que TapisT13 affiche "75%"

### Test 2: Commentaire présent
- ✅ Définir ManqueTraction avec 2 moteurs HS
- ✅ Saisir commentaire "Moteur avant gauche"
- ✅ Vérifier que TapisT13 affiche "50% Moteur avant gauche"

### Test 3: Changement de statut
- ✅ Définir ManqueTraction avec commentaire
- ✅ Changer vers statut OK
- ✅ Vérifier que le commentaire est effacé

### Test 4: Persistance
- ✅ Définir ManqueTraction avec commentaire
- ✅ Sauvegarder l'état
- ✅ Redémarrer l'application
- ✅ Vérifier que le commentaire est toujours présent

### Test 5: Commentaire multi-ligne
- ✅ Définir ManqueTraction
- ✅ Saisir commentaire sur plusieurs lignes
- ✅ Vérifier que l'affichage est correct (sur une seule ligne dans TapisT13)

## Notes Techniques

### Pourquoi le commentaire est optionnel?

Le commentaire est optionnel car:
1. Le pourcentage seul est suffisant pour comprendre l'état
2. L'opérateur peut ne pas avoir d'information détaillée au moment de la saisie
3. Flexibilité d'utilisation selon les situations

### Gestion du texte multi-ligne

Le commentaire peut être saisi sur plusieurs lignes dans le dialogue, mais il est affiché sur une seule ligne dans TapisT13. C'est un choix délibéré pour:
- Maintenir la lisibilité du tableau
- Éviter les cellules de hauteur variable
- Garder une présentation compacte

### Couleur orange

Le statut ManqueTraction s'affiche toujours avec un fond orange (🟠) dans la colonne Statut/Motif, que le commentaire soit présent ou non.

## Fichiers Modifiés

1. **Ploco/Models/DomainModels.cs**
   - Ajout de la propriété `TractionInfo`

2. **Ploco/Dialogs/StatusDialog.xaml**
   - Ajout du TextBox pour le commentaire

3. **Ploco/Dialogs/StatusDialog.xaml.cs**
   - Chargement et sauvegarde du TractionInfo

4. **Ploco/Dialogs/TapisT13Window.xaml.cs**
   - Méthode `FormatTractionMotif` pour formater l'affichage
   - Modification du switch pour ManqueTraction

5. **Ploco/Data/PlocoRepository.cs**
   - Ajout de la colonne `traction_info`
   - Chargement et sauvegarde dans la base de données

## Conclusion

Cette fonctionnalité améliore significativement la gestion du statut "Manque de traction" en:
- Fournissant une information précise (pourcentage)
- Permettant d'ajouter des détails contextuels (commentaire optionnel)
- Facilitant la priorisation et le suivi des locomotives

Le pourcentage remplace avantageusement le texte générique "Manque traction" et offre une meilleure visibilité de l'état réel de la locomotive.
