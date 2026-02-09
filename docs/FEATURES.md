# Ploco - Guide des Fonctionnalités

## Vue d'ensemble

Ce document regroupe toutes les fonctionnalités principales de l'application Ploco.

---

## 🔵 Placement Prévisionnel (Forecast Placement)

### Description
Permet de planifier le déplacement d'une locomotive vers une ligne de roulement sans l'y déplacer physiquement immédiatement.

### Utilisation

1. **Activation** : Clic droit sur une locomotive → "Placement prévisionnel"
2. **Sélection** : Choisir une ligne de roulement dans la liste
3. **Indicateurs visuels** :
   - 🔵 **Bleu** : Locomotive dans sa tuile d'origine (en attente)
   - 🟢 **Vert** : Copie fantôme sur la ligne cible
4. **Actions** :
   - **Valider** : Déplace réellement la locomotive
   - **Annuler** : Réinitialise tout

### Avantages
- ✅ Planification logistique facilitée
- ✅ Visualisation claire des futures affectations
- ✅ Aucun risque de déplacement accidentel
- ✅ Gestion automatique des conflits

---

## 📦 Import de Données par Lot

### Description
Synchronise les pools de locomotives en un seul clic depuis le presse-papier (Excel, CSV, etc.).

### Utilisation

1. **Copier** une liste de numéros de locomotives depuis Excel
2. **Menu** : Options > Import
3. **Coller** et cliquer sur "Importer Locomotives"
4. **Résultats** : Statistiques détaillées des modifications

### Logique de Synchronisation

- **Ajout à Sibelit** : Locomotives listées + non présentes dans Sibelit
- **Retour à Lineas** : Locomotives dans Sibelit mais non listées
- **Inchangées** : Locomotives déjà dans Sibelit et listées

### Avantages
- ✅ Rapidité : Copier/coller au lieu de sélection manuelle
- ✅ Fiabilité : Aucun oubli possible
- ✅ Simplicité : Format texte simple
- ✅ Feedback clair avec statistiques

---

## 🟡 Statut "Défaut Mineur"

### Description
Statut intermédiaire entre "OK" et "HS" pour marquer les locomotives nécessitant vérification.

### Caractéristiques

- **Couleur** : 🟡 Jaune
- **Champ obligatoire** : Description du problème requise
- **Validation stricte** : Impossible de valider sans description
- **Nettoyage auto** : Description effacée lors du changement de statut

### Utilisation

1. Clic droit sur locomotive → "Modifier le statut"
2. Sélectionner "A verifier / Defaut mineur"
3. **Remplir obligatoirement** la description
4. Valider → Locomotive devient jaune

### Les 4 Statuts

- ✅ **OK** (Vert) : Locomotive opérationnelle
- 🟠 **Manque de Traction** (Orange) : Traction réduite avec pourcentage
- 🟡 **Défaut Mineur** (Jaune) : À vérifier avec description
- 🔴 **HS** (Rouge) : Hors service avec motif obligatoire

---

## 📊 TapisT13 - Rapport Intelligent

### Description
Rapport T13 intelligent avec support du placement prévisionnel et affichage différencié.

### Caractéristiques

#### Support du Placement Prévisionnel
- Utilise la position du ghost (position future) si locomotive en mode prévisionnel
- Sinon, utilise la position réelle actuelle

#### Affichage Différencié

**Locomotives HS** :
- 🔴 Rouge : "TileName TrainNumber"
- Apparaît dans les deux colonnes

**Locomotives sur Ligne avec Train** :
- 🟢 Vert : "TileName TrainNumber"
- Colonne rapport uniquement

**Locomotives Disponibles** :
- Pas de couleur : "DISPO TileName"

**Locomotives sur Ligne de Roulement** :
- Pas de couleur : Numéro seul (ex: "1103")

#### Pourcentages de Traction
- 75%, 50%, 25% affichés dans le rapport
- Vision précise de la capacité du parc

---

## 🎯 Fonctionnalités d'Ergonomie

### ⚡ Double-Clic Transfert de Pool

**Description** : Transférez instantanément une locomotive entre Sibelit et Lineas.

**Utilisation** :
- Double-cliquez sur une locomotive dans la liste
- Elle change automatiquement de pool
- Plus besoin d'ouvrir la fenêtre de gestion

### 💾 Sauvegarde Automatique des Fenêtres

**Description** : Taille, position et état des fenêtres sauvegardés automatiquement.

**Fenêtres concernées** :
- MainWindow (fenêtre principale)
- TapisT13Window
- PoolTransferWindow
- DatabaseWindow
- ImportWindow

**Stockage** : `%AppData%\Ploco\WindowSettings.json`

### 📝 Informations de Traction Enrichies

**Caractéristiques** :
- Commentaire optionnel pour "Manque de Traction"
- Pourcentage affiché (75%, 50%, 25%)
- Documentation précise des problèmes
- Intégration dans le rapport T13

### 📋 Système de Logs Complet

**Fonctionnalités** :
- Enregistrement automatique de toutes les opérations
- Stockage : `%AppData%\Ploco\Logs\`
- Format : `ploco-YYYYMMDD.log`
- Rotation automatique sur 30 jours
- Accès rapide : Menu Options > Ouvrir les logs

**Contenu des logs** :
- Démarrage/arrêt application
- Déplacements de locomotives
- Changements de statut
- Opérations de forecast
- Imports de données
- Erreurs et exceptions

---

## 🚂 Gestion des Locomotives

### Statuts et Couleurs

- ✅ **OK** (Vert) : Opérationnelle
- 🟠 **Manque de Traction** (Orange) : Traction réduite avec %
- 🟡 **Défaut Mineur** (Jaune) : À vérifier
- 🔴 **HS** (Rouge) : Hors service

### Actions Disponibles

- **Glisser-déposer** : Déplacer entre voies
- **Double-clic** : Transférer entre pools
- **Clic droit** : Menu contextuel
  - Modifier statut
  - Placement prévisionnel
  - Déclarer HS
  - Swap de pool

### Gestion des Pools

- Pool Sibelit : Locomotives actives
- Pool Lineas : Locomotives en réserve
- Comptage automatique par pool
- Historique complet des transferts

---

## 🗺️ Interface Graphique

### Canvas de Tuiles

**Types de tuiles** :
- **Dépôts** : Voies principales et voies de sortie
- **Voies de garage** : Zones configurables
- **Arrêts de ligne** : Informations train

**Actions** :
- Déplacer les tuiles par glisser-déposer
- Redimensionner avec poignée (bas à droite)
- Menu contextuel pour configuration

### Voies et Zones

**Configuration** :
- Voies principales
- Voies de sortie
- Zones de garage
- Voies de ligne avec nommage

**Indicateurs** :
- Remplissage des zones (BLOCK / BIF)
- Informations train (numéro, heure, motif)
- Offsets de drop configurables
- Prévention du chevauchement

---

## 📁 Données et Persistance

### Stockage Local

- **Base de données** : `ploco.db` (SQLite)
- **Presets** : `layout_presets.json`
- **Paramètres fenêtres** : `WindowSettings.json`
- **Logs** : `%AppData%\Ploco\Logs\`

### Sauvegarde

- Sauvegarde automatique locale
- Aucun serveur externe requis
- Historique complet des actions
- Rotation automatique des logs (30 jours)

---

## 🎨 Personnalisation

### Thèmes

- Mode clair
- Mode sombre avec contraste amélioré

### Presets de Layout

- Sauvegarde de configurations
- Chargement rapide
- Suppression de presets

### Configuration

- Offsets de drop sur les voies
- Zones de garage personnalisables
- Filtrage des layouts par pool et lieu

---

## 💡 Conseils d'Utilisation

### Workflow Recommandé

1. **Import** : Synchroniser les pools avec Options > Import
2. **Planification** : Utiliser le placement prévisionnel
3. **Validation** : Confirmer les placements
4. **Suivi** : Consulter le TapisT13 pour l'état du parc
5. **Historique** : Vérifier les logs si besoin

### Raccourcis Pratiques

- **Double-clic** : Transfert de pool instantané
- **Clic droit** : Menu contextuel rapide
- **Glisser-déposer** : Déplacement de locomotives
- **Poignée** : Redimensionner les tuiles

### Bonnes Pratiques

- Utiliser le placement prévisionnel avant de déplacer
- Toujours remplir les descriptions pour DefautMineur et HS
- Vérifier le TapisT13 avant validation
- Consulter les logs en cas de problème
- Sauvegarder régulièrement les presets de layout

---

## 📚 Voir Aussi

- [README.md](../README.md) - Vue d'ensemble du projet
- [CHANGELOG.md](../CHANGELOG.md) - Historique des modifications
- [RELEASE_NOTES.md](../RELEASE_NOTES.md) - Notes de version détaillées
