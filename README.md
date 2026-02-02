# Ploco - Gestion des Locomotives

## Description
Ploco est une application Windows (WPF) en cours de développement qui permet de gérer un parc de locomotives de manière visuelle.
La nouvelle version repose sur un canvas de tuiles (dépôts, voies de garage, arrêts de ligne) où l'on dépose les
locomotives par glisser-déposer, avec un suivi détaillé de leur état et de leur position.

## Fonctionnalités
- Gestion du parc (statut OK / manque de traction / HS, avec pourcentage de traction et motif HS)
- Système de glisser-déposer pour déplacer les locomotives entre les voies
- Canvas de tuiles redimensionnables/déplaçables : dépôts, voies de garage et arrêts de ligne
- Voies configurables : voies principales, voies de sortie, zones de garage, voies de ligne
- Gestion des zones avec indicateurs de remplissage (BLOCK/BIF)
- Arrêts de ligne avec infos train (numéro, heure d'arrêt, motif)
- Gestion des pools (Lineas/Sibelit) et fenêtre dédiée de transfert
- Historique des actions (affectations, changements de statut, modifications de tuiles)
- Presets de layout (enregistrer/charger/supprimer une configuration de tuiles)
- Mode sombre
- Sauvegarde automatique via base SQLite locale (`ploco.db`) + presets dans `layout_presets.json`
- En cours de dev... et ça va être long XD

## Dépendances
- .NET 8.0
- WPF (Windows Presentation Foundation)
- Newtonsoft.Json (pour la gestion des sauvegardes)
- Microsoft.Data.Sqlite (persistance locale)

## Utilisation
1. **Ajouter un lieu (tuile)** : bouton *Ajouter un lieu*, puis choisissez le type (dépôt, voie de garage, arrêt de ligne).
2. **Déplacer/redimensionner une tuile** : cliquez-glissez la tuile ; utilisez la poignée en bas à droite pour la taille.
3. **Ajouter/éditer des voies** : menu contextuel de la tuile (ex. *Ajouter voie de sortie*, *Ajouter zone*, *Ajouter voie*).
4. **Déplacer une locomotive** : glissez-la depuis la liste de gauche vers une voie.
5. **Modifier le statut** : clic droit sur la locomotive puis *Modifier statut* ou *Loc HS*.
6. **Swap de pool** : clic droit et *Swap* pour basculer une loc entre les pools.
7. **Gestion des parcs & historique** : menu *Gestion*.
8. **Presets & mode sombre** : menu *Vue* pour les presets, *Option* pour le thème.
9. **Sauvegarde/chargement** : menu *Fichier* (état stocké dans `ploco.db`).

## Auteur
Développé par **LinkAtPlug**

## Licence
Ce projet est sous licence MIT. Consultez `LICENSE` pour plus d'informations.
