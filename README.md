# Ploco – Gestion de Parc de Locomotives

## Description

**Ploco** est une application Windows (WPF) destinée à la gestion visuelle d'un parc de locomotives.

L'application repose sur un **canvas de tuiles** représentant des dépôts, des voies de garage et des arrêts de ligne.  
Les locomotives peuvent être déplacées par **glisser-déposer**, avec un suivi précis de leur état et de leur position, le tout sauvegardé localement.

Ploco est actuellement en cours de développement actif.

## 📚 Documentation

- **[Guide Utilisateur](docs/USER_GUIDE.md)** - Manuel complet d'utilisation
- **[Guide des Fonctionnalités](docs/FEATURES.md)** - Toutes les fonctionnalités détaillées
- **[Notes de Version](RELEASE_NOTES.md)** - Dernières nouveautés et améliorations
- **[Changelog](CHANGELOG.md)** - Historique des modifications
- **[Documentation des Fonctionnalités](docs/features/)** - Détails techniques par fonctionnalité

---

## 🎯 Fonctionnalités Principales

### Gestion des Locomotives
- 4 statuts avec codes couleur : OK (vert), Manque de Traction (orange), Défaut Mineur (jaune), HS (rouge)
- Glisser-déposer intuitif entre voies
- Double-clic pour transfert rapide entre pools
- **Placement prévisionnel** pour planifier les affectations

### Import et Synchronisation
- **Import par lot** depuis Excel/presse-papier
- Synchronisation automatique des pools (Sibelit ↔ Lineas)
- Statistiques détaillées des modifications

### Rapports et Suivi
- **TapisT13** : Rapport intelligent avec support du placement prévisionnel
- Historique complet des actions
- Système de logs avec rotation automatique (30 jours)

### Interface et Ergonomie
- Canvas de tuiles interactif (dépôts, garages, lignes)
- Redimensionnement des tuiles par glisser-déposer
- Mode sombre avec contraste optimisé
- Sauvegarde automatique de la taille et position des fenêtres

📖 **[Voir toutes les fonctionnalités](docs/FEATURES.md)**

---

## 🚀 Démarrage Rapide

### Installation

1. Télécharger la dernière version
2. Extraire l'archive
3. Lancer `Ploco.exe`

### Première Utilisation

1. **Ajouter des lieux** : Bouton "Ajouter un lieu" pour créer dépôts, garages, lignes
2. **Importer des locomotives** : Menu Options > Import pour synchroniser depuis Excel
3. **Déplacer des locomotives** : Glisser-déposer depuis la liste vers les voies
4. **Planifier** : Clic droit > Placement prévisionnel pour visualiser avant validation

📖 **[Guide Utilisateur Complet](docs/USER_GUIDE.md)**

## 💻 Stack Technique

- **.NET 8.0**
- **WPF** (Windows Presentation Foundation)
- **SQLite** (Microsoft.Data.Sqlite) - Persistance locale
- **Newtonsoft.Json** - Gestion des layouts et presets

## 📦 Persistance des Données

Toutes les données sont stockées localement :
- **Base de données** : `ploco.db` (SQLite)
- **Presets** : `layout_presets.json`
- **Paramètres** : `%AppData%\Ploco\WindowSettings.json`
- **Logs** : `%AppData%\Ploco\Logs\`

### Screenshot

<img width="1425" height="878" alt="YYycwUrT2z" src="https://github.com/user-attachments/assets/3d616e8d-e754-49af-87cb-ee7857e5a180" />
<img width="1425" height="878" alt="FrtPaT2jaB" src="https://github.com/user-attachments/assets/6f175149-239a-46e0-95d2-b33ed69a6510" />
<img width="1425" height="878" alt="OTYYviWRnH" src="https://github.com/user-attachments/assets/23124c25-8855-4e65-8837-c4ca1752ae29" />
<img width="1425" height="878" alt="7tDbuq8MHB" src="https://github.com/user-attachments/assets/d2fbe378-b1d1-49d3-a3b9-481ecd9157a9" />
<img width="1425" height="878" alt="DEIlEmP72y" src="https://github.com/user-attachments/assets/ff065709-afba-4f10-ab6d-344b25382622" />
<img width="1425" height="878" alt="Hlu7fSlRMC" src="https://github.com/user-attachments/assets/d83e76e5-1ed7-4cb6-b904-a28c8c5ad7b9" />

---

## 👨‍💻 Développeur

Développé par **LinkAtPlug**

---

## 📄 Licence

Ce projet est distribué sous licence MIT.  
Voir le fichier `LICENSE` pour plus d'informations.
