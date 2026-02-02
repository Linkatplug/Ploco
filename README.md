# Ploco – Gestion de Parc de Locomotives

## Description

**Ploco** est une application Windows (WPF) destinée à la gestion visuelle d’un parc de locomotives.

L’application repose sur un **canvas de tuiles** représentant des dépôts, des voies de garage et des arrêts de ligne.  
Les locomotives peuvent être déplacées par **glisser-déposer**, avec un suivi précis de leur état et de leur position, le tout sauvegardé localement.

Ploco est actuellement en cours de développement actif.

---

## Fonctionnalités

- Gestion visuelle du parc de locomotives (OK / traction réduite / HS)
- Pourcentage de traction et motif HS obligatoire
- Glisser-déposer des locomotives entre les voies
- Canvas de tuiles :
  - Dépôts
  - Voies de garage
  - Arrêts de ligne
- Voies configurables :
  - Voies principales
  - Voies de sortie
  - Zones de garage
  - Voies de ligne
- Indicateurs de remplissage des zones (BLOCK / BIF)
- Arrêts de ligne avec informations train (numéro, heure d’arrêt, motif)
- Gestion des pools (Lineas / Sibelit) avec fenêtre de transfert dédiée
- Historique des actions (affectations, statuts, modifications de layout)
- Presets de layout (sauvegarde / chargement / suppression)
- Mode sombre
- Sauvegarde locale automatique
- Aucun serveur externe requis

---

## Stack technique

- .NET 8.0
- WPF (Windows Presentation Foundation)
- SQLite (persistance locale)
- Newtonsoft.Json (gestion des layouts et presets)
- Microsoft.Data.Sqlite

---

## Données et persistance

- Base de données principale : `ploco.db`
- Presets de layout : `layout_presets.json`
- Toutes les données sont stockées localement

---

## Utilisation

- **Ajouter un lieu (tuile)**  
  Bouton *Ajouter un lieu*, puis sélection du type (dépôt, voie de garage, arrêt de ligne)

- **Déplacer ou redimensionner une tuile**  
  Glisser la tuile pour la déplacer  
  Utiliser la poignée en bas à droite pour la redimensionner

- **Configurer les voies**  
  Menu contextuel de la tuile (ajout de voie, zone, sortie, etc.)

- **Déplacer une locomotive**  
  Glisser la locomotive depuis la liste vers une voie

- **Modifier le statut d’une locomotive**  
  Clic droit → modifier statut ou déclarer HS

- **Changer de pool**  
  Clic droit → swap de pool

- **Gestion des parcs et historique**  
  Menu *Gestion*

- **Presets et thème**  
  Menu *Vue* pour les presets  
  Menu *Options* pour le thème

---

## Auteur

Développé par **LinkAtPlug**

---

## Licence

Ce projet est distribué sous licence MIT.  
Voir le fichier `LICENSE` pour plus d’informations.
