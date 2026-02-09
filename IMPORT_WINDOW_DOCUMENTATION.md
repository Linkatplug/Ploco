# Fenêtre Import de Données - Documentation

## Vue d'ensemble

Nouvelle fonctionnalité d'import accessible depuis le menu **Options > Import** permettant d'importer facilement des locomotives depuis le presse-papier avec synchronisation automatique des pools.

## Accès

**Menu:** Options > Import (première option)

## Fonctionnalités

### 1. Import de Locomotives ✅ Fonctionnel

Import de locomotives depuis le presse-papier avec synchronisation automatique entre les pools Sibelit et Lineas.

#### Format d'Entrée

Liste de numéros de locomotives, un par ligne:

```
1310
1311
1312
1313
1314
1315
1316
1317
```

Le format peut provenir de:
- Excel (copier colonne)
- Fichier texte
- Tout autre source avec numéros ligne par ligne

#### Logique de Synchronisation

La synchronisation est **bidirectionnelle** et **automatique**:

1. **Ajout à Sibelit**
   - Si locomotive existe dans la base
   - ET son numéro est dans la liste importée
   - ET elle n'est PAS déjà dans Sibelit
   - → **Ajoutée à Sibelit**

2. **Retour à Lineas**
   - Si locomotive est dans Sibelit
   - MAIS son numéro n'est PAS dans la liste importée
   - → **Retourne à Lineas**

3. **Inchangée**
   - Si locomotive est déjà dans Sibelit
   - ET son numéro est dans la liste importée
   - → **Reste inchangée**

#### Workflow

1. **Copier** les numéros de locomotives (Excel, texte, etc.)
   ```
   1310
   1311
   1312
   ```

2. **Ouvrir** Options > Import

3. **Zone pré-remplie** avec le contenu du presse-papier

4. **Vérifier** les numéros

5. **Cliquer** "Importer Locomotives"

6. **Voir résultat:**
   ```
   Import terminé!
   
   - 15 locomotive(s) ajoutée(s) à Sibelit
   - 3 locomotive(s) retournée(s) à Lineas
   - 5 locomotive(s) déjà dans Sibelit (inchangées)
   ```

7. **Fermer** la fenêtre

#### Validation

- Ignore les lignes vides
- Ignore les lignes non-numériques
- N'importe que les locomotives existantes dans la base
- Affiche un avertissement si aucun numéro valide

### 2. Import de Dates d'Entretien 🚧 En cours de développement

Bouton présent mais affiche un message "En cours de développement".

Fonctionnalité prévue pour une version future.

## Exemples

### Exemple 1: Import Initial

**Situation:**
- Base: 50 locomotives (toutes dans Lineas)
- Import: Liste de 25 numéros

**Action:**
1. Copier les 25 numéros
2. Options > Import
3. Importer

**Résultat:**
```
Import terminé!

- 25 locomotive(s) ajoutée(s) à Sibelit
- 0 locomotive(s) retournée(s) à Lineas
- 0 locomotive(s) déjà dans Sibelit
```

### Exemple 2: Mise à Jour

**Situation:**
- Sibelit contient: 1310, 1311, 1312, 1313, 1314 (5 locos)
- Import: 1310, 1312, 1315, 1316 (4 numéros)

**Action:**
1. Copier les 4 numéros
2. Options > Import
3. Importer

**Résultat:**
```
Import terminé!

- 2 locomotive(s) ajoutée(s) à Sibelit (1315, 1316)
- 2 locomotive(s) retournée(s) à Lineas (1311, 1313)
- 2 locomotive(s) déjà dans Sibelit (1310, 1312)
```

**État final Sibelit:** 1310, 1312, 1315, 1316

### Exemple 3: Aucun Changement

**Situation:**
- Sibelit contient: 1310, 1311, 1312
- Import: 1310, 1311, 1312 (mêmes numéros)

**Action:**
1. Copier les numéros
2. Options > Import
3. Importer

**Résultat:**
```
Import terminé!

- 0 locomotive(s) ajoutée(s) à Sibelit
- 0 locomotive(s) retournée(s) à Lineas
- 3 locomotive(s) déjà dans Sibelit
```

## Avantages

### Pour l'Utilisateur

✅ **Rapidité**: Copier/coller au lieu de sélection manuelle
✅ **Simplicité**: Format texte simple
✅ **Feedback**: Statistiques claires et détaillées
✅ **Sécurité**: Validation des données

### Pour la Gestion

✅ **Synchronisation automatique**: Plus besoin de gérer manuellement
✅ **Traçabilité**: Tous les imports sont loggés
✅ **Fiabilité**: Impossible d'oublier des locomotives
✅ **Extensibilité**: Prêt pour import dates

## Caractéristiques Techniques

### Fichiers

- **ImportWindow.xaml**: Interface utilisateur
- **ImportWindow.xaml.cs**: Logique métier

### Dépendances

- `WindowSettingsHelper`: Sauvegarde taille/position fenêtre
- `Logger`: Logging des opérations
- `LocomotiveModel`: Modèle de données

### Persistance

- Modifications sauvegardées automatiquement après import
- Position et taille de la fenêtre mémorisées

### Logging

Chaque opération est loggée:
```
[INFO] Opening Import window
[INFO] Locomotive 1310 ajoutée à Sibelit
[INFO] Locomotive 1315 retournée à Lineas
[INFO] Import locomotives: 15 ajoutées, 3 retirées, 5 inchangées
```

## Sécurité

### Validation

- ✅ Vérifie que le texte n'est pas vide
- ✅ Parse uniquement les numéros valides
- ✅ Ignore les lignes invalides
- ✅ N'importe que les locomotives existantes

### Gestion d'Erreurs

- Try/catch sur toutes les opérations
- Messages d'erreur clairs pour l'utilisateur
- Logging des erreurs pour diagnostic

## Future: Import Dates

### Format Prévu (à implémenter)

```
1310;2026-02-15
1311;2026-03-20
1312;2026-01-10
```

Format: `NuméroLoco;DateEntretien`

### Fonctionnalité Prévue

- Parse des dates depuis presse-papier
- Mise à jour des dates d'entretien
- Validation des dates
- Statistiques de mise à jour

## Tests de Validation

### Test 1: Import Basique
1. Copier: "1310\n1311\n1312"
2. Options > Import
3. Importer
4. ✅ Vérifier: 3 locomotives dans Sibelit

### Test 2: Synchronisation
1. État initial: Sibelit = {1310, 1311}
2. Importer: {1311, 1312}
3. ✅ Vérifier: Sibelit = {1311, 1312} (1310 retirée, 1312 ajoutée)

### Test 3: Validation
1. Copier: "abc\n\n1310\nxyz"
2. Importer
3. ✅ Vérifier: Seule 1310 importée

### Test 4: Vide
1. Copier: "" (vide)
2. Importer
3. ✅ Message: "Veuillez coller les numéros"

### Test 5: Bouton Dates
1. Cliquer "Importer Dates"
2. ✅ Message: "En cours de développement"

## Dépannage

### Problème: Zone de texte vide

**Solution:**
- Vérifier que le presse-papier contient du texte
- Coller manuellement (Ctrl+V) dans la zone

### Problème: Aucun changement après import

**Solution:**
- Vérifier que les numéros existent dans la base
- Vérifier le format (un numéro par ligne)
- Consulter les logs pour détails

### Problème: Fenêtre ne s'ouvre pas

**Solution:**
- Vérifier les logs
- Redémarrer l'application
- Vérifier les permissions

## Conclusion

La fenêtre Import de données simplifie grandement la gestion des pools de locomotives en permettant une synchronisation rapide et fiable depuis n'importe quelle source de données texte.

**Statut:** ✅ Prêt pour production

**Version:** 1.0

**Date:** 2026-02-09
