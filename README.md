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
- Gestion des pools avec fenêtre de transfert dédiée
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

### Screenshot

<img width="1425" height="878" alt="YYycwUrT2z" src="https://github.com/user-attachments/assets/3d616e8d-e754-49af-87cb-ee7857e5a180" />
<img width="1425" height="878" alt="FrtPaT2jaB" src="https://github.com/user-attachments/assets/6f175149-239a-46e0-95d2-b33ed69a6510" />
<img width="1425" height="878" alt="OTYYviWRnH" src="https://github.com/user-attachments/assets/23124c25-8855-4e65-8837-c4ca1752ae29" />
<img width="1425" height="878" alt="7tDbuq8MHB" src="https://github.com/user-attachments/assets/d2fbe378-b1d1-49d3-a3b9-481ecd9157a9" />
<img width="1425" height="878" alt="DEIlEmP72y" src="https://github.com/user-attachments/assets/ff065709-afba-4f10-ab6d-344b25382622" />
<img width="1425" height="878" alt="Hlu7fSlRMC" src="https://github.com/user-attachments/assets/d83e76e5-1ed7-4cb6-b904-a28c8c5ad7b9" />

## Auteur

Développé par **LinkAtPlug**

---

## Licence

Ce projet est distribué sous licence MIT.  
Voir le fichier `LICENSE` pour plus d’informations.
