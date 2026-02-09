# Ploco - Guide Utilisateur

## Introduction

Bienvenue dans **Ploco**, votre application de gestion visuelle de parc de locomotives.

Ce guide vous accompagne pour utiliser efficacement toutes les fonctionnalités de l'application.

---

## Démarrage Rapide

### Première Utilisation

1. **Lancer l'application** : Double-clic sur Ploco.exe
2. **Interface principale** : Vous voyez le canvas de tuiles à droite et la liste des locomotives à gauche
3. **Pools** : Deux pools sont disponibles - Sibelit (actif) et Lineas (réserve)

### Navigation

- **Liste de gauche** : Locomotives non assignées
- **Canvas central** : Tuiles représentant les lieux (dépôts, garages, lignes)
- **Menu supérieur** : Accès aux fonctionnalités principales

---

## Gestion des Locomotives

### Ajouter une Locomotive

1. Les locomotives sont chargées depuis la base de données au démarrage
2. Pour importer en masse : **Options > Import** (voir section Import)

### Déplacer une Locomotive

**Méthode 1 : Glisser-déposer**
1. Cliquez et maintenez sur une locomotive dans la liste
2. Glissez-la vers une voie sur une tuile
3. Relâchez pour la déposer

**Méthode 2 : Placement Prévisionnel** (Recommandé)
1. Clic droit sur une locomotive déjà placée
2. "Placement prévisionnel"
3. Sélectionnez une ligne de roulement
4. La locomotive devient bleue, une copie verte apparaît sur la ligne
5. Validez ou annulez le placement

### Retirer une Locomotive

- Glissez la locomotive depuis la tuile vers la liste de gauche
- Elle retourne dans les locomotives non assignées

### Changer de Pool

**Méthode 1 : Double-clic** (Rapide)
- Double-cliquez sur une locomotive dans la liste
- Elle bascule automatiquement entre Sibelit et Lineas

**Méthode 2 : Menu contextuel**
1. Clic droit sur la locomotive
2. "Swap de pool"

**Méthode 3 : Fenêtre de gestion**
- Menu **Gestion > Pools**
- Sélectionnez les locomotives à transférer

---

## Statuts des Locomotives

### Les 4 Statuts

#### ✅ OK (Vert)
- Locomotive opérationnelle
- Aucune information supplémentaire requise

#### 🟠 Manque de Traction (Orange)
- Traction réduite
- Sélectionner le pourcentage : 75%, 50%, ou 25%
- Commentaire optionnel pour décrire le problème

#### 🟡 Défaut Mineur (Jaune)
- Problème mineur nécessitant vérification
- **Description obligatoire** du défaut

#### 🔴 HS - Hors Service (Rouge)
- Locomotive non opérationnelle
- **Motif obligatoire**

### Modifier un Statut

1. Clic droit sur la locomotive
2. "Modifier le statut"
3. Sélectionnez le nouveau statut
4. Remplissez les champs obligatoires si nécessaire
5. Validez

---

## Import de Données

### Import en Masse depuis Excel

L'import permet de synchroniser rapidement les pools depuis une liste.

**Étapes** :

1. **Préparer les données**
   - Ouvrez Excel avec votre liste de numéros
   - Sélectionnez la colonne des numéros
   - Copiez (Ctrl+C)

2. **Lancer l'import**
   - Menu **Options > Import**
   - La fenêtre s'ouvre avec le contenu du presse-papier pré-rempli
   - Vérifiez les numéros

3. **Importer**
   - Cliquez sur "Importer Locomotives"
   - L'application synchronise automatiquement :
     - ✅ Locomotives listées → Ajoutées à Sibelit
     - ⬅️ Locomotives non listées → Retournées à Lineas
     - ↔️ Locomotives déjà dans Sibelit → Inchangées

4. **Résultat**
   - Un message affiche les statistiques :
     - Nombre ajoutées
     - Nombre retirées
     - Nombre inchangées

**Format accepté** : Un numéro par ligne

```
1310
1311
1312
1313
```

---

## Placement Prévisionnel

### Pourquoi Utiliser le Placement Prévisionnel ?

- **Planification** : Visualiser les futures affectations
- **Sécurité** : Pas de déplacement accidentel
- **Organisation** : Préparer plusieurs placements avant validation

### Comment l'Utiliser ?

#### 1. Activer le Mode Prévisionnel

1. La locomotive doit être assignée à une tuile (pas dans la liste)
2. Clic droit sur la locomotive
3. "Placement prévisionnel"
4. Sélectionnez une ligne de roulement dans la liste

**Résultat** :
- 🔵 Locomotive devient bleue dans sa tuile d'origine
- 🟢 Copie verte (fantôme) apparaît sur la ligne cible

#### 2. Valider le Placement

1. Clic droit sur la locomotive bleue
2. "Valider le placement prévisionnel"
3. La locomotive est déplacée réellement
4. Le fantôme disparaît

#### 3. Annuler le Placement

1. Clic droit sur la locomotive bleue
2. "Annuler le placement prévisionnel"
3. Tout revient à l'état initial
4. Le fantôme disparaît

### Gestion des Conflits

Si la ligne cible est occupée entre-temps :
- Un message vous demande confirmation
- Acceptez pour remplacer la locomotive existante
- Refusez pour annuler l'opération

---

## Gestion des Tuiles

### Types de Tuiles

#### Dépôts
- Voies principales
- Voies de sortie
- Stockage des locomotives

#### Voies de Garage
- Zones configurables
- Indicateurs de remplissage (BLOCK / BIF)

#### Arrêts de Ligne
- Lignes de roulement
- Informations train (numéro, heure, motif)

### Ajouter une Tuile

1. Bouton **"Ajouter un lieu"**
2. Sélectionnez le type (Dépôt, Garage, Ligne)
3. Configurez les paramètres
4. Cliquez sur le canvas pour placer

### Déplacer une Tuile

- Cliquez et glissez la tuile pour la déplacer

### Redimensionner une Tuile

- Utilisez la poignée en bas à droite de la tuile
- Glissez pour ajuster la taille

### Configurer une Tuile

1. Clic droit sur la tuile
2. Menu contextuel avec les options :
   - Ajouter voie
   - Ajouter zone
   - Ajouter sortie
   - Reset
   - Presets de garage

---

## Rapport TapisT13

### Accès

Menu **Vue > TapisT13** (ou raccourci si configuré)

### Contenu du Rapport

Le TapisT13 affiche l'état complet du parc :

#### Locomotives HS
- 🔴 Affichage rouge
- Format : "TileName TrainNumber"
- Apparaît dans les deux colonnes

#### Locomotives sur Ligne avec Train
- 🟢 Affichage vert
- Format : "TileName TrainNumber"
- Colonne rapport uniquement

#### Locomotives Disponibles
- Pas de couleur
- Format : "DISPO TileName"

#### Locomotives sur Ligne de Roulement
- Pas de couleur
- Format : Numéro seul (ex: "1103")

### Support du Placement Prévisionnel

Le rapport prend en compte le mode prévisionnel :
- Si locomotive en prévisionnel → Utilise la position du fantôme
- Affiche la future position planifiée

### Pourcentages de Traction

Les locomotives avec traction réduite affichent :
- 75%, 50%, ou 25%
- Visible directement dans le rapport

---

## Presets de Layout

### Sauvegarder un Preset

1. Organisez vos tuiles comme souhaité
2. Menu **Vue > Sauvegarder le preset**
3. Donnez un nom au preset
4. Validez

### Charger un Preset

1. Menu **Vue > Charger un preset**
2. Sélectionnez dans la liste
3. Le layout est restauré

### Supprimer un Preset

1. Menu **Vue > Gérer les presets**
2. Sélectionnez le preset
3. "Supprimer"

---

## Historique

### Consulter l'Historique

Menu **Gestion > Historique**

### Types d'Événements

- Affectations de locomotives
- Changements de statut
- Modifications de layout
- Transferts de pool

---

## Système de Logs

### Accès aux Logs

Menu **Options > Ouvrir les logs**

Le dossier s'ouvre : `%AppData%\Ploco\Logs\`

### Format des Logs

Fichiers journaliers : `ploco-YYYYMMDD.log`

### Contenu

- Démarrage/arrêt application
- Toutes les opérations importantes
- Erreurs et exceptions
- Imports et exports

### Rotation

- Conservation : 30 jours
- Suppression automatique des anciens logs

---

## Personnalisation

### Thème

**Changer le thème** :
1. Menu **Options > Thème**
2. Sélectionnez Mode Clair ou Mode Sombre

### Paramètres des Fenêtres

- Taille et position sauvegardées automatiquement
- Chaque fenêtre retrouve son état au prochain lancement

---

## Dépannage

### La liste de gauche n'est pas à jour

**Solution** : Menu Gestion > Rafraîchir

### Une locomotive ne se déplace pas

**Vérifications** :
- La voie cible a-t-elle de la place ?
- La locomotive est-elle un fantôme ? (non déplaçable)
- Vérifiez les logs pour les erreurs

### Problème de base de données

**Solution** :
1. Menu **Gestion > Base de données**
2. Vérifiez l'intégrité
3. Consultez les logs

### L'import ne fonctionne pas

**Vérifications** :
- Format correct : un numéro par ligne
- Numéros existent dans la base de données
- Consultez les logs pour détails

---

## Raccourcis et Astuces

### Raccourcis Utiles

- **Double-clic** : Transfert de pool instantané
- **Clic droit** : Menu contextuel
- **Glisser-déposer** : Déplacer locomotives et tuiles

### Astuces

1. **Utilisez le placement prévisionnel** pour éviter les erreurs
2. **Importez en masse** plutôt que de sélectionner manuellement
3. **Vérifiez le TapisT13** avant de valider les placements
4. **Sauvegardez des presets** pour vos layouts fréquents
5. **Consultez les logs** en cas de doute

### Workflow Efficace

1. **Import** : Synchroniser les pools
2. **Planification** : Placements prévisionnels
3. **Vérification** : Consulter TapisT13
4. **Validation** : Confirmer les placements
5. **Suivi** : Historique et logs

---

## Support

### En cas de Problème

1. **Consultez les logs** : Options > Ouvrir les logs
2. **Vérifiez l'historique** : Gestion > Historique
3. **Contactez le support** avec :
   - Description du problème
   - Fichiers de logs
   - Captures d'écran si possible

---

## Glossaire

- **Tuile** : Zone du canvas représentant un lieu (dépôt, garage, ligne)
- **Pool** : Groupe de locomotives (Sibelit actif, Lineas réserve)
- **Fantôme** : Copie verte d'une locomotive en placement prévisionnel
- **TapisT13** : Rapport d'état du parc
- **HS** : Hors Service
- **Preset** : Configuration sauvegardée du layout
- **Offset** : Décalage de position pour éviter le chevauchement

---

## Voir Aussi

- [FEATURES.md](FEATURES.md) - Guide détaillé des fonctionnalités
- [README.md](../README.md) - Vue d'ensemble du projet
- [CHANGELOG.md](../CHANGELOG.md) - Historique des modifications
- [RELEASE_NOTES.md](../RELEASE_NOTES.md) - Notes de version
