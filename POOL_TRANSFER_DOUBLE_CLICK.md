# Transfert de Locomotives par Double-Clic

## Vue d'ensemble

Cette fonctionnalité permet de transférer rapidement des locomotives entre les pools Sibelit et Lineas en utilisant le double-clic dans la fenêtre de gestion de parc.

## Fonctionnalité

### Comportement
- **Double-clic sur une locomotive dans le pool Sibelit (gauche)** → Transfert immédiat vers le pool Lineas (droite)
- **Double-clic sur une locomotive dans le pool Lineas (droite)** → Transfert immédiat vers le pool Sibelit (gauche)

### Caractéristiques
- ✅ **Transfert unique** : Seule la locomotive double-cliquée est transférée
- ✅ **Indépendant de la sélection** : Fonctionne même si plusieurs locomotives sont sélectionnées
- ✅ **Même logique** : Utilise le même mécanisme que les boutons ">>" et "<<"
- ✅ **Persistance** : Les changements sont automatiquement persistés via ObservableCollection
- ✅ **Compatible** : Ne casse pas la multi-sélection (Ctrl/Maj) ni les boutons existants

## Utilisation

### Méthode 1 : Double-clic (nouveau)
1. Ouvrir la fenêtre "Gérer les Locomotives"
2. Double-cliquer sur une locomotive dans n'importe quelle liste
3. La locomotive est transférée instantanément dans l'autre pool

### Méthode 2 : Boutons (existant)
1. Sélectionner une ou plusieurs locomotives (Ctrl/Maj pour multi-sélection)
2. Cliquer sur le bouton ">>" ou "<<"
3. Les locomotives sélectionnées sont transférées

## Implémentation Technique

### Fichiers Modifiés
- `PoolTransferWindow.xaml` : Ajout des handlers `MouseDoubleClick`
- `PoolTransferWindow.xaml.cs` : Implémentation des méthodes de transfert

### Code XAML
```xml
<ListBox x:Name="ListBoxSibelit"
         ...
         MouseDoubleClick="ListBoxSibelit_MouseDoubleClick">
```

```xml
<ListBox x:Name="ListBoxLineas"
         ...
         MouseDoubleClick="ListBoxLineas_MouseDoubleClick">
```

### Code C#

#### Méthodes de Transfert
```csharp
private void ListBoxSibelit_MouseDoubleClick(object sender, MouseButtonEventArgs e)
{
    var listBox = sender as System.Windows.Controls.ListBox;
    if (listBox == null) return;

    var item = GetItemUnderMouse(listBox, e);
    if (item is LocomotiveModel loco)
    {
        // Transfert vers Lineas
        loco.Pool = "Lineas";
        RefreshViews();
    }
}

private void ListBoxLineas_MouseDoubleClick(object sender, MouseButtonEventArgs e)
{
    var listBox = sender as System.Windows.Controls.ListBox;
    if (listBox == null) return;

    var item = GetItemUnderMouse(listBox, e);
    if (item is LocomotiveModel loco)
    {
        // Transfert vers Sibelit
        loco.Pool = "Sibelit";
        RefreshViews();
    }
}
```

#### Hit Testing (Détection Précise)
```csharp
private object? GetItemUnderMouse(System.Windows.Controls.ListBox listBox, MouseButtonEventArgs e)
{
    // Hit test pour obtenir l'élément sous la souris
    var mousePosition = e.GetPosition(listBox);
    var hitTestResult = System.Windows.Media.VisualTreeHelper.HitTest(listBox, mousePosition);
    
    if (hitTestResult != null)
    {
        // Remonter l'arbre visuel pour trouver le ListBoxItem
        var element = hitTestResult.VisualHit;
        while (element != null && element != listBox)
        {
            if (element is System.Windows.Controls.ListBoxItem listBoxItem)
            {
                return listBoxItem.Content; // Retourne la LocomotiveModel
            }
            element = System.Windows.Media.VisualTreeHelper.GetParent(element);
        }
    }

    return null;
}
```

### Points Clés de l'Implémentation

1. **Hit Testing Précis**
   - Utilise `VisualTreeHelper.HitTest` pour obtenir l'élément exact sous la souris
   - Remonte l'arbre visuel pour trouver le `ListBoxItem`
   - Retourne le `DataContext` (LocomotiveModel)

2. **Transfert Unitaire**
   - N'utilise pas `SelectedItems` (qui contiendrait toute la sélection)
   - Transfère uniquement l'item double-cliqué
   - Comportement prévisible et intuitif

3. **Réutilisation du Code**
   - Même logique que les boutons : `loco.Pool = "..."`
   - Même méthode de rafraîchissement : `RefreshViews()`
   - Garantit la cohérence du comportement

## Tests

### Scénarios de Test

#### Test 1 : Transfert Basique
1. Double-cliquer sur une locomotive dans le pool Sibelit
2. ✅ La locomotive apparaît dans le pool Lineas
3. ✅ Le compteur est mis à jour

#### Test 2 : Multi-Sélection Préservée
1. Sélectionner 3 locomotives dans le pool Sibelit (Ctrl+clic)
2. Double-cliquer sur UNE des locomotives sélectionnées
3. ✅ Seule la locomotive double-cliquée est transférée
4. ✅ Les 2 autres restent dans le pool Sibelit

#### Test 3 : Boutons Fonctionnent Toujours
1. Sélectionner plusieurs locomotives
2. Cliquer sur le bouton ">>"
3. ✅ Toutes les locomotives sélectionnées sont transférées

#### Test 4 : Bidirectionnel
1. Double-cliquer sur une locomotive dans le pool Lineas
2. ✅ La locomotive est transférée vers le pool Sibelit

#### Test 5 : Persistance
1. Transférer des locomotives par double-clic
2. Fermer la fenêtre
3. ✅ Les changements sont persistés (ObservableCollection)

## Avantages

### Utilisabilité
- ⚡ **Rapidité** : Transfert instantané sans sélection préalable
- 🎯 **Précision** : Transfert de l'élément exact sous la souris
- 🖱️ **Naturel** : Geste familier (double-clic = action)

### Technique
- 🔄 **Cohérence** : Même logique que les boutons existants
- 🛡️ **Robustesse** : Hit testing précis évite les erreurs
- 🔧 **Maintenabilité** : Code clair et réutilisable

### Compatibilité
- ✅ N'affecte pas la multi-sélection existante
- ✅ N'affecte pas les boutons existants
- ✅ Ajoute une option sans remplacer les existantes

## Notes Techniques

### WPF Visual Tree
Le hit testing utilise l'arbre visuel WPF :
```
ListBox
  └─ ListBoxItem (conteneur)
      └─ ContentPresenter
          └─ Border (DataTemplate)
              └─ TextBlock, Grid, etc.
```

La méthode remonte l'arbre pour trouver le `ListBoxItem`, qui contient la `LocomotiveModel` dans son `Content`.

### Performance
- Hit testing est très performant (opération native WPF)
- Pas d'impact sur les performances avec des milliers de locomotives
- Pas de boucle coûteuse, juste une recherche dans l'arbre visuel

## Évolutions Possibles

### Améliorations Futures
1. **Animation** : Ajouter une animation de transfert visuelle
2. **Feedback** : Son ou effet visuel lors du transfert
3. **Undo** : Possibilité d'annuler le dernier transfert
4. **Drag & Drop** : Compléter avec un système de glisser-déposer

### Extensibilité
Le code est facilement extensible pour :
- Ajouter plus de pools
- Ajouter des validations avant transfert
- Ajouter des logs/historique des transferts

## Conclusion

Cette fonctionnalité améliore significativement l'expérience utilisateur en permettant des transferts rapides et intuitifs, tout en préservant les fonctionnalités existantes et en suivant les mêmes patterns de code.
