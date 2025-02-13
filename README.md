# Ploco - Gestion des Locomotives

## Description
Ploco est une application Windows permettant de gérer un parc de locomotives de manière visuelle. L'interface propose un système de glisser-déposer permettant de placer et déplacer les locomotives sur un tapis représentant les voies.

## Fonctionnalités
- Gestion des locomotives avec un suivi de leur statut
- Système de glisser-déposer pour organiser les locomotives
- Historique des affectations
- Modification des statuts avec codes couleur
- Sauvegarde et chargement de l'état du logiciel
- Menu de gestion des locomotives et options avancées
- Image de fond représentant les voies ferrées

## Installation
1. Clonez le dépôt GitHub :
   ```sh
   git clone https://github.com/votre-utilisateur/ploco.git
   ```
2. Ouvrez le projet dans **Visual Studio**.
3. Assurez-vous que les dépendances NuGet sont restaurées.
4. Compilez et exécutez l'application en mode Debug ou Release.

## Dépendances
- .NET 8.0
- WPF (Windows Presentation Foundation)
- Newtonsoft.Json (pour la gestion des sauvegardes)

## Utilisation
1. **Ajouter une locomotive** : Faites glisser une locomotive depuis la liste sur la gauche.
2. **Modifier le statut** : Clic droit sur une locomotive puis "Modifier statut".
3. **Swap de pool** : Clic droit et "Swap" pour changer une locomotive de pool.
4. **Sauvegarde et chargement** : Utilisez le menu "Fichier".
5. **Réinitialisation** : Via "Options > Reset", remet toutes les locomotives en état initial.

## Contribuer
1. Forkez le projet.
2. Créez une branche (`feature/ma-nouvelle-fonction`).
3. Effectuez vos modifications et poussez vos commits.
4. Ouvrez une Pull Request.

## Auteur
Développé par **Max**

## Licence
Ce projet est sous licence MIT. Consultez `LICENSE` pour plus d'informations.

