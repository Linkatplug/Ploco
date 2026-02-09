# 🗺️ Roadmap - Ploco Manager

**Dernière mise à jour** : 9 février 2026  
**Version actuelle** : 1.0.5

---

## 📋 Table des Matières

1. [Vision du Projet](#-vision-du-projet)
2. [État Actuel](#-état-actuel)
3. [Historique des Versions](#-historique-des-versions)
4. [Fonctionnalités Complétées](#-fonctionnalités-complétées)
5. [Court Terme (v1.1.0)](#-court-terme-v110)
6. [Moyen Terme (v1.2.0 - v1.5.0)](#-moyen-terme-v120---v150)
7. [Long Terme (v2.0.0+)](#-long-terme-v200)
8. [Améliorations Continues](#-améliorations-continues)

---

## 🎯 Vision du Projet

**Ploco** est une application de gestion visuelle de parc de locomotives destinée à faciliter la coordination et le suivi logistique des locomotives.

### Objectifs Principaux

- ✅ **Visualisation intuitive** : Canvas interactif avec tuiles représentant dépôts, garages et lignes
- ✅ **Gestion simplifiée** : Glisser-déposer, double-clic, actions contextuelles
- ✅ **Planification intelligente** : Placement prévisionnel, synchronisation des pools
- ✅ **Traçabilité complète** : Historique, logs, rapports détaillés
- 🔄 **Automatisation** : Import/export, synchronisation, notifications
- 🔄 **Collaboration** : Support multi-utilisateurs, permissions, synchronisation cloud

---

## 📊 État Actuel

### Version 1.0.5 (Février 2026) - ✅ Stable

**Statut** : Version de production stable avec toutes les fonctionnalités principales implémentées.

#### Principales Réalisations

- ✅ **Placement Prévisionnel** : Planification des déplacements avant validation
- ✅ **Import par Lot** : Synchronisation automatique des pools depuis Excel/presse-papier
- ✅ **4 Statuts de Locomotives** : OK, Manque de Traction, Défaut Mineur, HS
- ✅ **TapisT13** : Rapport intelligent avec support du placement prévisionnel
- ✅ **Système de Logs** : Traçabilité complète avec rotation automatique
- ✅ **Sauvegarde Fenêtres** : Position et taille mémorisées
- ✅ **Documentation Complète** : 30+ fichiers de documentation organisés

#### Métriques de Qualité

- 🟢 **0 warnings** au build
- 🟢 **0 errors** connus
- 🟢 **100% compatible** avec versions précédentes
- 🟢 **Documentation** complète en français
- 🟢 **Persistance locale** robuste (SQLite + JSON)

---

## 📚 Historique des Versions

### Version 1.0.5 - Février 2026
**Thème** : Planification et Automatisation

Fonctionnalités majeures :
- Placement Prévisionnel (forecast placement)
- Import de données par lot avec synchronisation
- Nouveau statut "Défaut Mineur"
- Système de logs complet
- Double-clic transfert de pool
- Sauvegarde automatique des fenêtres

### Version 1.0.0 - 1.0.4
**Thème** : Fondations et Fonctionnalités de Base

Réalisations principales :
- Architecture WPF + SQLite
- Canvas de tuiles interactif
- Gestion des locomotives (4 statuts)
- Système de pools (Sibelit/Lineas)
- Drag & drop intuitif
- Rapport TapisT13
- Mode sombre
- Historique des actions
- 30+ fenêtres et dialogues

---

## ✅ Fonctionnalités Complétées

### 🚂 Gestion des Locomotives

#### Statuts et États
- ✅ 4 statuts avec codes couleur : OK (vert), Manque de Traction (orange), Défaut Mineur (jaune), HS (rouge)
- ✅ Informations de traction enrichies (pourcentages 75%, 50%, 25%)
- ✅ Commentaires et descriptions obligatoires pour certains statuts
- ✅ Validation stricte des changements de statut

#### Déplacements et Affectations
- ✅ Glisser-déposer entre voies
- ✅ Double-clic pour transfert rapide entre pools
- ✅ Placement prévisionnel (forecast) avec visualisation
- ✅ Menu contextuel avec actions rapides
- ✅ Protection contre les opérations invalides

### 📦 Import et Synchronisation

- ✅ Import par lot depuis Excel/presse-papier
- ✅ Synchronisation bidirectionnelle automatique (Sibelit ↔ Lineas)
- ✅ Statistiques détaillées des modifications
- ✅ Validation et filtrage des numéros invalides
- ✅ Logs complets de toutes les opérations

### 🎨 Interface Graphique

#### Canvas de Tuiles
- ✅ 3 types de tuiles : Dépôts, Garages, Arrêts de ligne
- ✅ Déplacement par glisser-déposer
- ✅ Redimensionnement avec poignée
- ✅ Configuration contextuelle
- ✅ Layouts sauvegardables (presets)

#### Ergonomie
- ✅ Mode clair et mode sombre
- ✅ Contraste optimisé
- ✅ Sauvegarde automatique taille/position fenêtres
- ✅ Support multi-écrans
- ✅ Menus contextuels intuitifs

### 📊 Rapports et Suivi

- ✅ **TapisT13** : Rapport intelligent avec placement prévisionnel
- ✅ Affichage différencié par contexte (HS, disponibles, sur ligne)
- ✅ Pourcentages de traction dans rapports
- ✅ Historique complet des actions
- ✅ Système de logs avec rotation (30 jours)
- ✅ Accès rapide aux logs via menu

### 💾 Persistance et Configuration

- ✅ Base SQLite (ploco.db)
- ✅ Presets JSON (layout_presets.json)
- ✅ Paramètres fenêtres (%AppData%\Ploco\WindowSettings.json)
- ✅ Logs organisés (%AppData%\Ploco\Logs\)
- ✅ Migration automatique de base de données
- ✅ Compatibilité ascendante garantie

### 📖 Documentation

- ✅ README complet avec captures d'écran
- ✅ Guide Utilisateur détaillé
- ✅ Guide des Fonctionnalités
- ✅ Notes de Version
- ✅ Changelog complet
- ✅ Documentation technique par fonctionnalité
- ✅ Archive de documentation historique

---

## 🎯 Court Terme (v1.1.0)

**Période estimée** : Mars - Avril 2026  
**Thème** : Export et Notifications

### Priorité Haute

#### 📤 Export de Données
- **Objectif** : Permettre l'export des données en différents formats
- **Fonctionnalités** :
  - Export Excel (.xlsx) du parc de locomotives
  - Export CSV pour compatibilité universelle
  - Export PDF des rapports
  - Sélection des colonnes à exporter
  - Filtres d'export (par pool, par statut, par lieu)
- **Bénéfices** :
  - Intégration avec outils externes
  - Archivage des données
  - Partage avec autres services

#### 📅 Import Dates d'Entretien
- **Objectif** : Gérer les maintenances préventives
- **Fonctionnalités** :
  - Import depuis Excel/presse-papier
  - Colonne "Prochaine maintenance" dans interface
  - Calcul automatique des délais
  - Tri par urgence de maintenance
- **Bénéfices** :
  - Planification des maintenances
  - Prévention des pannes
  - Optimisation de la disponibilité

#### 🔔 Système de Notifications
- **Objectif** : Alertes proactives pour événements importants
- **Fonctionnalités** :
  - Notifications pour locomotives HS (urgent)
  - Alertes maintenance proche (7 jours)
  - Notification de conflits dans placement prévisionnel
  - Résumé quotidien (optionnel)
- **Interface** :
  - Centre de notifications dans interface
  - Badge de compteur
  - Historique des notifications
- **Bénéfices** :
  - Réactivité améliorée
  - Moins d'oublis
  - Meilleure coordination

### Priorité Moyenne

#### 🔍 Recherche et Filtres Avancés
- **Objectif** : Trouver rapidement des locomotives
- **Fonctionnalités** :
  - Barre de recherche globale
  - Filtres multiples combinables (statut + pool + lieu)
  - Recherche par numéro de train
  - Recherche par plage de numéros
  - Historique des recherches
- **Bénéfices** :
  - Navigation rapide dans grands parcs
  - Identification rapide des problèmes

#### 📊 Statistiques de Base
- **Objectif** : Vue d'ensemble du parc
- **Métriques** :
  - Répartition par statut (graphique en camembert)
  - Évolution du nombre de HS dans le temps
  - Temps moyen par statut
  - Taux d'utilisation des voies
- **Interface** :
  - Tableau de bord simple
  - Graphiques intégrés
- **Bénéfices** :
  - Vision stratégique
  - Aide à la décision

---

## 🚀 Moyen Terme (v1.2.0 - v1.5.0)

**Période estimée** : Mai 2026 - Décembre 2026  
**Thème** : Collaboration et Intelligence

### Version 1.2.0 - Statistiques Avancées

#### 📈 Module d'Analytics
- **Tableaux de bord interactifs** :
  - Vue hebdomadaire/mensuelle/annuelle
  - Comparaison des périodes
  - Tendances et prévisions
- **Rapports personnalisables** :
  - Générateur de rapports drag & drop
  - Templates de rapports sauvegardables
  - Export automatique programmé
- **KPIs métiers** :
  - Disponibilité du parc (%)
  - Temps moyen de réparation
  - Coût des immobilisations
  - Performance par type de locomotive

#### 📊 Analyse Prédictive
- Prédiction des pannes basée sur historique
- Recommandations de maintenance préventive
- Optimisation de l'affectation des locomotives

### Version 1.3.0 - Synchronisation Cloud (Optionnelle)

#### ☁️ Backup Cloud
- **Sauvegarde automatique** :
  - Backup quotidien vers cloud
  - Chiffrement end-to-end
  - Restauration en un clic
- **Choix du provider** :
  - Support Azure, AWS, Google Cloud
  - Stockage local reste disponible

#### 🔄 Synchronisation Multi-Postes
- **Mode optionnel** :
  - Activation volontaire par utilisateur
  - Synchronisation temps réel ou différée
  - Gestion des conflits intelligente
- **Cas d'usage** :
  - Plusieurs postes dans un même bureau
  - Poste fixe + laptop
  - Backup de sécurité

### Version 1.4.0 - Application Mobile Companion

#### 📱 App Mobile (Android/iOS)
- **Fonctionnalités principales** :
  - Vue en lecture seule du parc
  - Consultation des rapports
  - Notifications push
  - Changement rapide de statut
  - Scan QR code pour identifier locomotive
- **Synchronisation** :
  - Temps réel avec application desktop
  - Mode offline avec sync à la reconnexion
- **Cas d'usage** :
  - Consultation terrain
  - Validation rapide de statut
  - Alertes mobiles

### Version 1.5.0 - Améliorations d'Efficacité

#### ⚡ Performance et Optimisations
- Optimisation du rendu canvas (grandes flottes)
- Cache intelligent des données
- Chargement paresseux des tuiles

#### 🎨 Personnalisation Avancée
- Thèmes de couleurs personnalisables
- Layouts adaptables par utilisateur
- Raccourcis clavier configurables

#### 📦 Intégrations Externes
- API REST pour systèmes tiers
- Webhooks pour événements
- Connecteurs SAP, Maximo, etc.

---

## 🌟 Long Terme (v2.0.0+)

**Période estimée** : 2027+  
**Thème** : Entreprise et Collaboration

### Version 2.0.0 - Multi-Utilisateurs

#### 👥 Gestion des Utilisateurs
- **Authentification** :
  - Comptes utilisateurs individuels
  - SSO (Single Sign-On) avec Active Directory
  - Authentification à deux facteurs (2FA)
- **Profils** :
  - Informations utilisateur
  - Préférences personnelles
  - Historique d'actions par utilisateur

#### 🔐 Système de Permissions
- **Rôles prédéfinis** :
  - Administrateur : Accès complet
  - Gestionnaire : Gestion locomotives + rapports
  - Opérateur : Déplacements uniquement
  - Lecteur : Vue seule
- **Permissions granulaires** :
  - Par fonctionnalité
  - Par pool
  - Par lieu
- **Audit complet** :
  - Qui a fait quoi et quand
  - Traçabilité totale des modifications

### Version 2.1.0 - Collaboration Temps Réel

#### 🤝 Edition Collaborative
- **Présence utilisateurs** :
  - Voir qui est connecté
  - Curseurs des autres utilisateurs
  - Indicateurs de verrouillage
- **Modifications simultanées** :
  - Gestion intelligente des conflits
  - Merge automatique quand possible
  - Notifications de changements
- **Communication** :
  - Chat intégré par lieu/voie
  - Commentaires sur locomotives
  - Mentions @utilisateur

### Version 2.2.0 - Intelligence Artificielle

#### 🤖 IA et Machine Learning
- **Prédictions avancées** :
  - Prédiction de pannes avec 85%+ précision
  - Recommandations d'affectation optimales
  - Détection d'anomalies comportementales
- **Optimisation automatique** :
  - Suggestion de routage optimal
  - Équilibrage automatique de charge
  - Planification intelligente de maintenance
- **Assistants virtuels** :
  - Assistant vocal pour opérations terrain
  - Chatbot pour support utilisateur
  - Génération automatique de rapports

### Version 2.3.0 - IoT et Capteurs

#### 📡 Intégration IoT
- **Capteurs temps réel** :
  - Position GPS des locomotives
  - État mécanique (température, pression, etc.)
  - Consommation énergétique
  - Niveau de carburant
- **Alertes automatiques** :
  - Détection automatique de pannes
  - Alertes de dépassement de seuils
  - Notifications de maintenance imminente
- **Tableau de bord temps réel** :
  - Carte avec positions actuelles
  - Télémétrie en direct
  - Alertes visuelles

### Version 3.0.0 - Plateforme Complète

#### 🏢 Solution Entreprise
- **Multi-sites** :
  - Gestion de plusieurs dépôts/sites
  - Vue consolidée multi-sites
  - Transferts inter-sites
- **Gestion de flotte avancée** :
  - Support multi-types de véhicules
  - Gestion du personnel (conducteurs)
  - Planification de trajets
- **Modules complémentaires** :
  - Module financier (coûts, facturation)
  - Module RH (planning conducteurs)
  - Module achats (pièces détachées)

---

## 🔄 Améliorations Continues

### Performance
- Optimisations régulières du code
- Réduction de la consommation mémoire
- Amélioration des temps de chargement

### Sécurité
- Audits de sécurité trimestriels
- Mises à jour des dépendances
- Correction des vulnérabilités

### Qualité
- Tests automatisés (unit, intégration, E2E)
- Revue de code systématique
- Amélioration continue de la documentation

### Expérience Utilisateur
- Recueil régulier de feedback
- A/B testing des nouvelles fonctionnalités
- Amélioration de l'accessibilité

---

## 📋 Processus de Priorisation

### Comment les Fonctionnalités sont Choisies

1. **Feedback utilisateurs** : Demandes et suggestions
2. **Analyse d'usage** : Fonctionnalités les plus utilisées
3. **Impact business** : ROI et valeur ajoutée
4. **Effort de développement** : Complexité technique
5. **Dépendances** : Prérequis techniques

### Critères de Décision

- **Impact utilisateur** : Fort/Moyen/Faible
- **Effort développement** : Petit/Moyen/Grand
- **Urgence** : Critique/Haute/Moyenne/Basse
- **Alignement stratégique** : Oui/Non

---

## 🎯 Demandes de Fonctionnalités

Vous avez une idée pour améliorer Ploco ?

### Comment Contribuer

1. **GitHub Issues** : Créer une issue avec le tag `enhancement`
2. **Format suggéré** :
   ```
   ### Problème
   Description du besoin ou problème

   ### Solution proposée
   Comment cela pourrait fonctionner

   ### Alternatives
   Autres approches considérées

   ### Bénéfices
   Impact attendu
   ```

3. **Discussion** : Les propositions sont discutées en communauté
4. **Priorisation** : Intégration dans le backlog selon critères

---

## 📈 Métriques de Succès

### KPIs du Projet

- **Adoption** : Nombre d'utilisateurs actifs
- **Satisfaction** : Score NPS (Net Promoter Score)
- **Fiabilité** : Uptime > 99.9%
- **Performance** : Temps de réponse < 200ms
- **Qualité** : 0 bugs critiques en production

### Objectifs 2026

- ✅ v1.0.5 stable et documentée (Février)
- 🎯 v1.1.0 avec export et notifications (Avril)
- 🎯 v1.2.0 avec statistiques avancées (Juin)
- 🎯 v1.3.0 avec synchronisation cloud (Septembre)
- 🎯 v1.4.0 avec app mobile (Décembre)

---

## 🔗 Liens Utiles

- **Documentation** : [docs/](docs/)
- **Guide Utilisateur** : [docs/USER_GUIDE.md](docs/USER_GUIDE.md)
- **Fonctionnalités** : [docs/FEATURES.md](docs/FEATURES.md)
- **Changelog** : [CHANGELOG.md](CHANGELOG.md)
- **Notes de Version** : [RELEASE_NOTES.md](RELEASE_NOTES.md)
- **GitHub** : [Linkatplug/PlocoManager](https://github.com/Linkatplug/PlocoManager)

---

## 👨‍💻 Équipe et Contributeurs

**Développeur Principal** : LinkAtPlug

### Contributions Bienvenues

Le projet accepte les contributions dans les domaines suivants :
- 💻 Code (nouvelles fonctionnalités, corrections)
- 📖 Documentation (guides, traductions)
- 🐛 Rapports de bugs
- 💡 Suggestions d'améliorations
- 🎨 Design et UX

---

## 📄 Licence

Ce projet est distribué sous licence MIT.

---

## 📝 Notes de Version

### Version de ce Document

- **Version** : 1.0
- **Date** : 9 février 2026
- **Dernière révision** : 9 février 2026

### Changelog du Roadmap

- **1.0** (9 février 2026) : Création initiale du roadmap complet

---

**Roadmap vivant** : Ce document est mis à jour régulièrement pour refléter l'évolution du projet et les nouvelles priorités.

**Questions ?** Consultez la [documentation](docs/) ou créez une issue sur GitHub.

---

*"Ploco - Simplifier la gestion de parc de locomotives, une locomotive à la fois"* 🚂✨
