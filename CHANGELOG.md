# Changelog

Toutes les modifications notables de ce projet sont documentées dans ce fichier.

Le format est basé sur [Keep a Changelog](https://keepachangelog.com/fr/1.0.0/),
et ce projet adhère au [Semantic Versioning](https://semver.org/lang/fr/).

---

## [1.0.5] - 2026-02-09

### Ajouts Majeurs

#### 🔵 Placement Prévisionnel (Forecast Placement)
- Planification des déplacements de locomotives sans les déplacer physiquement
- Indicateurs visuels : locomotive bleue dans la tuile d'origine, copie fantôme verte sur la ligne cible
- Actions de validation ou d'annulation du placement
- Gestion automatique des conflits (ligne occupée entre-temps)
- Protection contre les opérations non autorisées sur les fantômes
- Support complet dans le rapport TapisT13

#### 📦 Import de Données par Lot
- Import de locomotives en masse depuis le presse-papier (Excel, CSV)
- Synchronisation bidirectionnelle automatique :
  - Ajout automatique à Sibelit des locomotives listées
  - Retour automatique à Lineas des locomotives non listées
  - Conservation des locomotives déjà dans Sibelit
- Statistiques détaillées après import (ajouts, retraits, inchangées)
- Validation et filtrage des numéros invalides
- Logs complets de toutes les opérations d'import

#### 🟡 Nouveau Statut "Défaut Mineur"
- Statut intermédiaire entre "OK" et "HS" avec couleur jaune
- Description du défaut obligatoire
- Validation stricte : impossible de valider sans description
- Nettoyage automatique de la description lors du changement de statut
- Persistance dans SQLite (colonne defaut_info)
- Affichage dans les rapports et tuiles

#### 📊 TapisT13 - Implémentation Complète
- Support du placement prévisionnel (utilise la position du ghost)
- Affichage différencié par contexte :
  - Locomotives HS : rouge "TileName TrainNumber" (deux colonnes)
  - Locomotives sur ligne avec train : vert "TileName TrainNumber" (colonne rapport)
  - Locomotives disponibles : "DISPO TileName"
  - Locomotives sur ligne de roulement : numéro seul
- Pourcentages de traction affichés (75%, 50%, 25%)
- Logique cohérente avec le système existant

### Améliorations d'Ergonomie

#### ⚡ Double-Clic Transfert de Pool
- Transfert instantané entre pools Sibelit ↔ Lineas par double-clic
- Hit-testing précis (indépendant de la sélection)
- Plus besoin d'ouvrir la fenêtre de gestion des pools

#### 💾 Sauvegarde Automatique des Fenêtres
- Taille, position et état (normal/maximisé) sauvegardés automatiquement
- Support multi-écrans
- Fenêtres concernées : MainWindow, TapisT13Window, PoolTransferWindow, DatabaseWindow, ImportWindow
- Stockage dans %AppData%\Ploco\WindowSettings.json

#### 📝 Informations de Traction Enrichies
- Commentaire optionnel pour le statut "Manque de Traction"
- Pourcentages de traction (75%, 50%, 25%)
- Documentation précise des problèmes de traction
- Intégration dans le rapport T13

#### 📋 Système de Logs Complet
- Enregistrement automatique de toutes les opérations :
  - Démarrage/arrêt de l'application
  - Déplacements de locomotives
  - Changements de statut
  - Opérations de forecast (placement prévisionnel)
  - Imports de données
  - Erreurs et exceptions
- Stockage organisé dans %AppData%\Ploco\Logs\
- Format : ploco-YYYYMMDD.log
- Rotation automatique sur 30 jours
- Accès rapide via menu Options > Ouvrir les logs
- Thread-safe (multi-threading supporté)

### Corrections de Bugs

- **Rafraîchissement de la liste** : Liste de gauche mise à jour automatiquement après import
- **Gestion des fantômes** : Locomotives fantômes jamais sauvegardées en base de données
- **Validation des statuts** : Validation stricte des champs obligatoires pour DefautMineur et HS
- **Correction de la récupération du dernier ID SQLite**
- **Protection contre les fichiers SQLite invalides**

### Documentation

- Réorganisation complète de la documentation (88% de réduction des fichiers à la racine)
- Guide utilisateur complet (docs/USER_GUIDE.md)
- Guide des fonctionnalités (docs/FEATURES.md)
- Documentation détaillée par fonctionnalité dans docs/features/
- Documentation technique archivée dans docs/archive/
- Notes de version détaillées (RELEASE_NOTES.md)

---

## [1.0.0 - 1.0.4] - Versions initiales

### Architecture et Technologies

#### Stack Technique
- **.NET 8.0** avec WPF (Windows Presentation Foundation)
- **SQLite** (Microsoft.Data.Sqlite) pour la persistance locale
- **Newtonsoft.Json** pour la gestion des layouts et presets
- **PdfPig** et **PdfSharpCore** pour la génération de rapports PDF
- Architecture MVVM avec INotifyPropertyChanged

### Fonctionnalités de Base

#### 🚂 Gestion des Locomotives
- Système de 4 statuts avec codes couleur :
  - ✅ OK (Vert) : Locomotive opérationnelle
  - 🟠 Manque de Traction (Orange) : Traction réduite
  - 🟡 Défaut Mineur (Jaune) : À vérifier (ajouté en v1.0.5)
  - 🔴 HS (Rouge) : Hors service
- Glisser-déposer intuitif entre voies
- Menu contextuel avec actions rapides
- Changement de statut avec validation

#### 🎯 Gestion des Pools
- Pool Sibelit : Locomotives actives
- Pool Lineas : Locomotives en réserve
- Fenêtre de transfert dédiée (PoolTransferWindow)
- Comptage automatique par pool
- Historique complet des transferts
- Intégration de l'historique dans l'interface

#### 🗺️ Interface Graphique - Canvas de Tuiles
- **Types de tuiles** :
  - Dépôts : Voies principales et voies de sortie
  - Voies de garage : Zones configurables
  - Arrêts de ligne : Informations train (numéro, heure, motif)
- **Interactions** :
  - Déplacement des tuiles par glisser-déposer
  - Redimensionnement avec poignée (bas à droite)
  - Menu contextuel pour configuration
- **Layouts** :
  - Pilotés par lieu et filtrage par pool
  - Presets de layout sauvegardables
  - Nommage des voies de ligne
  - Actions de reset et presets de garage

#### 🎨 Personnalisation et Thèmes
- Mode clair et mode sombre
- Amélioration du contraste et des espacements en mode sombre
- Amélioration des surfaces de menus
- Presets de configuration sauvegardables dans layout_presets.json

#### 📊 Rapports et Suivi
- **TapisT13** : Rapport intelligent avec support du placement prévisionnel
- Affichage du numéro de train dans les informations de ligne
- Obligation du motif HS et affichage dans le tapis
- Historique complet des actions

### Configuration et Paramètres

#### 📁 Persistance des Données
- Base de données SQLite : ploco.db
- Presets de layout : layout_presets.json
- Paramètres des fenêtres : %AppData%\Ploco\WindowSettings.json
- Logs applicatifs : %AppData%\Ploco\Logs\
- Sauvegarde automatique locale

#### ⚙️ Configuration des Voies
- Offsets de drop configurables sur les voies
- Configuration des zones de garage
- Prévention du chevauchement des locomotives
- Gestion des aiguillages bloqués

### Corrections et Améliorations Techniques

#### Corrections
- Correction du chargement des offsets nullables
- Gestion des valeurs nulles de configuration des voies
- Correction de l'utilisation manquante de CollectionViewSource
- Correction du typage des valeurs de configuration des voies
- Suppression des duplications de styles de toggle
- Correction du layout des sorties de dépôt
- Correction du layout des zones de garage
- Correction du wrapping de flotte
- Correction de l'indentation du menu tapis
- Correction des avertissements nullable sur les statuts legacy
- Correction de la référence StatusDialog dans le menu contextuel

#### Modifications
- Fenêtres auxiliaires rendues non bloquantes (modeless)
- Amélioration de la création des arrêts de ligne
- Alignement des menus
- Mise à jour de la logique et de l'affichage des statuts locomotives
- Séparation du numéro de locomotive et du badge de traction
- Retour par glisser-déposer vers la liste (comptage des pools)

### Fenêtres et Dialogues
- MainWindow : Fenêtre principale avec canvas de tuiles
- ParcLocoWindow : Gestion du parc de locomotives
- PoolTransferWindow : Transfert entre pools
- HistoriqueWindow/HistoriqueDialog : Historique des actions
- TapisT13Window : Génération du rapport T13
- DatabaseManagementWindow : Gestion de la base de données
- ImportWindow : Import de données par lot
- SettingsWindow : Paramètres de l'application
- StatusDialog : Modification du statut des locomotives
- TileConfigDialog : Configuration des tuiles
- RollingLineSelectionDialog : Sélection de ligne pour forecast
- Et nombreux autres dialogues spécialisés

---

## Migration et Compatibilité

### Base de Données
- Migration automatique avec ajout de nouvelles colonnes (ex: defaut_info)
- Fonction EnsureColumn() pour la compatibilité ascendante
- Aucune action manuelle requise
- Toutes les données existantes préservées

### Fichiers de Configuration
- Création automatique des nouveaux fichiers de configuration
- Compatibilité totale avec les versions précédentes
- Pas de perte de données lors des mises à jour

---

## [Unreleased]

### Ajouts

#### 🗺️ Roadmap du Projet
- Ajout d'une roadmap complète (ROADMAP.md)
- Vision claire du développement futur
- Planification court, moyen et long terme
- Priorisation transparente des fonctionnalités
- Processus de contribution documenté

### Évolutions Prévues

**Note** : Consultez le [ROADMAP.md](ROADMAP.md) pour la planification complète et détaillée.

#### Court Terme (v1.1.0)
- Import des dates d'entretien depuis presse-papier
- Export Excel/CSV des données
- Notifications pour locomotives HS
- Recherche et filtres avancés
- Statistiques de base

#### Moyen Terme (v1.2.0 - v1.5.0)
- Module de statistiques avancées
- Synchronisation cloud optionnelle
- Application mobile companion
- Intégrations externes (API REST)

#### Long Terme (v2.0.0+)
- Support multi-utilisateurs
- Système de permissions
- Collaboration temps réel
- Intelligence artificielle et ML
- Intégration IoT

---

## Notes

### Liens
- **Repository** : https://github.com/Linkatplug/PlocoManager
- **Roadmap** : Voir [ROADMAP.md](ROADMAP.md)
- **Documentation** : Voir dossier docs/
- **Licence** : MIT

### Remerciements
Développé par **LinkAtPlug**

---
