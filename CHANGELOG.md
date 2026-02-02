# Changelog

Toutes les modifications notables de ce projet sont documentées dans ce fichier.

## [Unreleased]

### Ajouts
- Gestion des pools et fenêtre de transfert dédiée
- Intégration de l’historique des pools dans la nouvelle interface
- Layouts de tuiles pilotés par lieu et filtrage par pool
- Menus de tuiles avec actions de reset et presets de garage
- Presets de layout et nommage des voies de ligne
- Affichage du numéro de train dans les informations de ligne
- Comptage des pools et retour par glisser-déposer vers la liste
- Redimensionnement des tuiles et offsets de drop sur les voies
- Fenêtre de gestion de la base de données
- Obligation du motif HS et affichage dans le tapis
- Amélioration du résumé T13
- Amélioration du mode sombre et des surfaces de menus
- Canvas de tuiles pour dépôts, garages et arrêts de ligne

### Corrections
- Correction de la récupération du dernier ID SQLite
- Protection contre les fichiers SQLite invalides
- Gestion des valeurs nulles de configuration des voies
- Correction du chargement des offsets nullables
- Correction de l’utilisation manquante de CollectionViewSource
- Correction du typage des valeurs de configuration des voies
- Suppression des duplications de styles de toggle
- Correction du layout des sorties de dépôt
- Correction du layout des zones de garage
- Correction du wrapping de flotte et des aiguillages bloqués
- Prévention du chevauchement des locomotives sur les voies
- Correction de l’indentation du menu tapis
- Correction des avertissements nullable sur les statuts legacy
- Correction de la référence StatusDialog dans le menu contextuel

### Modifications
- Fenêtres auxiliaires rendues non bloquantes (modeless)
- Amélioration de la création des arrêts de ligne et alignement des menus
- Mise à jour de la logique et de l’affichage des statuts locomotives
- Séparation du numéro de locomotive et du badge de traction
- Amélioration du contraste et des espacements en mode sombre

---
